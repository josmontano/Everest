﻿/* 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Teltec.Everest.App.Controls;
using Teltec.Everest.Data.DAO;
using Teltec.Common.Extensions;
using Teltec.Storage.Versioning;
using Models = Teltec.Everest.Data.Models;

namespace Teltec.Everest.App.Forms.RestorePlan
{
	public partial class RestorePlanSelectSourceForm : Teltec.Forms.Wizard.WizardForm
	{
		private readonly RestorePlanSourceEntryRepository _dao = new RestorePlanSourceEntryRepository();

		public RestorePlanSelectSourceForm()
		{
			InitializeComponent();
			loadingPanel.Dock = DockStyle.Fill;
			tvFiles.ExpandFetchStarted += (object sender, EventArgs e) =>
			{
				//loadingPanel.Visible = true;
			};
			tvFiles.ExpandFetchEnded += (object sender, EventArgs e) =>
			{
				//loadingPanel.Visible = false;
			};

			this.ModelChangedEvent += (object sender, Teltec.Forms.Wizard.WizardForm.ModelChangedEventArgs e) =>
			{
				Models.RestorePlan plan = e.Model as Models.RestorePlan;
				tvFiles.StorageAccount = plan.StorageAccount;
				// Lazily select nodes that match entries from `plan.SelectedSources`.
				tvFiles.CheckedDataSource = RestorePlanSelectedSourcesToCheckedDataSource(plan);
			};
		}

		private Dictionary<string, BackupPlanTreeNodeData> RestorePlanSelectedSourcesToCheckedDataSource(Models.RestorePlan plan)
		{
			return plan.SelectedSources.ToDictionary(
				e => e.Path,
				e => new BackupPlanTreeNodeData
				{
					Id = e.Id,
					StorageAccount = plan.StorageAccount,
					Type = Models.EntryTypeExtensions.ToTypeEnum(e.Type),
					Path = e.Path,
					State = Teltec.Common.Controls.CheckState.Checked,
					InfoObject = new EntryInfo(Models.EntryTypeExtensions.ToTypeEnum(e.Type), e.PathNode.Name, e.Path, new FileVersion { Version = e.Version })
				}
			);
		}

		protected override bool IsValid()
		{
			Models.RestorePlan plan = Model as Models.RestorePlan;
			bool didSelectSource = plan.SelectedSources != null && plan.SelectedSources.Count > 0;
			return didSelectSource;
		}

		protected override void OnBeforeNextOrFinish(object sender, CancelEventArgs e)
		{
			Models.RestorePlan plan = Model as Models.RestorePlan;

			ICollection<Models.RestorePlanSourceEntry> entries = tvFiles.GetCheckedTagData().ToRestorePlanSourceEntry(plan, _dao);
			plan.SelectedSources.Clear();
			plan.SelectedSources.AddRange(entries);

			if (DoValidate && !IsValid())
			{
				e.Cancel = true;
				this.ShowErrorMessage("Please, select a source.");
			}
			base.OnBeforeNextOrFinish(sender, e);
		}
	}
}
