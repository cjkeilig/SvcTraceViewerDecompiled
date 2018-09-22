using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[ObjectStateMachine(typeof(TraceFilterDisableState), true, typeof(TraceFilterSearchNoneState))]
	[ObjectStateMachine(typeof(TraceFilterEnableState), false, typeof(TraceFilterSearchNoneState))]
	[ObjectStateTransfer("EmptyProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("NoFileProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("FileLoadingProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("FileRemovingProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("TraceLoadingProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("FileReloadingProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("IdleProjectState", "TraceFilterEnableState")]
	internal class TraceDetailInfoControl : UserControl, IStateAwareObject
	{
		private enum TabFocusIndex
		{
			Toolbar,
			MainPanel,
			AdvancedInfoPanel
		}

		private StateMachineController objectStateController;

		private TraceViewerForm parentForm;

		private IUserInterfaceProvider userIP;

		private IErrorReport errorReport;

		private TraceRecord currentTraceRecord;

		private ToolStripDropDownButton extensionMenu;

		private ToolStripMenuItem showBasicsMenuItem;

		private ToolStripMenuItem showDiagMenuItem;

		private IContainer components;

		private Panel mainPanel;

		private ToolStrip toolStrip;

		private Panel advancedInfoPanel;

		public TraceDetailInfoControl()
		{
			InitializeComponent();
			objectStateController = new StateMachineController(this);
		}

		internal void Initialize(TraceViewerForm parent, IUserInterfaceProvider userIP, IErrorReport errorReport)
		{
			parentForm = parent;
			this.userIP = userIP;
			this.errorReport = errorReport;
			extensionMenu = new ToolStripDropDownButton();
			extensionMenu.Text = SR.GetString("FV_Options");
			extensionMenu.ShowDropDownArrow = true;
			extensionMenu.ToolTipText = SR.GetString("FV_OptionsTip");
			showBasicsMenuItem = new ToolStripMenuItem(SR.GetString("FV_BasicInfoOption"));
			showBasicsMenuItem.ToolTipText = SR.GetString("FV_BasicInfoOptionTip");
			showBasicsMenuItem.Checked = true;
			showBasicsMenuItem.CheckOnClick = true;
			showBasicsMenuItem.Click += changeOptionsMenuItem_Click;
			showDiagMenuItem = new ToolStripMenuItem(SR.GetString("FV_DiagInfoOption"));
			showDiagMenuItem.ToolTipText = SR.GetString("FV_DiagInfoOptionTip");
			showDiagMenuItem.Checked = true;
			showDiagMenuItem.CheckOnClick = true;
			showDiagMenuItem.Click += changeOptionsMenuItem_Click;
			extensionMenu.DropDownItems.Add(showBasicsMenuItem);
			extensionMenu.DropDownItems.Add(showDiagMenuItem);
			toolStrip.Items.Add(extensionMenu);
		}

		private void ChangeOptionMenuStatus()
		{
			if (!showBasicsMenuItem.Checked || !showDiagMenuItem.Checked)
			{
				extensionMenu.ForeColor = Utilities.GetColor(ApplicationColors.HightlightedMenuColor);
			}
			else
			{
				extensionMenu.ForeColor = SystemColors.ControlText;
			}
		}

		private void changeOptionsMenuItem_Click(object sender, EventArgs e)
		{
			ReloadTraceDetailedInfo();
			ChangeOptionMenuStatus();
		}

		void IStateAwareObject.PreStateSwitch(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		void IStateAwareObject.PostStateSwitch(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		void IStateAwareObject.StateSwitchSuccess(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		void IStateAwareObject.StateSwitchFailed(ObjectStateBase fromState, ObjectStateBase toState, ObjectStateSwitchFailReason reason)
		{
		}

		internal void CleanUp()
		{
			currentTraceRecord = null;
			advancedInfoPanel.Controls.Clear();
		}

		private void ReloadTraceInfo()
		{
			if (currentTraceRecord != null)
			{
				IAdvancedTraceInfoProvider advancedTraceInfoProvider = TraceDetailInfoManager.GetInstance().GetAdvancedTraceInfoProvider(currentTraceRecord);
				Control advancedTraceInfoControl = advancedTraceInfoProvider.GetAdvancedTraceInfoControl();
				if (advancedTraceInfoControl != null)
				{
					advancedTraceInfoControl.SuspendLayout();
					try
					{
						advancedTraceInfoProvider.ReloadTrace(currentTraceRecord, new TraceDetailInfoControlParam(showBasicsMenuItem.Checked, showDiagMenuItem.Checked));
					}
					catch (TraceViewerException ex)
					{
						throw ex;
					}
					finally
					{
						advancedTraceInfoControl.ResumeLayout();
					}
					advancedTraceInfoControl.Dock = DockStyle.Fill;
					advancedInfoPanel.Controls.Add(advancedTraceInfoControl);
				}
			}
		}

		private void ReloadTraceDetailedInfo()
		{
			try
			{
				ReloadTraceDetailedInfo(currentTraceRecord);
			}
			catch (TraceViewerException exception)
			{
				errorReport.ReportErrorToUser(exception);
			}
		}

		internal void ReloadTraceDetailedInfo(TraceRecord trace)
		{
			CleanUp();
			if (trace != null)
			{
				currentTraceRecord = trace;
				ReloadTraceInfo();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			mainPanel = new System.Windows.Forms.Panel();
			advancedInfoPanel = new System.Windows.Forms.Panel();
			toolStrip = new System.Windows.Forms.ToolStrip();
			mainPanel.SuspendLayout();
			SuspendLayout();
			mainPanel.AutoScroll = true;
			mainPanel.BackColor = System.Drawing.SystemColors.Window;
			mainPanel.Controls.Add(advancedInfoPanel);
			mainPanel.Controls.Add(toolStrip);
			mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			mainPanel.Location = new System.Drawing.Point(0, 0);
			mainPanel.Name = "mainPanel";
			mainPanel.Size = new System.Drawing.Size(600, 400);
			mainPanel.TabIndex = 1;
			advancedInfoPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			advancedInfoPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			advancedInfoPanel.Location = new System.Drawing.Point(0, 25);
			advancedInfoPanel.Name = "advancedInfoPanel";
			advancedInfoPanel.Size = new System.Drawing.Size(600, 375);
			advancedInfoPanel.TabIndex = 2;
			toolStrip.Location = new System.Drawing.Point(0, 0);
			toolStrip.Name = "toolStrip";
			toolStrip.Size = new System.Drawing.Size(600, 25);
			toolStrip.TabIndex = 0;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			BackColor = System.Drawing.SystemColors.Window;
			base.Controls.Add(mainPanel);
			base.Name = "TraceDetailInfoControl";
			base.Size = new System.Drawing.Size(600, 400);
			mainPanel.ResumeLayout(performLayout: false);
			mainPanel.PerformLayout();
			ResumeLayout(performLayout: false);
		}
	}
}
