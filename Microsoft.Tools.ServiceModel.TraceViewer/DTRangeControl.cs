using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[ObjectStateMachine(typeof(NonOpsState), true, null)]
	[ObjectStateMachine(typeof(AdjustingTimeRangeByScroller), false, typeof(NonOpsState))]
	[ObjectStateMachine(typeof(AdjustingTimeRangeByDTPicker), false, typeof(NonOpsState))]
	[ObjectStateMachine(typeof(RefreshState), false, typeof(NonOpsState))]
	internal class DTRangeControl : UserControl, IStateAwareObject
	{
		public delegate void TimeRangeChange(DateTime start, DateTime end);

		private enum TabFocusIndex
		{
			StartTimeLabel,
			StartTimePicker,
			EndTimeLabel,
			EndTimePicker
		}

		private StateMachineController objectStateController;

		private const int STEP_COUNT = 280;

		private const int TICK_TO_SECOND = 10000000;

		private int startStep;

		private int endStep = 280;

		private bool isEventInitialized;

		private TimeRangeChange timeRangeChangeCallback;

		private IContainer components;

		[UIControlSpecialStateEventHandler(new string[]
		{
			"NonOpsState"
		}, "startDateTimePicker_ValueChanged", "ValueChanged", "startDateTimePicker_ValueChanged")]
		[UIControlEnablePropertyState(new string[]
		{
			"NonOpsState",
			"AdjustingTimeRangeByDTPicker"
		})]
		private DateTimePicker startDateTimePicker;

		[UIControlSpecialStateEventHandler(new string[]
		{
			"NonOpsState"
		}, "endDateTimePicker_ValueChanged", "ValueChanged", "endDateTimePicker_ValueChanged")]
		[UIControlEnablePropertyState(new string[]
		{
			"NonOpsState",
			"AdjustingTimeRangeByDTPicker"
		})]
		private DateTimePicker endDateTimePicker;

		[UIControlEnablePropertyState(new string[]
		{
			"NonOpsState",
			"AdjustingTimeRangeByScroller"
		})]
		private Panel timeRangePanel;

		private Panel opsPanel;

		[UIControlSpecialStateEventHandler(new string[]
		{
			"NonOpsState",
			"AdjustingTimeRangeByScroller"
		}, "rightSplitter_SplitterMoving", "SplitterMoving", "rightSplitter_SplitterMoving")]
		[UIControlSpecialStateEventHandler(new string[]
		{
			"AdjustingTimeRangeByScroller"
		}, "rightSplitter_SplitterMoved", "SplitterMoved", "rightSplitter_SplitterMoved")]
		private Splitter rightSplitter;

		[UIControlSpecialStateEventHandler(new string[]
		{
			"NonOpsState",
			"AdjustingTimeRangeByScroller"
		}, "leftSplitter_SplitterMoving", "SplitterMoving", "leftSplitter_SplitterMoving")]
		[UIControlSpecialStateEventHandler(new string[]
		{
			"AdjustingTimeRangeByScroller"
		}, "leftSplitter_SplitterMoved", "SplitterMoved", "leftSplitter_SplitterMoved")]
		private Splitter leftSplitter;

		private Panel leftEmptyPanel;

		private Panel selelctedPanel;

		private Panel rightEmptyPanel;

		private Label lblStartDateTime;

		private Label lblEndDateTime;

		public StateMachineController ObjectStateController => objectStateController;

		public TimeRangeChange TimeRangeChangeCallback
		{
			get
			{
				return timeRangeChangeCallback;
			}
			set
			{
				timeRangeChangeCallback = value;
			}
		}

		public DateTime MaxDateTime => endDateTimePicker.MaxDate;

		public DateTime MinDateTime => startDateTimePicker.MinDate;

		public DateTime StartDateTime => startDateTimePicker.Value;

		public DateTime EndDateTime => endDateTimePicker.Value;

		public override string Text
		{
			get
			{
				return lblStartDateTime.Text + lblEndDateTime.Text;
			}
			set
			{
				base.Text = value;
			}
		}

		public DTRangeControl()
		{
			InitializeComponent();
			InitializeEventHandlers();
			objectStateController = new StateMachineController(this);
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

		private void InternalEventTriggerHelper()
		{
			if (timeRangeChangeCallback != null)
			{
				try
				{
					timeRangeChangeCallback(StartDateTime, EndDateTime);
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
				}
			}
		}

		public void RefreshTimeRange(DateTime start, DateTime end)
		{
			if (start > end)
			{
				start = end;
			}
			ObjectStateController.SwitchState("RefreshState");
			startDateTimePicker.MaxDate = DateTimePicker.MaximumDateTime;
			startDateTimePicker.MinDate = DateTimePicker.MinimumDateTime;
			endDateTimePicker.MaxDate = DateTimePicker.MaximumDateTime;
			endDateTimePicker.MinDate = DateTimePicker.MinimumDateTime;
			DateTime dateTime = Utilities.RoundDateTimeToSecond(start);
			DateTime dateTime2 = Utilities.RoundDateTimeToSecond(end);
			if (dateTime >= DateTimePicker.MinimumDateTime && dateTime <= DateTimePicker.MaximumDateTime && dateTime2 >= DateTimePicker.MinimumDateTime && dateTime2 <= DateTimePicker.MaximumDateTime)
			{
				startDateTimePicker.MaxDate = dateTime2;
				startDateTimePicker.MinDate = dateTime;
				endDateTimePicker.MaxDate = startDateTimePicker.MaxDate;
				endDateTimePicker.MinDate = startDateTimePicker.MinDate;
			}
			ObjectStateController.SwitchState();
		}

		public void RefreshSelectedTimeRange(DateTime start, DateTime end)
		{
			if (start > end)
			{
				start = end;
			}
			ObjectStateController.SwitchState("RefreshState");
			RefreshStartDateTime(start);
			RefreshEndDateTime(end);
			RefreshDateTimeRange(StartDateTime, EndDateTime);
			ObjectStateController.SwitchState();
		}

		private void InitializeEventHandlers()
		{
			if (!isEventInitialized)
			{
				startDateTimePicker.ValueChanged += startDateTimePicker_ValueChanged;
				endDateTimePicker.ValueChanged += endDateTimePicker_ValueChanged;
				rightSplitter.SplitterMoved += rightSplitter_SplitterMoved;
				rightSplitter.SplitterMoving += rightSplitter_SplitterMoving;
				leftSplitter.SplitterMoved += leftSplitter_SplitterMoved;
				leftSplitter.SplitterMoving += leftSplitter_SplitterMoving;
				isEventInitialized = true;
			}
		}

		private DateTime CalculateDateTimeByStepValue(int value)
		{
			double num = (double)value / 280.0;
			double num2 = (MaxDateTime - MinDateTime).TotalSeconds;
			try
			{
				return MinDateTime.AddTicks((long)(num2 * num) * 10000000);
			}
			catch (ArgumentOutOfRangeException)
			{
				if (value > 0)
				{
					return MaxDateTime;
				}
				return MinDateTime;
			}
		}

		private int CalculateStepValueByDateTime(DateTime dt)
		{
			DateTime d = dt;
			if (dt > MaxDateTime)
			{
				d = MaxDateTime;
			}
			if (dt < MinDateTime)
			{
				d = MinDateTime;
			}
			int num = (int)(MaxDateTime - MinDateTime).TotalSeconds;
			double num2 = (double)(int)(d - MinDateTime).TotalSeconds / (double)num;
			if (num2 < 0.0)
			{
				return 0;
			}
			if (num2 > 1.0)
			{
				return 280;
			}
			return (int)(num2 * 280.0);
		}

		private void PaintDateTimeRange()
		{
			leftEmptyPanel.Size = new Size(startStep, 16);
			opsPanel.Size = new Size(endStep + 3, 16);
		}

		private void RefreshDateTimeRange(DateTime start, DateTime end)
		{
			DateTime dateTime = (start <= MinDateTime) ? MinDateTime : Utilities.RoundDateTimeToSecond(start);
			DateTime dateTime2 = (end >= MaxDateTime) ? MaxDateTime : Utilities.RoundDateTimeToSecond(end);
			startStep = CalculateStepValueByDateTime(dateTime);
			endStep = CalculateStepValueByDateTime(dateTime2);
			if (dateTime == dateTime2)
			{
				startStep = endStep;
			}
			PaintDateTimeRange();
		}

		private void RefreshStartDateTime(DateTime dateTime)
		{
			DateTime dateTime2 = Utilities.RoundDateTimeToSecond(dateTime);
			startDateTimePicker.Value = ((dateTime2 >= MinDateTime && dateTime2 <= MaxDateTime) ? dateTime2 : MinDateTime);
		}

		private void RefreshEndDateTime(DateTime dateTime)
		{
			DateTime dateTime2 = Utilities.RoundDateTimeToSecond(dateTime);
			endDateTimePicker.Value = ((dateTime2 >= MinDateTime && dateTime2 <= MaxDateTime) ? dateTime2 : MaxDateTime);
		}

		private void startDateTimePicker_ValueChanged(object sender, EventArgs e)
		{
			ObjectStateController.SwitchState("AdjustingTimeRangeByDTPicker");
			if (StartDateTime > EndDateTime)
			{
				startDateTimePicker.Value = endDateTimePicker.Value;
			}
			RefreshDateTimeRange(StartDateTime, EndDateTime);
			InternalEventTriggerHelper();
			ObjectStateController.SwitchState();
		}

		private void endDateTimePicker_ValueChanged(object sender, EventArgs e)
		{
			ObjectStateController.SwitchState("AdjustingTimeRangeByDTPicker");
			if (StartDateTime > EndDateTime)
			{
				endDateTimePicker.Value = startDateTimePicker.Value;
			}
			RefreshDateTimeRange(StartDateTime, EndDateTime);
			InternalEventTriggerHelper();
			ObjectStateController.SwitchState();
		}

		private void leftSplitter_SplitterMoved(object sender, SplitterEventArgs e)
		{
			InternalEventTriggerHelper();
			ObjectStateController.SwitchState();
		}

		private void rightSplitter_SplitterMoved(object sender, SplitterEventArgs e)
		{
			InternalEventTriggerHelper();
			ObjectStateController.SwitchState();
		}

		private void leftSplitter_SplitterMoving(object sender, SplitterEventArgs e)
		{
			try
			{
				ObjectStateController.SwitchState("AdjustingTimeRangeByScroller");
				int splitX = e.SplitX;
				DateTime dateTime = CalculateDateTimeByStepValue(splitX);
				RefreshStartDateTime(dateTime);
			}
			catch (Exception e2)
			{
				ExceptionManager.GeneralExceptionFilter(e2);
			}
		}

		private void rightSplitter_SplitterMoving(object sender, SplitterEventArgs e)
		{
			try
			{
				ObjectStateController.SwitchState("AdjustingTimeRangeByScroller");
				if (e.SplitX < leftEmptyPanel.Size.Width + 3)
				{
					e.SplitX = leftEmptyPanel.Size.Width + 3;
				}
				if (e.SplitX == leftEmptyPanel.Size.Width + 3)
				{
					RefreshEndDateTime(StartDateTime);
				}
				else
				{
					int value = e.SplitX - 3;
					DateTime dateTime = CalculateDateTimeByStepValue(value);
					RefreshEndDateTime(dateTime);
				}
			}
			catch (Exception e2)
			{
				ExceptionManager.GeneralExceptionFilter(e2);
			}
		}

		private void DTRangeControl_EnabledChanged(object sender, EventArgs e)
		{
			if (!base.Enabled)
			{
				startDateTimePicker.Enabled = false;
				endDateTimePicker.Enabled = false;
				timeRangePanel.Enabled = false;
			}
			else
			{
				startDateTimePicker.Enabled = true;
				endDateTimePicker.Enabled = true;
				timeRangePanel.Enabled = true;
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
			startDateTimePicker = new System.Windows.Forms.DateTimePicker();
			endDateTimePicker = new System.Windows.Forms.DateTimePicker();
			timeRangePanel = new System.Windows.Forms.Panel();
			rightEmptyPanel = new System.Windows.Forms.Panel();
			rightSplitter = new System.Windows.Forms.Splitter();
			opsPanel = new System.Windows.Forms.Panel();
			selelctedPanel = new System.Windows.Forms.Panel();
			leftSplitter = new System.Windows.Forms.Splitter();
			leftEmptyPanel = new System.Windows.Forms.Panel();
			lblStartDateTime = new System.Windows.Forms.Label();
			lblEndDateTime = new System.Windows.Forms.Label();
			timeRangePanel.SuspendLayout();
			opsPanel.SuspendLayout();
			SuspendLayout();
			lblStartDateTime.AutoSize = false;
			lblStartDateTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lblStartDateTime.Name = "lblStartDateTime";
			lblStartDateTime.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("PL_StartTime");
			lblStartDateTime.Size = new System.Drawing.Size(50, 20);
			lblStartDateTime.Location = new System.Drawing.Point(0, 0);
			lblStartDateTime.TabIndex = 0;
			startDateTimePicker.CustomFormat = "yyyy-MM-dd  HH:mm:ss";
			startDateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			startDateTimePicker.Location = new System.Drawing.Point(50, 0);
			startDateTimePicker.Name = "startDateTimePicker";
			startDateTimePicker.Size = new System.Drawing.Size(140, 20);
			startDateTimePicker.TabIndex = 1;
			lblEndDateTime.AutoSize = false;
			lblEndDateTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lblEndDateTime.Name = "lblEndDateTime";
			lblEndDateTime.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("PL_EndTime");
			lblEndDateTime.Size = new System.Drawing.Size(50, 20);
			lblEndDateTime.Location = new System.Drawing.Point(490, 0);
			lblEndDateTime.TabIndex = 2;
			endDateTimePicker.CustomFormat = "yyyy-MM-dd  HH:mm:ss";
			endDateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			endDateTimePicker.Location = new System.Drawing.Point(540, 0);
			endDateTimePicker.Name = "endDateTimePicker";
			endDateTimePicker.Size = new System.Drawing.Size(140, 20);
			endDateTimePicker.TabIndex = 3;
			timeRangePanel.BackColor = Microsoft.Tools.ServiceModel.TraceViewer.Utilities.GetColor(Microsoft.Tools.ServiceModel.TraceViewer.ApplicationColors.GradientInactiveCaption);
			timeRangePanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			timeRangePanel.Controls.Add(rightEmptyPanel);
			timeRangePanel.Controls.Add(rightSplitter);
			timeRangePanel.Controls.Add(opsPanel);
			timeRangePanel.Location = new System.Drawing.Point(196, 2);
			timeRangePanel.Name = "timeRangePanel";
			timeRangePanel.Size = new System.Drawing.Size(288, 16);
			timeRangePanel.TabStop = false;
			rightEmptyPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			rightEmptyPanel.Location = new System.Drawing.Point(227, 0);
			rightEmptyPanel.Name = "rightEmptyPanel";
			rightEmptyPanel.Size = new System.Drawing.Size(79, 14);
			rightEmptyPanel.TabStop = false;
			rightSplitter.BackColor = System.Drawing.Color.Gold;
			rightSplitter.Location = new System.Drawing.Point(224, 0);
			rightSplitter.MinExtra = 0;
			rightSplitter.MinSize = 0;
			rightSplitter.Name = "rightSplitter";
			rightSplitter.Size = new System.Drawing.Size(3, 14);
			rightSplitter.TabStop = false;
			opsPanel.Controls.Add(selelctedPanel);
			opsPanel.Controls.Add(leftSplitter);
			opsPanel.Controls.Add(leftEmptyPanel);
			opsPanel.Dock = System.Windows.Forms.DockStyle.Left;
			opsPanel.Location = new System.Drawing.Point(0, 0);
			opsPanel.Name = "opsPanel";
			opsPanel.Size = new System.Drawing.Size(224, 14);
			opsPanel.TabStop = false;
			selelctedPanel.BackColor = Microsoft.Tools.ServiceModel.TraceViewer.Utilities.GetColor(Microsoft.Tools.ServiceModel.TraceViewer.ApplicationColors.Highlight);
			selelctedPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			selelctedPanel.Location = new System.Drawing.Point(23, 0);
			selelctedPanel.Name = "selelctedPanel";
			selelctedPanel.Size = new System.Drawing.Size(201, 14);
			selelctedPanel.TabStop = false;
			leftSplitter.BackColor = System.Drawing.Color.Gold;
			leftSplitter.Location = new System.Drawing.Point(20, 0);
			leftSplitter.MinExtra = 0;
			leftSplitter.MinSize = 0;
			leftSplitter.Name = "leftSplitter";
			leftSplitter.Size = new System.Drawing.Size(3, 14);
			leftSplitter.TabStop = false;
			leftEmptyPanel.Dock = System.Windows.Forms.DockStyle.Left;
			leftEmptyPanel.Location = new System.Drawing.Point(0, 0);
			leftEmptyPanel.Name = "leftEmptyPanel";
			leftEmptyPanel.Size = new System.Drawing.Size(20, 14);
			leftEmptyPanel.TabStop = false;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			BackColor = System.Drawing.Color.Transparent;
			base.Controls.Add(timeRangePanel);
			base.Controls.Add(lblStartDateTime);
			base.Controls.Add(startDateTimePicker);
			base.Controls.Add(lblEndDateTime);
			base.Controls.Add(endDateTimePicker);
			base.Name = "DTRangeControl";
			base.Size = new System.Drawing.Size(680, 20);
			base.EnabledChanged += new System.EventHandler(DTRangeControl_EnabledChanged);
			timeRangePanel.ResumeLayout(performLayout: false);
			opsPanel.ResumeLayout(performLayout: false);
			ResumeLayout(performLayout: false);
		}
	}
}
