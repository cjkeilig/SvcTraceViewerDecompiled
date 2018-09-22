using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[ObjectStateMachine(typeof(AdjustEnableState), true, typeof(AdjustDisableState))]
	[ObjectStateMachine(typeof(AdjustDisableState), false, typeof(AdjustEnableState))]
	[ObjectStateTransfer("TraceLoadingProjectState", "AdjustDisableState")]
	[ObjectStateTransfer("FileLoadingProjectState", "AdjustDisableState")]
	[ObjectStateTransfer("FileRemovingProjectState", "AdjustDisableState")]
	[ObjectStateTransfer("FileReloadingProjectState", "AdjustDisableState")]
	[ObjectStateTransfer("EmptyProjectState", "AdjustEnableState")]
	[ObjectStateTransfer("IdleProjectState", "AdjustEnableState")]
	internal class FilePartialLoadingStrip : UserControl, IStateAwareObject
	{
		public delegate void TimeRangeChanged(DateTime start, DateTime end);

		private StateMachineController objectStateController;

		private TimeRangeChanged timeRangeChangedCallback;

		private IContainer components;

		[UIToolStripItemEnablePropertyState(new string[]
		{
			"AdjustEnableState"
		})]
		private ToolStripButton btnAdjust;

		[UIControlEnablePropertyState(new string[]
		{
			"AdjustEnableState"
		})]
		private DTRangeControl rangeControl;

		private ToolStrip hostStrip;

		public StateMachineController ObjectStateController => objectStateController;

		public TimeRangeChanged TimeRangeChangedCallback
		{
			get
			{
				return timeRangeChangedCallback;
			}
			set
			{
				timeRangeChangedCallback = value;
			}
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

		public FilePartialLoadingStrip()
		{
			InitializeComponent();
			objectStateController = new StateMachineController(this);
		}

		public void RefreshTimeRange(DateTime start, DateTime end)
		{
			if (!(start > end))
			{
				rangeControl.RefreshTimeRange(start, end);
			}
		}

		public void RefreshSelectedTimeRange(DateTime start, DateTime end)
		{
			if (!(start > end))
			{
				rangeControl.RefreshSelectedTimeRange(start, end);
			}
		}

		private void btnAdjust_Click(object sender, EventArgs e)
		{
			if (timeRangeChangedCallback != null)
			{
				try
				{
					timeRangeChangedCallback(rangeControl.StartDateTime, rangeControl.EndDateTime);
				}
				catch (Exception e2)
				{
					ExceptionManager.GeneralExceptionFilter(e2);
				}
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
			btnAdjust = new System.Windows.Forms.ToolStripButton();
			rangeControl = new Microsoft.Tools.ServiceModel.TraceViewer.DTRangeControl();
			hostStrip = new System.Windows.Forms.ToolStrip();
			SuspendLayout();
			btnAdjust.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			btnAdjust.Name = "btnAdjust";
			btnAdjust.Size = new System.Drawing.Size(75, 22);
			btnAdjust.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("PL_Adjust");
			btnAdjust.Click += new System.EventHandler(btnAdjust_Click);
			rangeControl.Location = new System.Drawing.Point(16, 2);
			rangeControl.Name = "rangeControl";
			rangeControl.Size = new System.Drawing.Size(680, 20);
			rangeControl.TabIndex = 2;
			System.Windows.Forms.ToolStripControlHost toolStripControlHost = new System.Windows.Forms.ToolStripControlHost(rangeControl);
			hostStrip.Location = new System.Drawing.Point(0, 0);
			hostStrip.Name = "hostStrip";
			hostStrip.Size = new System.Drawing.Size(800, 25);
			hostStrip.TabIndex = 0;
			hostStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[2]
			{
				toolStripControlHost,
				btnAdjust
			});
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(hostStrip);
			base.Name = "FilePartialLoadingStrip";
			base.Size = new System.Drawing.Size(700, 25);
			ResumeLayout(performLayout: false);
		}
	}
}
