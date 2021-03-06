/* 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Teltec.Common;
using Teltec.Common.Utils;

namespace Teltec.Everest.Data.Models
{
	public class RestorePlanSourceEntry : BaseEntity<Int64?>
	{
		private Int64? _Id;
		public virtual Int64? Id
		{
			get { return _Id; }
			set { SetField(ref _Id, value); }
		}

		private RestorePlan _RestorePlan;
		public virtual RestorePlan RestorePlan
		{
			get { return _RestorePlan; }
			set { SetField(ref _RestorePlan, value); }
		}

		private EntryType _Type;
		public virtual EntryType Type
		{
			get { return _Type; }
			set { SetField(ref _Type, value); }
		}

		public const int PathMaxLen = 1024;
		private string _Path;
		public virtual string Path
		{
			get { return _Path; }
			set { SetField(ref _Path, StringUtils.NormalizeUsingPreferredForm(value)); }
		}

		private BackupPlanPathNode _PathNode;
		public virtual BackupPlanPathNode PathNode
		{
			get { return _PathNode; }
			set { SetField(ref _PathNode, value); }
		}

		public const int VersionMaxLen = 14; // Enough to hold a string formatted as `BackupedFile.VersionFormat`.
		private string _Version;
		public virtual string Version
		{
			get { return _Version; }
			set { SetField(ref _Version, value); }
		}
	}
}
