using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[ObjectStateMachine(typeof(TraceFilterDisableState), true, typeof(TraceFilterSearchNoneState))]
	[ObjectStateMachine(typeof(TraceFilterEnableState), false, typeof(TraceFilterSearchNoneState))]
	[ObjectStateMachine(typeof(TraceFilterSearchNoneState), false, null)]
	[ObjectStateMachine(typeof(TraceFilterSearchTimeState), false, null)]
	[ObjectStateMachine(typeof(TraceFilterSearchTextState), false, null)]
	[ObjectStateMachine(typeof(TraceFilterSearchTimeRangeState), false, null)]
	[ObjectStateTransfer("EmptyProjectState", "TraceFilterEnableState")]
	[ObjectStateTransfer("FileLoadingProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("FileRemovingProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("TraceLoadingProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("FileReloadingProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("IdleProjectState", "TraceFilterEnableState")]
	[ObjectStateTransfer("NoFileProjectState", "TraceFilterEnableState")]
	internal class TraceFilterControl : UserControl, IStateAwareObject
	{
		internal delegate void TraceFilterChanged(object o, TraceFilterChangedEventArgs e);

		private StateMachineController objectStateController;

		private IErrorReport errorReport;

		private TraceViewerForm parentForm;

		private TraceDataSource currentDataSource;

		private int lastSelectedSearchInListIndex = -1;

		private Dictionary<int, CustomFilter> customFilterMap;

		private IContainer components;

		private ToolStripContainer filterStripContainer;

		[UIControlEnablePropertyState(new string[]
		{
			"TraceFilterSearchNoneState",
			"TraceFilterSearchTimeState",
			"TraceFilterSearchTextState",
			"TraceFilterSearchTimeRange",
			"TraceFilterEnableState"
		})]
		private ToolStrip toolStripStandard;

		private ToolStripLabel searchInLabel;

		[UIControlEnablePropertyState(new string[]
		{
			"TraceFilterSearchTextState"
		})]
		[UIControlVisiblePropertyState(new string[]
		{
			"TraceFilterDisableState",
			"TraceFilterSearchNoneState",
			"TraceFilterSearchTextState"
		})]
		private ToolStrip lookForFilterStrip;

		private ToolStripLabel lookForLabel;

		private ToolStripTextBox toolStripTextBoxLookFor;

		private ToolStripComboBox searchInList;

		private ToolStripLabel traceLevelLabel;

		private ToolStripComboBox traceLevelList;

		private ToolStripButton toolStripButtonFilterNow;

		private ToolStripButton toolStripButtonRestore;

		[UIControlEnablePropertyState(new string[]
		{
			"TraceFilterSearchTimeState"
		})]
		[UIControlVisiblePropertyState(new string[]
		{
			"TraceFilterSearchTimeState"
		})]
		private ToolStrip timeFilterStrip;

		private ToolStripLabel timeLabel;

		[UIControlEnablePropertyState(new string[]
		{
			"TraceFilterSearchTimeRange"
		})]
		[UIControlVisiblePropertyState(new string[]
		{
			"TraceFilterSearchTimeRange"
		})]
		private ToolStrip timeRangeFilterStrip;

		[UIControlEnablePropertyState(new string[]
		{
			"TraceFilterSearchTimeState"
		})]
		[UIControlVisiblePropertyState(new string[]
		{
			"TraceFilterSearchTimeState"
		})]
		private DateTimePicker dtpTime;

		[UIControlEnablePropertyState(new string[]
		{
			"TraceFilterSearchTimeRange"
		})]
		[UIControlVisiblePropertyState(new string[]
		{
			"TraceFilterSearchTimeRange"
		})]
		private DateTimePicker dtpTimeFrom;

		[UIControlEnablePropertyState(new string[]
		{
			"TraceFilterSearchTimeRange"
		})]
		[UIControlVisiblePropertyState(new string[]
		{
			"TraceFilterSearchTimeRange"
		})]
		private DateTimePicker dtpTimeTo;

		internal event TraceFilterChanged TraceFilterChangedEvent;

		public TraceFilterControl(IErrorReport report , TraceViewerForm form)
		{
			InitializeComponent();
			InitializeToolStrips();
			objectStateController = new StateMachineController(this);
			searchInList.SelectedIndex = 0;
			traceLevelList.SelectedIndex = 0;
            this.errorReport = report;
            this.parentForm = form;
			TraceViewerForm traceViewerForm = this.parentForm;
			traceViewerForm.DataSourceChangedHandler = (TraceViewerForm.DataSourceChanged)Delegate.Combine(traceViewerForm.DataSourceChangedHandler, new TraceViewerForm.DataSourceChanged(TraceViewerForm_DataSourceChanged));
			objectStateController.SwitchState("TraceFilterDisableState");
		}

		private void TraceViewerForm_DataSourceChanged(TraceDataSource dataSource)
		{
			if (dataSource != null)
			{
				dataSource.AppendFileFinishedCallback = (TraceDataSource.AppendFileFinished)Delegate.Combine(dataSource.AppendFileFinishedCallback, new TraceDataSource.AppendFileFinished(DataSource_OnAppendFileFinished));
				dataSource.RemoveAllFileFinishedCallback = (TraceDataSource.RemoveAllFileFinished)Delegate.Combine(dataSource.RemoveAllFileFinishedCallback, new TraceDataSource.RemoveAllFileFinished(DataSource_RemoveAllFileFinished));
				dataSource.RemoveFileFinishedCallback = (TraceDataSource.RemoveFileFinished)Delegate.Combine(dataSource.RemoveFileFinishedCallback, new TraceDataSource.RemoveFileFinished(DataSource_RemoveFileFinished));
				dataSource.ReloadFilesFinishedCallback = (TraceDataSource.ReloadFilesFinished)Delegate.Combine(dataSource.ReloadFilesFinishedCallback, new TraceDataSource.ReloadFilesFinished(DataSource_ReloadFilesFinished));
				currentDataSource = dataSource;
			}
		}

		private void DataSource_OnAppendFileFinished(string[] filesPath, TaskInfoBase task)
		{
			UpdateTimeFilterRange();
		}

		private void DataSource_RemoveAllFileFinished()
		{
			UpdateTimeFilterRange();
		}

		private void DataSource_RemoveFileFinished(string[] filesPath)
		{
			UpdateTimeFilterRange();
		}

		private void DataSource_ReloadFilesFinished()
		{
			UpdateTimeFilterRange();
		}

		private void UpdateTimeFilterRange()
		{
			if (currentDataSource != null)
			{
				DateTimePair traceTimeRange = currentDataSource.TraceTimeRange;
				if (traceTimeRange != null)
				{
					DateTime dateTime = traceTimeRange.StartTime.AddSeconds(-1.0);
					DateTime dateTime2 = traceTimeRange.EndTime.AddSeconds(1.0);
					if (dateTime >= dtpTimeFrom.MinDate && dateTime <= dtpTimeFrom.MaxDate && dateTime2 >= dtpTimeFrom.MinDate && dateTime2 <= dtpTimeFrom.MaxDate)
					{
						dtpTimeFrom.Value = dateTime;
						dtpTimeTo.Value = dateTime2;
						dtpTime.Value = dateTime;
						return;
					}
				}
				dtpTimeFrom.Value = DateTime.Now.AddSeconds(-1.0);
				dtpTimeTo.Value = DateTime.Now;
			}
		}

		private void InitializeToolStrips()
		{
			dtpTime = new DateTimePicker();
			dtpTime.Width = 160;
			dtpTime.CustomFormat = "yyyy-MM-dd  HH:mm:ss";
			dtpTime.Format = DateTimePickerFormat.Custom;
			ToolStripControlHost toolStripControlHost = new ToolStripControlHost(dtpTime);
			toolStripControlHost.Margin = new Padding(10, 0, 1, 0);
			timeFilterStrip.Items.Add(toolStripControlHost);
			ToolStripLabel toolStripLabel = new ToolStripLabel();
			toolStripLabel.Name = "toolStripLabelTimeFrom";
			toolStripLabel.Text = SR.GetString("TxtFrom");
			dtpTimeFrom = new DateTimePicker();
			dtpTimeFrom.CustomFormat = "yyyy-MM-dd  HH:mm:ss";
			dtpTimeFrom.Format = DateTimePickerFormat.Custom;
			dtpTimeFrom.Width = 129;
			ToolStripControlHost toolStripControlHost2 = new ToolStripControlHost(dtpTimeFrom);
			ToolStripLabel toolStripLabel2 = new ToolStripLabel();
			toolStripLabel2.Name = "toolStripLabelTimeTo";
			toolStripLabel2.Text = SR.GetString("TxtTo");
			toolStripLabel2.Margin = new Padding(10, 0, 1, 0);
			dtpTimeTo = new DateTimePicker();
			dtpTimeTo.CustomFormat = "yyyy-MM-dd  HH:mm:ss";
			dtpTimeTo.Format = DateTimePickerFormat.Custom;
			dtpTimeTo.Width = 129;
			ToolStripControlHost toolStripControlHost3 = new ToolStripControlHost(dtpTimeTo);
			timeRangeFilterStrip.Items.AddRange(new ToolStripItem[4]
			{
				toolStripLabel,
				toolStripControlHost2,
				toolStripLabel2,
				toolStripControlHost3
			});
		}

		public void UpdateSearchInList(List<CustomFilter> customFilters)
		{
			searchInList.Items.Clear();
			searchInList.Items.AddRange(new object[12]
			{
				SR.GetString("AppFilterItem1"),
				SR.GetString("AppFilterItem2"),
				SR.GetString("AppFilterItem3"),
				SR.GetString("AppFilterItem4"),
				SR.GetString("AppFilterItem5"),
				SR.GetString("AppFilterItem6"),
				SR.GetString("AppFilterItem7"),
				SR.GetString("AppFilterItem8"),
				SR.GetString("AppFilterItem9"),
				SR.GetString("AppFilterItem10"),
				SR.GetString("AppFilterItem17"),
				SR.GetString("AppFilterItem18")
			});
			if (customFilters != null && customFilters.Count != 0)
			{
				int num = searchInList.Items.Count;
				customFilterMap = new Dictionary<int, CustomFilter>();
				foreach (CustomFilter customFilter in customFilters)
				{
					searchInList.Items.Add(customFilter.FilterName);
					customFilterMap.Add(num++, customFilter);
				}
			}
			searchInList.SelectedIndex = 0;
		}

		public void ClearFilter()
		{
			searchInList.SelectedIndex = 0;
			traceLevelList.SelectedIndex = 0;
			FilterEngine.CurrentFilterCriteria = null;
		}

		public bool IsFilterEnabled()
		{
			if (FilterEngine.CurrentFilterCriteria != null && (FilterEngine.CurrentFilterCriteria.SearchOption != 0 || FilterEngine.CurrentFilterCriteria.FilterSourceLevel != SourceLevels.All))
			{
				return true;
			}
			return false;
		}

		internal void Initialize(TraceViewerForm parent)
		{
			parent.ObjectStateController.RegisterStateSwitchListener(objectStateController);
		}

		void IStateAwareObject.PreStateSwitch(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		void IStateAwareObject.PostStateSwitch(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		void IStateAwareObject.StateSwitchSuccess(ObjectStateBase fromState, ObjectStateBase toState)
		{
			if (toState.StateName == "TraceFilterEnableState")
			{
				SwitchStateBySearchInListSelection();
			}
		}

		void IStateAwareObject.StateSwitchFailed(ObjectStateBase fromState, ObjectStateBase toState, ObjectStateSwitchFailReason reason)
		{
		}

		public void FilterNow()
		{
			FilterNowAction();
		}

		private void SearchInList_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (searchInList.SelectedIndex == -1)
			{
				objectStateController.SwitchState("TraceFilterSearchNoneState");
				lastSelectedSearchInListIndex = searchInList.SelectedIndex;
			}
			else
			{
				SwitchStateBySearchInListSelection();
				lastSelectedSearchInListIndex = searchInList.SelectedIndex;
			}
		}

		private void SwitchStateBySearchInListSelection()
		{
			toolStripTextBoxLookFor.ToolTipText = string.Empty;
			if (searchInList.SelectedIndex < 12)
			{
				switch (searchInList.SelectedIndex)
				{
				case -1:
				case 0:
					objectStateController.SwitchState("TraceFilterSearchNoneState");
					break;
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
				case 9:
				case 10:
				case 11:
					objectStateController.SwitchState("TraceFilterSearchTextState");
					break;
				case 6:
				case 7:
					objectStateController.SwitchState("TraceFilterSearchTimeState");
					break;
				case 8:
					objectStateController.SwitchState("TraceFilterSearchTimeRange");
					break;
				}
			}
			else if (customFilterMap[searchInList.SelectedIndex].ContainsParameters)
			{
				objectStateController.SwitchState("TraceFilterSearchTextState");
				string text = null;
				int num = 0;
				foreach (CustomFilter.CustomFilterParameter parameter in customFilterMap[searchInList.SelectedIndex].Parameters)
				{
					if (!string.IsNullOrEmpty(parameter.description))
					{
						text = text + num.ToString(CultureInfo.InvariantCulture) + " - " + parameter.description + "\n";
					}
					num++;
				}
				if (!string.IsNullOrEmpty(text))
				{
					if (text.EndsWith("\n", StringComparison.Ordinal))
					{
						text = text.Substring(0, text.Length - 1);
					}
					toolStripTextBoxLookFor.ToolTipText = text;
				}
			}
			else
			{
				objectStateController.SwitchState("TraceFilterSearchNoneState");
			}
		}

		public void PerformFiltering(FilterCriteria criteria)
		{
			if (criteria != null && objectStateController.CurrentStateName != "TraceFilterDisableState")
			{
				if (criteria.SearchOption == SearchOptions.None)
				{
					searchInList.SelectedIndex = 0;
				}
				SourceLevels filterSourceLevel = criteria.FilterSourceLevel;
				if (filterSourceLevel == SourceLevels.All)
				{
					traceLevelList.SelectedIndex = 0;
				}
				FilterNowAction();
			}
		}

		private void toolStripButtonFilterNow_Click(object sender, EventArgs e)
		{
			if (objectStateController.CurrentStateName == "TraceFilterSearcEnableState")
			{
				SwitchStateBySearchInListSelection();
			}
			if (!(objectStateController.CurrentStateName == "TraceFilterSearchTextState") || !string.IsNullOrEmpty(toolStripTextBoxLookFor.Text))
			{
				FilterNowAction();
			}
		}

		private void FilterNowAction()
		{
			FilterCriteria filterCriteria = new FilterCriteria();
			filterCriteria.FilterSourceLevel = FilterEngine.ParseLevel((traceLevelList.SelectedItem != null) ? traceLevelList.SelectedItem.ToString() : SR.GetString("AppFilterItem1"));
			if (searchInList.SelectedIndex >= 12)
			{
				filterCriteria.SearchOption = SearchOptions.CustomFilter;
			}
			else
			{
				filterCriteria.SearchOption = FilterEngine.ParseSearchOption((searchInList.SelectedItem != null) ? searchInList.SelectedItem.ToString() : SR.GetString("AppFilterItem13"));
			}
			switch (filterCriteria.SearchOption)
			{
			case SearchOptions.EventID:
			case SearchOptions.SourceName:
			case SearchOptions.ProcessName:
			case SearchOptions.TraceCode:
			case SearchOptions.Description:
			case SearchOptions.EndpointAddress:
			case SearchOptions.AppDataSection:
			case SearchOptions.EntireRawData:
				filterCriteria.searchCondition = toolStripTextBoxLookFor.Text;
				break;
			case SearchOptions.StartTime:
			{
				DateTime value4 = dtpTime.Value;
				DateTime dateTime2 = new DateTime(value4.Year, value4.Month, value4.Day, value4.Hour, value4.Minute, value4.Second, 0, value4.Kind);
				filterCriteria.searchCondition = dateTime2;
				break;
			}
			case SearchOptions.StopTime:
			{
				DateTime value3 = dtpTime.Value;
				DateTime dateTime = new DateTime(value3.Year, value3.Month, value3.Day, value3.Hour, value3.Minute, value3.Second, 999, value3.Kind);
				filterCriteria.searchCondition = dateTime;
				break;
			}
			case SearchOptions.TimeRange:
			{
				DateTime value = dtpTimeFrom.Value;
				DateTime startTime = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, 0, value.Kind);
				DateTime value2 = dtpTimeTo.Value;
				DateTime endTime = new DateTime(value2.Year, value2.Month, value2.Day, value2.Hour, value2.Minute, value2.Second, 999, value2.Kind);
				filterCriteria.searchCondition = new DateTimePair(startTime, endTime);
				break;
			}
			case SearchOptions.CustomFilter:
				if (customFilterMap.ContainsKey(searchInList.SelectedIndex))
				{
					filterCriteria.searchCondition = new CustomFilterCondition();
					((CustomFilterCondition)filterCriteria.searchCondition).customFilter = customFilterMap[searchInList.SelectedIndex];
					if (((CustomFilterCondition)filterCriteria.searchCondition).customFilter.ContainsParameters)
					{
						string text = ((CustomFilterCondition)filterCriteria.searchCondition).customFilter.ParseXPathExpression(toolStripTextBoxLookFor.Text);
						if (string.IsNullOrEmpty(text))
						{
							errorReport.ReportErrorToUser(SR.GetString("CF_IPError"));
							return;
						}
						((CustomFilterCondition)filterCriteria.searchCondition).parsedXPathExpress = text;
					}
				}
				break;
			}
			FilterEngine.CurrentFilterCriteria = filterCriteria;
			try
			{
				if (this.TraceFilterChangedEvent != null)
				{
					this.TraceFilterChangedEvent(this, new TraceFilterChangedEventArgs(filterCriteria));
				}
			}
			catch (Exception ex)
			{
				ExceptionManager.GeneralExceptionFilter(ex);
				ExceptionManager.LogAppError(new TraceViewerException(SR.GetString("MsgTraceFilterChangeCallbackError") + ex.Message));
			}
		}

		private void toolStripTextBoxLookFor_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return && !string.IsNullOrEmpty(toolStripTextBoxLookFor.Text))
			{
				FilterNowAction();
			}
		}

		private void toolStripButtonRestore_Click(object sender, EventArgs e)
		{
			FilterCriteria filterCriteria = new FilterCriteria();
			filterCriteria.FilterSourceLevel = SourceLevels.All;
			filterCriteria.searchCondition = string.Empty;
			filterCriteria.SearchOption = SearchOptions.None;
			PerformFiltering(filterCriteria);
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
            this.filterStripContainer = new System.Windows.Forms.ToolStripContainer();
            this.toolStripStandard = new System.Windows.Forms.ToolStrip();
            this.searchInLabel = new System.Windows.Forms.ToolStripLabel();
            this.searchInList = new System.Windows.Forms.ToolStripComboBox();
            this.traceLevelLabel = new System.Windows.Forms.ToolStripLabel();
            this.traceLevelList = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripButtonFilterNow = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonRestore = new System.Windows.Forms.ToolStripButton();
            this.lookForFilterStrip = new System.Windows.Forms.ToolStrip();
            this.lookForLabel = new System.Windows.Forms.ToolStripLabel();
            this.toolStripTextBoxLookFor = new System.Windows.Forms.ToolStripTextBox();
            this.timeFilterStrip = new System.Windows.Forms.ToolStrip();
            this.timeLabel = new System.Windows.Forms.ToolStripLabel();
            this.timeRangeFilterStrip = new System.Windows.Forms.ToolStrip();
            this.filterStripContainer.ContentPanel.SuspendLayout();
            this.filterStripContainer.SuspendLayout();
            this.toolStripStandard.SuspendLayout();
            this.lookForFilterStrip.SuspendLayout();
            this.timeFilterStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // filterStripContainer
            // 
            this.filterStripContainer.BottomToolStripPanelVisible = false;
            // 
            // filterStripContainer.ContentPanel
            // 
            this.filterStripContainer.ContentPanel.Controls.Add(this.toolStripStandard);
            this.filterStripContainer.ContentPanel.Controls.Add(this.lookForFilterStrip);
            this.filterStripContainer.ContentPanel.Controls.Add(this.timeFilterStrip);
            this.filterStripContainer.ContentPanel.Controls.Add(this.timeRangeFilterStrip);
            this.filterStripContainer.ContentPanel.Size = new System.Drawing.Size(800, 0);
            this.filterStripContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.filterStripContainer.LeftToolStripPanelVisible = false;
            this.filterStripContainer.Location = new System.Drawing.Point(0, 0);
            this.filterStripContainer.Name = "filterStripContainer";
            this.filterStripContainer.RightToolStripPanelVisible = false;
            this.filterStripContainer.Size = new System.Drawing.Size(800, 25);
            this.filterStripContainer.TabIndex = 0;
            // 
            // toolStripStandard
            // 
            this.toolStripStandard.AllowMerge = false;
            this.toolStripStandard.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStripStandard.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.searchInLabel,
            this.searchInList,
            this.traceLevelLabel,
            this.traceLevelList,
            this.toolStripButtonFilterNow,
            this.toolStripButtonRestore});
            this.toolStripStandard.Location = new System.Drawing.Point(340, 0);
            this.toolStripStandard.Name = "toolStripStandard";
            this.toolStripStandard.Size = new System.Drawing.Size(474, 25);
            this.toolStripStandard.TabIndex = 0;
            // 
            // searchInLabel
            // 
            this.searchInLabel.Name = "searchInLabel";
            this.searchInLabel.Size = new System.Drawing.Size(58, 22);
            this.searchInLabel.Text = "Sea&rch In:";
            // 
            // searchInList
            // 
            this.searchInList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.searchInList.DropDownWidth = 230;
            this.searchInList.Items.AddRange(new object[] {
            "None",
            "Event ID",
            "Source Name",
            "Process Name",
            "Trace Identifier",
            "Description",
            "Start Time",
            "Stop Time",
            "Time Range",
            "Endpoint Address"});
            this.searchInList.Name = "searchInList";
            this.searchInList.Size = new System.Drawing.Size(125, 25);
            this.searchInList.ToolTipText = "Available filter options";
            this.searchInList.SelectedIndexChanged += new System.EventHandler(this.SearchInList_SelectedIndexChanged);
            // 
            // traceLevelLabel
            // 
            this.traceLevelLabel.Name = "traceLevelLabel";
            this.traceLevelLabel.Size = new System.Drawing.Size(37, 22);
            this.traceLevelLabel.Text = "Leve&l:";
            // 
            // traceLevelList
            // 
            this.traceLevelList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.traceLevelList.DropDownWidth = 190;
            this.traceLevelList.Items.AddRange(new object[] {
            "All",
            "Critical",
            "Error And Up",
            "Warning And Up",
            "Information And Up"});
            this.traceLevelList.Name = "traceLevelList";
            this.traceLevelList.Size = new System.Drawing.Size(135, 25);
            this.traceLevelList.ToolTipText = "Trace level filter";
            // 
            // toolStripButtonFilterNow
            // 
            this.toolStripButtonFilterNow.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonFilterNow.Name = "toolStripButtonFilterNow";
            this.toolStripButtonFilterNow.Size = new System.Drawing.Size(65, 22);
            this.toolStripButtonFilterNow.Text = "F&ilter Now";
            this.toolStripButtonFilterNow.ToolTipText = "Filter traces using current settings";
            this.toolStripButtonFilterNow.Click += new System.EventHandler(this.toolStripButtonFilterNow_Click);
            // 
            // toolStripButtonRestore
            // 
            this.toolStripButtonRestore.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonRestore.Name = "toolStripButtonRestore";
            this.toolStripButtonRestore.Size = new System.Drawing.Size(38, 22);
            this.toolStripButtonRestore.Text = "&Clear";
            this.toolStripButtonRestore.ToolTipText = "Restore all traces.";
            this.toolStripButtonRestore.Click += new System.EventHandler(this.toolStripButtonRestore_Click);
            // 
            // lookForFilterStrip
            // 
            this.lookForFilterStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.lookForFilterStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lookForLabel,
            this.toolStripTextBoxLookFor});
            this.lookForFilterStrip.Location = new System.Drawing.Point(0, 0);
            this.lookForFilterStrip.Name = "lookForFilterStrip";
            this.lookForFilterStrip.Size = new System.Drawing.Size(342, 25);
            this.lookForFilterStrip.TabIndex = 1;
            this.lookForFilterStrip.Visible = false;
            // 
            // lookForLabel
            // 
            this.lookForLabel.Name = "lookForLabel";
            this.lookForLabel.Size = new System.Drawing.Size(53, 22);
            this.lookForLabel.Text = "L&ook For";
            // 
            // toolStripTextBoxLookFor
            // 
            this.toolStripTextBoxLookFor.Name = "toolStripTextBoxLookFor";
            this.toolStripTextBoxLookFor.Size = new System.Drawing.Size(275, 25);
            this.toolStripTextBoxLookFor.KeyUp += new System.Windows.Forms.KeyEventHandler(this.toolStripTextBoxLookFor_KeyUp);
            // 
            // timeFilterStrip
            // 
            this.timeFilterStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.timeFilterStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.timeLabel});
            this.timeFilterStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.timeFilterStrip.Location = new System.Drawing.Point(0, 0);
            this.timeFilterStrip.Name = "timeFilterStrip";
            this.timeFilterStrip.Size = new System.Drawing.Size(46, 25);
            this.timeFilterStrip.TabIndex = 2;
            this.timeFilterStrip.Visible = false;
            // 
            // timeLabel
            // 
            this.timeLabel.Name = "timeLabel";
            this.timeLabel.Size = new System.Drawing.Size(34, 22);
            this.timeLabel.Text = "Ti&me";
            // 
            // timeRangeFilterStrip
            // 
            this.timeRangeFilterStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.timeRangeFilterStrip.Location = new System.Drawing.Point(0, 0);
            this.timeRangeFilterStrip.Name = "timeRangeFilterStrip";
            this.timeRangeFilterStrip.Size = new System.Drawing.Size(111, 25);
            this.timeRangeFilterStrip.TabIndex = 3;
            this.timeRangeFilterStrip.Visible = false;
            // 
            // TraceFilterControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.filterStripContainer);
            this.Name = "TraceFilterControl";
            this.Size = new System.Drawing.Size(800, 25);
            this.filterStripContainer.ContentPanel.ResumeLayout(false);
            this.filterStripContainer.ContentPanel.PerformLayout();
            this.filterStripContainer.ResumeLayout(false);
            this.filterStripContainer.PerformLayout();
            this.toolStripStandard.ResumeLayout(false);
            this.toolStripStandard.PerformLayout();
            this.lookForFilterStrip.ResumeLayout(false);
            this.lookForFilterStrip.PerformLayout();
            this.timeFilterStrip.ResumeLayout(false);
            this.timeFilterStrip.PerformLayout();
            this.ResumeLayout(false);

		}
	}
}
