﻿using Amazon.Runtime;
using NLog;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Teltec.Backup.App.Versioning;
using Teltec.Backup.Data.DAO;
using Teltec.Common.Utils;
using Teltec.Storage;
using Teltec.Storage.Implementations.S3;
using Models = Teltec.Backup.Data.Models;

namespace Teltec.Backup.App.Backup
{
	public enum BackupOperationStatus
	{
		Unknown					= 0,
		Started					= 1,
		Resumed					= 2,
		ScanningFilesStarted	= 3,
		ScanningFilesFinished	= 4,
		ProcessingFilesStarted	= 5,
		ProcessingFilesFinished = 6,
		Updated					= 7,
		Canceled				= 8,
		Failed					= 9,
		Finished				= 10,
	}

	public static class Extensions
	{
		public static bool IsEnded(this BackupOperationStatus status)
		{
			return status == BackupOperationStatus.Canceled
				|| status == BackupOperationStatus.Failed
				|| status == BackupOperationStatus.Finished;
		}
	}

	public sealed class BackupOperationEvent : EventArgs
	{
		public BackupOperationStatus Status;
		public string Message;
	}

	public sealed class BackupOperationOptions
	{
		// ...
	}

	public abstract class BackupOperation : BaseOperation
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		
		protected readonly BackupRepository _daoBackup = new BackupRepository();

		protected Models.Backup Backup;

		#region Properties

		public delegate void UpdateEventHandler(object sender, BackupOperationEvent e);
		public event UpdateEventHandler Updated;

		public DateTime? StartedAt
		{
			get { Assert.IsNotNull(Backup); return Backup.StartedAt; }
		}

		public DateTime? FinishedAt
		{
			get { Assert.IsNotNull(Backup); return Backup.FinishedAt; }
		}

		public string Sources
		{
			get
			{
				Debug.Assert(BackupAgent != null || Backup != null);
				const string delimiter = ", ", trail = "...";
				const int maxLength = 50;
				return BackupAgent != null
					? BackupAgent.FilesAsDelimitedString(delimiter, maxLength, trail)
					: Backup.BackupPlan.SelectedSourcesAsDelimitedString(delimiter, maxLength, trail);
			}
		}

		//private string _RootDir;
		//public string RootDir
		//{
		//	get
		//	{
		//		if (_RootDir == null)
		//			_RootDir = string.Format("backup-{0}", Plan.Id);
		//		return _RootDir;
		//	}
		//}

		#endregion

		#region Constructors

		public BackupOperation(BackupOperationOptions options)
		{
			Options = options;
		}

		#endregion

		#region Transfer

		protected IncrementalFileVersioner Versioner; // IDisposable
		protected BackupOperationOptions Options;
		protected CustomBackupAgent BackupAgent;

		public override void Start(out TransferResults results)
		{
			Assert.IsFalse(IsRunning);
			Assert.IsNotNull(Backup);
			Assert.IsNotNull(Backup.BackupPlan);
			Assert.IsNotNull(Backup.BackupPlan.StorageAccount);
			Assert.AreEqual(Models.EStorageAccountType.AmazonS3, Backup.BackupPlan.StorageAccountType);

			AmazonS3AccountRepository dao = new AmazonS3AccountRepository();
			Models.AmazonS3Account s3account = dao.Get(Backup.BackupPlan.StorageAccount.Id);

			//
			// Dispose and recycle previous objects, if needed.
			//
			if (TransferAgent != null)
				TransferAgent.Dispose();
			if (TransferListControl != null)
				TransferListControl.ClearTransfers();
			if (Versioner != null)
				Versioner.Dispose();

			//
			// Setup agents.
			//
			AWSCredentials awsCredentials = new BasicAWSCredentials(s3account.AccessKey, s3account.SecretKey);
			TransferAgent = new S3AsyncTransferAgent(awsCredentials, s3account.BucketName);
			TransferAgent.RemoteRootDir = string.Format("backup-plan-{0}", Backup.BackupPlan.Id);

			BackupAgent = new CustomBackupAgent(TransferAgent);
			BackupAgent.Results.Monitor = TransferListControl;

			Versioner = new IncrementalFileVersioner(CancellationTokenSource.Token);

			RegisterResultsEventHandlers(Backup, BackupAgent.Results);
			
			results = BackupAgent.Results;

			//
			// Start the backup.
			//
			DoBackup(BackupAgent, Backup, Options);
		}

		protected void RegisterResultsEventHandlers(Models.Backup backup, TransferResults results)
		{
			BackupedFileRepository daoBackupedFile = new BackupedFileRepository();
			results.Failed += (object sender, TransferFileProgressArgs args, Exception ex) =>
			{
				Models.BackupedFile backupedFile = daoBackupedFile.GetByBackupAndPath(backup, args.FilePath);
				backupedFile.TransferStatus = TransferStatus.FAILED;
				backupedFile.UpdatedAt = DateTime.UtcNow;
				daoBackupedFile.Update(backupedFile);

				var message = string.Format("Failed {0} - {1}", args.FilePath, ex != null ? ex.Message : "Unknown reason");
				Warn(message);
				//StatusInfo.Update(BackupStatusLevel.ERROR, message);
				OnUpdate(new BackupOperationEvent { Status = BackupOperationStatus.Updated, Message = message });
			};
			results.Canceled += (object sender, TransferFileProgressArgs args, Exception ex) =>
			{
				Models.BackupedFile backupedFile = daoBackupedFile.GetByBackupAndPath(backup, args.FilePath);
				backupedFile.TransferStatus = TransferStatus.CANCELED;
				backupedFile.UpdatedAt = DateTime.UtcNow;
				daoBackupedFile.Update(backupedFile);

				var message = string.Format("Canceled {0} - {1}", args.FilePath, ex != null ? ex.Message : "Unknown reason");
				Warn(message);
				//StatusInfo.Update(BackupStatusLevel.ERROR, message);
				OnUpdate(new BackupOperationEvent { Status = BackupOperationStatus.Updated, Message = message });
			};
			results.Completed += (object sender, TransferFileProgressArgs args) =>
			{
				Models.BackupedFile backupedFile = daoBackupedFile.GetByBackupAndPath(backup, args.FilePath);
				backupedFile.TransferStatus = TransferStatus.COMPLETED;
				backupedFile.UpdatedAt = DateTime.UtcNow;
				daoBackupedFile.Update(backupedFile);

				var message = string.Format("Completed {0}", args.FilePath);
				Info(message);
				OnUpdate(new BackupOperationEvent { Status = BackupOperationStatus.Updated, Message = message });
			};
			results.Started += (object sender, TransferFileProgressArgs args) =>
			{
				Models.BackupedFile backupedFile = daoBackupedFile.GetByBackupAndPath(backup, args.FilePath);
				backupedFile.TransferStatus = TransferStatus.RUNNING;
				backupedFile.UpdatedAt = DateTime.UtcNow;
				daoBackupedFile.Update(backupedFile);

				var message = string.Format("Started {0}", args.FilePath);
				//Info(message);
				OnUpdate(new BackupOperationEvent { Status = BackupOperationStatus.Updated, Message = message });
			};
			results.Progress += (object sender, TransferFileProgressArgs args) =>
			{
				//var message = string.Format("Progress {0}% {1} ({2}/{3} bytes)",
				//	args.PercentDone, args.FilePath, args.TransferredBytes, args.TotalBytes);
				//Info(message);
				OnUpdate(new BackupOperationEvent { Status = BackupOperationStatus.Updated, Message = null });
			};
		}

		protected abstract Task<LinkedList<string>> GetFilesToProcess(Models.Backup backup);
		protected abstract Task DoVersionFiles(Models.Backup backup, LinkedList<string> filesToProcess);

		protected async void DoBackup(CustomBackupAgent agent, Models.Backup backup, BackupOperationOptions options)
		{
			OnStart(agent, backup);

			//
			// Scanning
			//

			LinkedList<string> filesToProcess = null;
			{
				Task<LinkedList<string>> filesToProcessTask = GetFilesToProcess(backup);

				{
					var message = string.Format("Scanning files started.");
					Info(message);
					//StatusInfo.Update(BackupStatusLevel.INFO, message);
					OnUpdate(new BackupOperationEvent { Status = BackupOperationStatus.ScanningFilesStarted, Message = message });
				}

				try
				{
					await filesToProcessTask;
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Exception Message: " + ex.Message);
				}

				if (filesToProcessTask.IsFaulted || filesToProcessTask.IsCanceled)
				{
					OnFailure(agent, backup, filesToProcessTask.Exception);
					return;
				}

				filesToProcess = filesToProcessTask.Result;

				{
					var message = string.Format("Scanning files finished.");
					Info(message);
					//StatusInfo.Update(BackupStatusLevel.INFO, message);
					OnUpdate(new BackupOperationEvent { Status = BackupOperationStatus.ScanningFilesFinished, Message = message });
				}
			}

			//
			// Versioning
			//

			{
				Task versionerTask = DoVersionFiles(backup, filesToProcess);

				{
					var message = string.Format("Processing files started.");
					Info(message);
					//StatusInfo.Update(BackupStatusLevel.INFO, message);
					OnUpdate(new BackupOperationEvent { Status = BackupOperationStatus.ProcessingFilesStarted, Message = message });
				}

				try
				{
					await versionerTask;
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Exception Message: " + ex.Message);
				}

				if (versionerTask.IsFaulted || versionerTask.IsCanceled)
				{
					Versioner.Undo();
					OnFailure(agent, backup, versionerTask.Exception);
					return;
				}

				// IMPORTANT: Must happen before any attempt to get `Versioner.FilesToTransfer`.
				Versioner.Save();

				agent.Files = Versioner.FilesToTransfer;

				{
					var message = string.Format("Processing files finished.");
					Info(message);
					//StatusInfo.Update(BackupStatusLevel.INFO, message);
					OnUpdate(new BackupOperationEvent { Status = BackupOperationStatus.ProcessingFilesFinished, Message = message });
				}

				{
					var message = string.Format("Estimate backup size: {0} files, {1}",
						agent.Files.Count(), FileSizeUtils.FileSizeToString(agent.EstimatedTransferSize));
					Info(message);
				}
			}

			//
			// Transfer
			//

			{
				Task transferTask = agent.Start();
				await transferTask;
			}

			OnFinish(agent, backup);
		}

		public override void Cancel()
		{
			base.Cancel();
			DoCancel(BackupAgent);
		}

		protected void DoCancel(CustomBackupAgent agent)
		{
			agent.Cancel();
			CancellationTokenSource.Cancel();
		}

		#endregion

		#region Event handlers

		public virtual void OnStart(CustomBackupAgent agent, Models.Backup backup)
		{
			IsRunning = true;

			backup.DidStart();
		}

		protected void OnUpdate(BackupOperationEvent e)
		{
			if (Updated != null)
				Updated(this, e);
		}

		public void OnFailure(CustomBackupAgent agent, Models.Backup backup, Exception exception)
		{
			IsRunning = false;

			var message = string.Format("Backup failed: {0}", exception != null ? exception.Message : "Canceled?");
			Error(message);
			//StatusInfo.Update(BackupStatusLevel.ERROR, message);

			backup.DidFail();
			_daoBackup.Update(backup);

			OnUpdate(new BackupOperationEvent { Status = BackupOperationStatus.Failed, Message = message });
		}

		public void OnFinish(CustomBackupAgent agent, Models.Backup backup)
		{
			IsRunning = false;

			TransferResults.Statistics stats = agent.Results.Stats;

			var message = string.Format(
				"Backup finished! Stats: {0} completed, {1} failed, {2} canceled, {3} pending, {4} running",
				stats.Completed, stats.Failed, stats.Canceled, stats.Pending, stats.Running);
			Info(message);
			//StatusInfo.Update(BackupStatusLevel.OK, message);

			switch (agent.Results.OverallStatus)
			//switch (backup.Status)
			{
				default: throw new InvalidOperationException("Unexpected TransferStatus");
				case TransferStatus.CANCELED:
					backup.WasCanceled();
					_daoBackup.Update(backup);
					OnUpdate(new BackupOperationEvent { Status = BackupOperationStatus.Canceled, Message = message });
					break;
				case TransferStatus.FAILED:
					backup.DidFail();
					_daoBackup.Update(backup);
					OnUpdate(new BackupOperationEvent { Status = BackupOperationStatus.Failed, Message = message });
					break;
				case TransferStatus.COMPLETED:
					backup.DidComplete();
					_daoBackup.Update(backup);
					OnUpdate(new BackupOperationEvent { Status = BackupOperationStatus.Finished, Message = message });
					break;
			}
		}

		#endregion

		#region Dispose Pattern Implementation

		bool _shouldDispose = true;
		bool _isDisposed;

		/// <summary>
		/// Implements the Dispose pattern
		/// </summary>
		/// <param name="disposing">Whether this object is being disposed via a call to Dispose
		/// or garbage collected.</param>
		protected override void Dispose(bool disposing)
		{
			if (!this._isDisposed)
			{
				if (disposing && _shouldDispose)
				{
					BackupAgent = null;
					
					if (Versioner != null)
					{
						Versioner.Dispose();
						Versioner = null;
					}
				}
				this._isDisposed = true;
			}
			base.Dispose(disposing);
		}

		#endregion
	}
}
