﻿using System;

namespace Teltec.Backup.Data.Models
{
	public abstract class StorageAccount : BaseEntity<Int32?>
    {
		private Int32? _Id;
		public virtual Int32? Id
		{
			get { return _Id; }
			set { SetField(ref _Id, value); }
		}

		public abstract EStorageAccountType Type
		{
			get;
		}

		private String _DisplayName;
		public virtual String DisplayName
		{
			get { return _DisplayName; }
			set { SetField(ref _DisplayName, value); }
		}

		public const int HostnameMaxLen = 255;
		private String _Hostname;
		public virtual String Hostname
		{
			get { return _Hostname; }
			set { SetField(ref _Hostname, value); }
		}

		//IList<BackupPlan> BackupPlans { get; set; }
    }
}
