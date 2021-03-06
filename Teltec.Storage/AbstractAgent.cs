/* 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Teltec.Common.Extensions;
using Teltec.Storage.Backend;
using Teltec.Storage.Versioning;

namespace Teltec.Storage
{
	public abstract class AbstractAgent<TFile> : IDisposable where TFile : IVersionedFile
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		protected ITransferAgent TransferAgent { get; private set; }

		public TransferResults Results { get; private set; }

		public AbstractAgent(ITransferAgent agent)
		{
			TransferAgent = agent;
			Results = new TransferResults();
		}

		private string _FilesAsDelimitedString;
		public string FilesAsDelimitedString(string delimiter, int maxLength, string trail)
		{
			if (_FilesAsDelimitedString == null)
				_FilesAsDelimitedString = Files.AsDelimitedString(p => p.Path,
					"No files to transfer", delimiter, maxLength, trail);
			return _FilesAsDelimitedString;
		}

		private IEnumerable<TFile> _Files = new List<TFile>();
		public IEnumerable<TFile> Files
		{
			get { return _Files; }
			set
			{
				_Files = value;
				FilesChanged();
			}
		}

		protected void FilesChanged()
		{
			_EstimatedTransferSize = 0;
			_FilesAsDelimitedString = null;
			Results.Stats.Reset(_Files.Count());
		}

		// In Bytes
		private long _EstimatedTransferSize = 0;
		public long EstimatedTransferSize
		{
			get
			{
				if (_EstimatedTransferSize == 0 && Files != null)
					_EstimatedTransferSize = Enumerable.Sum(Files, p => p.Size);
				return _EstimatedTransferSize;
			}
		}

		protected void RegisterUploadEventHandlers()
		{
			TransferAgent.UploadFileStarted += (object sender, TransferFileProgressArgs e) =>
			{
				Results.Stats.Running += 1;
				Results.Stats.Pending -= 1;
				Results.OnStarted(this, e);
			};
			TransferAgent.UploadFileProgress += (object sender, TransferFileProgressArgs e) =>
			{
				//logger.Debug("## DEBUG Results.Stats.BytesCompleted = {0}, e.DeltaTransferredBytes = {1}",
				//				Results.Stats.BytesCompleted, e.DeltaTransferredBytes);
				Results.Stats.BytesCompleted += e.DeltaTransferredBytes;
				Results.OnProgress(this, e);
			};
			TransferAgent.UploadFileCanceled += (object sender, TransferFileProgressArgs e) =>
			{
				Results.Stats.Running -= 1;
				Results.Stats.Canceled += 1;
				Results.Stats.BytesCanceled += e.TotalBytes;
				//Results.Stats.BytesCompleted -= e.TransferredBytes;
				Results.OnCanceled(this, e);
			};
			TransferAgent.UploadFileFailed += (object sender, TransferFileProgressArgs e) =>
			{
				Results.Stats.Running -= 1;
				Results.Stats.Failed += 1;
				Results.Stats.BytesFailed += e.TotalBytes;
				//Results.Stats.BytesCompleted -= e.TransferredBytes;
				Results.OnFailed(this, e);
			};
			TransferAgent.UploadFileCompleted += (object sender, TransferFileProgressArgs e) =>
			{
				Results.Stats.Running -= 1;
				Results.Stats.Completed += 1;
				//Results.Stats.BytesCompleted += e.TotalBytes;
				Results.OnCompleted(this, e);
			};
		}

		protected void RegisterDownloadEventHandlers()
		{
			TransferAgent.DownloadFileStarted += (object sender, TransferFileProgressArgs e) =>
			{
				Results.Stats.Running += 1;
				Results.Stats.Pending -= 1;
				Results.OnStarted(this, e);
			};
			TransferAgent.DownloadFileProgress += (object sender, TransferFileProgressArgs e) =>
			{
				Results.Stats.BytesCompleted += e.DeltaTransferredBytes;
				Results.OnProgress(this, e);
			};
			TransferAgent.DownloadFileCanceled += (object sender, TransferFileProgressArgs e) =>
			{
				Results.Stats.Running -= 1;
				Results.Stats.Canceled += 1;
				Results.Stats.BytesCanceled += e.TotalBytes;
				//Results.Stats.BytesCompleted -= e.TransferredBytes;
				Results.OnCanceled(this, e);
			};
			TransferAgent.DownloadFileFailed += (object sender, TransferFileProgressArgs e) =>
			{
				Results.Stats.Running -= 1;
				Results.Stats.Failed += 1;
				Results.Stats.BytesFailed += e.TotalBytes;
				//Results.Stats.BytesCompleted -= e.TransferredBytes;
				Results.OnFailed(this, e);
			};
			TransferAgent.DownloadFileCompleted += (object sender, TransferFileProgressArgs e) =>
			{
				Results.Stats.Running -= 1;
				Results.Stats.Completed += 1;
				//Results.Stats.BytesCompleted += e.TotalBytes;
				Results.OnCompleted(this, e);
			};
		}

		protected void RegisterDeleteEventHandlers()
		{
			TransferAgent.DeleteFileStarted += (object sender, DeletionArgs e) =>
			{
				Results.OnDeleteStarted(this, e);
			};
			TransferAgent.DeleteFileCanceled += (object sender, DeletionArgs e) =>
			{
				Results.OnDeleteCanceled(this, e);
			};
			TransferAgent.DeleteFileFailed += (object sender, DeletionArgs e) =>
			{
				Results.OnDeleteFailed(this, e);
			};
			TransferAgent.DeleteFileCompleted += (object sender, DeletionArgs e) =>
			{
				Results.OnDeleteCompleted(this, e);
			};
		}

		public void RemoveAllFiles()
		{
			Files = new List<TFile>();
			FilesChanged();
		}

		public void Cancel()
		{
			CancelTransfers();
		}

		private void CancelTransfers()
		{
			CancellationTokenSource.Cancel();
		}

		private CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

		private void RenewCancellationToken()
		{
			bool alreadyUsed = CancellationTokenSource != null && CancellationTokenSource.IsCancellationRequested;
			if (alreadyUsed || CancellationTokenSource == null)
			{
				if (CancellationTokenSource != null)
					CancellationTokenSource.Dispose();

				CancellationTokenSource = new CancellationTokenSource();
			}
		}

		public async Task<TransferResults> Start()
		{
			RenewCancellationToken();

			await Task.Run(() =>
			{
				try
				{
					ParallelOptions options = new ParallelOptions();
					options.CancellationToken = CancellationTokenSource.Token;
					options.MaxDegreeOfParallelism = AsyncHelper.SettingsMaxThreadCount;

					ParallelLoopResult result = Parallel.ForEach(Files, options, (currentFile) =>
					{
						DoImplementation(currentFile, /*userData*/ null);
					});
				}
				catch (Exception ex)
				{
					// When there are Tasks running inside another Task, and the inner-tasks are cancelled,
					// the propagated exception is an instance of `AggregateException`, rather than
					// `OperationCanceledException`.
					if (ex.IsCancellation())
					{
						throw new OperationCanceledException("The operation was canceled.");
					}
					else
					{
						throw ex;
					}
				}
			});

			return Results;
		}

		public abstract void DoImplementation(IVersionedFile file, object userData);

		#region Dispose Pattern Implementation

		bool _shouldDispose = true;
		bool _isDisposed;

		/// <summary>
		/// Implements the Dispose pattern
		/// </summary>
		/// <param name="disposing">Whether this object is being disposed via a call to Dispose
		/// or garbage collected.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this._isDisposed)
			{
				if (disposing && _shouldDispose)
				{
					if (CancellationTokenSource != null)
					{
						CancellationTokenSource.Dispose();
						CancellationTokenSource = null;
					}
				}
				this._isDisposed = true;
			}
		}

		/// <summary>
		/// Disposes of all managed and unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
