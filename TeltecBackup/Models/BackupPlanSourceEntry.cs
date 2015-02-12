﻿using System;
using System.Collections.Generic;
using Teltec.Backup.DAO;
using Teltec.Common;
using Teltec.Common.Forms;

namespace Teltec.Backup.Models
{
	public class BackupPlanSourceEntry : BaseEntity<Int64?>
	{
		public enum EntryType
		{
			DRIVE = 1,
			FOLDER = 2,
			FILE = 3,
		}

		//public BackupPlanSourceEntry()
		//{
		//}

		//public BackupPlanSourceEntry(BackupPlan plan, EntryType type, string path) : this()
		//{
		//	BackupPlan = plan;
		//	Type = type;
		//	Path = path;
		//}

		//public BackupPlanSourceEntry(BackupPlan plan, FileSystemTreeNodeTag tag)
		//	: this(plan, tag.ToEntryType(), tag.Path)
		//{
		//}

		private Int64? _Id;
		public virtual Int64? Id
		{
			get { return _Id; }
			set { SetField(ref _Id, value); }
		}

		private BackupPlan _BackupPlan;
		public virtual BackupPlan BackupPlan
		{
			get { return _BackupPlan; }
			set { SetField(ref _BackupPlan, value); }
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
			set { SetField(ref _Path, value); }
		}
	}

	public static class EntryTypeExtensions
	{
		public static FileSystemTreeNodeTag.InfoType ToInfoType(
			this BackupPlanSourceEntry.EntryType obj)
		{
			switch (obj)
			{
				case BackupPlanSourceEntry.EntryType.DRIVE:
					return FileSystemTreeNodeTag.InfoType.DRIVE;
				case BackupPlanSourceEntry.EntryType.FOLDER:
					return FileSystemTreeNodeTag.InfoType.FOLDER;
				case BackupPlanSourceEntry.EntryType.FILE:
					return FileSystemTreeNodeTag.InfoType.FILE;
				default:
					throw new ArgumentException("Unhandled EntryType", "obj");
			}
		}
	}

	public static class TreeNodeTagExtensions
	{
		// Convert collection of `FileSystemTreeView.TreeNodeTag` to `BackupPlanSourceEntry`.
		public static List<BackupPlanSourceEntry> ToBackupPlanSourceEntry(
			this List<FileSystemTreeNodeTag> tags, BackupPlan plan, BackupPlanSourceEntryRepository dao)
		{
			List<BackupPlanSourceEntry> entries = new List<BackupPlanSourceEntry>(tags.Count);
			foreach (Teltec.Common.Forms.FileSystemTreeNodeTag tag in tags)
			{
				BackupPlanSourceEntry entry = null;
				if (tag.Id != null)
					entry = dao.Get(tag.Id as long?);
				else
					entry = new BackupPlanSourceEntry();
				entry.BackupPlan = plan;
				entry.Type = tag.ToEntryType();
				entry.Path = tag.Path;
				entries.Add(entry);
			}
			return entries;
		}

		public static BackupPlanSourceEntry.EntryType ToEntryType(this FileSystemTreeNodeTag tag)
		{
			switch (tag.Type)
			{
				default: throw new ArgumentException("type", "Unhandled TreeNodeTag.InfoType");
				case FileSystemTreeNodeTag.InfoType.DRIVE:
					return BackupPlanSourceEntry.EntryType.DRIVE;
				case FileSystemTreeNodeTag.InfoType.FOLDER:
					return BackupPlanSourceEntry.EntryType.FOLDER;
				case FileSystemTreeNodeTag.InfoType.FILE:
					return BackupPlanSourceEntry.EntryType.FILE;
			}
		}
	}
}
