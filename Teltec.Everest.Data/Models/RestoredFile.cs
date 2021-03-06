/* 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Teltec.Storage;

namespace Teltec.Everest.Data.Models
{
	public class RestoredFile : BaseEntity<Int64?>
	{
		public RestoredFile()
		{
		}

		public RestoredFile(Restore restore, RestorePlanFile file, Models.BackupedFile backupedFile)
			: this()
		{
			_Restore = restore;
			_File = file;
			_BackupedFile = backupedFile;
		}

		private Int64? _Id;
		public virtual Int64? Id
		{
			get { return _Id; }
			set { SetField(ref _Id, value); }
		}

		private Restore _Restore;
		public virtual Restore Restore
		{
			get { return _Restore; }
			protected set { _Restore = value; }
		}

		private RestorePlanFile _File;
		public virtual RestorePlanFile File
		{
			get { return _File; }
			protected set { _File = value; }
		}

		public virtual string Version // Non-persistent property.
		{
			get
			{
				return BackupedFile != null ? BackupedFile.Version : null;
			}
		}

		private BackupedFile _BackupedFile;
		public virtual BackupedFile BackupedFile
		{
			get { return _BackupedFile; }
			protected set { _BackupedFile = value; }
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
