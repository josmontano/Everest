/* 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using Teltec.Everest.Data.DAO;
using Teltec.Everest.Data.FileSystem;
using Teltec.Everest.Data.Models;

namespace Teltec.Everest.App.Controls
{
	public sealed class FileSystemTreeNodeData : EntryTreeNodeData
	{
		public FileSystemTreeNodeData()
		{
		}

		public FileSystemTreeNodeData(EntryInfo infoObject)
		{
			InfoObject = infoObject;
		}

		protected override void UpdateProperties()
		{
			switch (Type)
			{
				default:
					throw new ArgumentException("Unhandled TypeEnum", "type");
				case TypeEnum.FILE:
				case TypeEnum.FOLDER:
				case TypeEnum.DRIVE:
					Path = InfoObject.Path;
					break;
			}
		}
	}

	public static class FileSystemTreeNodeDataExtensions
	{
		// Convert collection of `FileSystemTreeView.TreeNodeTag` to `BackupPlanSourceEntry`.
		public static List<BackupPlanSourceEntry> ToBackupPlanSourceEntry(
			this Dictionary<string, FileSystemTreeNodeData> dataDict, BackupPlan plan, BackupPlanSourceEntryRepository dao)
		{
			List<BackupPlanSourceEntry> sources = new List<BackupPlanSourceEntry>(dataDict.Count);
			foreach (var entry in dataDict)
			{
				FileSystemTreeNodeData data = entry.Value;
				BackupPlanSourceEntry source = null;
				if (data.Id != null)
					source = dao.Get(data.Id as long?);
				else
					source = new BackupPlanSourceEntry();
				source.BackupPlan = plan;
				source.Type = data.ToEntryType();
				source.Path = data.Path;
				sources.Add(source);
			}
			return sources;
		}
	}
}
