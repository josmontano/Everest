﻿using System;
using Teltec.Storage;

namespace Teltec.Backup.Data.Models
{
	public class BackupedFile : BaseEntity<Int64?>
	{
		public BackupedFile()
		{
		}

		public BackupedFile(Backup backup, BackupPlanFile file)
			: this()
		{
			Backup = backup;
			File = file;
		}

		private Int64? _Id;
		public virtual Int64? Id
		{
			get { return _Id; }
			set { SetField(ref _Id, value); }
		}

		private Backup _Backup;
		public virtual Backup Backup
		{
			get { return _Backup; }
			protected set { _Backup = value; }
		}

		private BackupPlanFile _File;
		public virtual BackupPlanFile File
		{
			get { return _File; }
			protected set { _File = value; }
		}

		private long _FileSize;
		public virtual long FileSize
		{
			get { return _FileSize; }
			set { SetField(ref _FileSize, value); }
		}

		private BackupFileStatus _FileStatus;
		public virtual BackupFileStatus FileStatus
		{
			get { return _FileStatus; }
			set { SetField(ref _FileStatus, value); }
		}

		private TransferStatus _TransferStatus;
		public virtual TransferStatus TransferStatus
		{
			get { return _TransferStatus; }
			set { _TransferStatus = value; }
		}

		private DateTime _UpdatedAt;
		public virtual DateTime UpdatedAt
		{
			get { return _UpdatedAt; }
			set { _UpdatedAt = value; }
		}
	}
}