﻿/* 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;

namespace Teltec.Everest.Data.Models
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
		private String _Hostname = Environment.MachineName;
		public virtual String Hostname
		{
			get { return _Hostname; }
			set { SetField(ref _Hostname, value); }
		}

		//IList<BackupPlan> BackupPlans { get; set; }

		#region Files

		private IList<BackupPlanFile> _Files = new List<BackupPlanFile>();
		public virtual IList<BackupPlanFile> Files
		{
			get { return _Files; }
			protected set { SetField(ref _Files, value); }
		}

		#endregion
    }
}
