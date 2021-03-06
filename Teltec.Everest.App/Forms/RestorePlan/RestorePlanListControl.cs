/* 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using NLog;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Teltec.Everest.Data.DAO;
using Models = Teltec.Everest.Data.Models;

namespace Teltec.Everest.App.Forms.RestorePlan
{
	public partial class RestorePlanListControl : UserControl
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private readonly RestorePlanRepository dao = new RestorePlanRepository();

		public RestorePlanListControl()
		{
			InitializeComponent();
			this.layoutPanel.ControlAdded += layoutPanel_ControlAdded;
		}

		void layoutPanel_ControlAdded(object sender, ControlEventArgs e)
		{
			this.layoutPanel.SetFlowBreak(e.Control, true);
		}

		public bool ControlsAlreadyContainControlForPlan(Models.RestorePlan plan)
		{
			foreach (Control ctrl in this.layoutPanel.Controls)
			{
				if (!(ctrl is RestorePlanViewControl))
					continue;

				RestorePlanViewControl obj = ctrl as RestorePlanViewControl;
				Models.RestorePlan objPlan = obj.Model as Models.RestorePlan;

				if (objPlan.Id.Equals(plan.Id))
					return true;
			}
			return false;
		}

		public void RemoveAllExceptRunning()
		{
			List<Control> toBeRemoved = new List<Control>();

			// Remove all that are not running.
			foreach (Control ctrl in this.layoutPanel.Controls)
			{
				if (!(ctrl is RestorePlanViewControl))
					continue;

				RestorePlanViewControl obj = ctrl as RestorePlanViewControl;
				if (!obj.OperationIsRunning)
					toBeRemoved.Add(ctrl);
			}

			// Remove them.
			foreach (Control ctrl in toBeRemoved)
				this.layoutPanel.Controls.Remove(ctrl);
		}

		public void LoadPlans()
		{
			RemoveAllExceptRunning();

			var plans = dao.GetAllActive();

			foreach (var plan in plans)
			{
				if (ControlsAlreadyContainControlForPlan(plan))
					continue;

				RestorePlanViewControl viewControl = new RestorePlanViewControl();
				viewControl.Model = plan;
				viewControl.Deleted += (object sender, EventArgs e) =>
				{
					RestorePlanViewControl control = sender as RestorePlanViewControl;
					layoutPanel.Controls.Remove(control);
				};
				this.layoutPanel.Controls.Add(viewControl);
			}
		}

		public void RefreshPlans()
		{
			LoadPlans();
		}
	}
}
