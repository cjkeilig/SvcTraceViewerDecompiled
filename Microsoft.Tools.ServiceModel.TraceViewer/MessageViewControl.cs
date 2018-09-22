using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[ObjectStateMachine(typeof(IdleProjectState), true, typeof(BusyProjectState))]
	[ObjectStateMachine(typeof(BusyProjectState), false, typeof(IdleProjectState))]
	[ObjectStateMachine(typeof(GroupByNoneState), false, null, "TraceGroupBy")]
	[ObjectStateMachine(typeof(GroupByActivityState), false, null, "TraceGroupBy")]
	[ObjectStateMachine(typeof(GroupByProcessState), false, null, "TraceGroupBy")]
	[ObjectStateMachine(typeof(GroupByInOutState), false, null, "TraceGroupBy")]
	[ObjectStateTransfer("TraceLoadingProjectState", "BusyProjectState")]
	[ObjectStateTransfer("FileLoadingProjectState", "BusyProjectState")]
	[ObjectStateTransfer("FileRemovingProjectState", "BusyProjectState")]
	[ObjectStateTransfer("FileReloadingProjectState", "BusyProjectState")]
	[ObjectStateTransfer("NoFileProjectState", "IdleProjectState")]
	internal class MessageViewControl : UserControl, IStateAwareObject
	{
		private class InternalMessageInfo
		{
			private string action;

			private string to;

			public string Action
			{
				get
				{
					return action;
				}
				set
				{
					action = value;
				}
			}

			public string To
			{
				get
				{
					return to;
				}
				set
				{
					to = value;
				}
			}
		}

		private class InternalListViewItemComparer : IComparer<ListViewItem>
		{
			private IComparer comparer;

			public InternalListViewItemComparer(IComparer comparer)
			{
				this.comparer = comparer;
			}

			public int Compare(ListViewItem x, ListViewItem y)
			{
				return comparer.Compare(x, y);
			}
		}

		private enum TabFocusIndex
		{
			MainPanel,
			GroupByStripMenu,
			MessageTraceList
		}

		private EventHandler messageTraceItemClick;

		private StateMachineController objectStateController;

		private StateMachineController traceGroupByStateController;

		private List<ListViewItem> currentListViewItems = new List<ListViewItem>();

		private TraceDataSource currentDataSource;

		private EventHandler messageTraceItemDoubleClick;

		private IContainer components;

		private Panel mainPanel;

		[UIControlSpecialStateEventHandler(new string[]
		{
			"IdleProjectState"
		}, "listTraces_SelectedIndexChanged", "SelectedIndexChanged", "listTraces_SelectedIndexChanged")]
		[UIControlSpecialStateEventHandler(new string[]
		{
			"IdleProjectState"
		}, "listTraces_DoubleClick", "DoubleClick", "listTraces_DoubleClick")]
		private ListView listTraces;

		private ColumnHeader dateTimeHeader;

		private ColumnHeader executionHeader;

		private ColumnHeader activityHeader;

		private ColumnHeader actionHeader;

		private ColumnHeader toHeader;

		private StatusStrip statusStrip;

		private ToolStripStatusLabel traceCountStatusLabel;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"IdleProjectState"
		}, "Enabled")]
		private MenuStrip groupByStripMenu;

		private ToolStripMenuItem groupByStripMenuItem;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"GroupByNoneState"
		}, "Checked")]
		private ToolStripMenuItem groupByNoneStripMenuItem;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"GroupByActivityState"
		}, "Checked")]
		private ToolStripMenuItem groupByActivityStripMenuItem;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"GroupByProcessState"
		}, "Checked")]
		private ToolStripMenuItem groupByProcessStripMenuItem;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"GroupByInOutState"
		}, "Checked")]
		private ToolStripMenuItem groupByInOutStripMenuItem;

		public TraceRecord CurrentSelectedTraceRecord
		{
			get
			{
				if (listTraces.VirtualMode && listTraces.SelectedIndices.Count != 0 && listTraces.SelectedIndices[0] >= 0 && listTraces.SelectedIndices[0] < currentListViewItems.Count)
				{
					return (TraceRecord)currentListViewItems[listTraces.SelectedIndices[0]].Tag;
				}
				if (!listTraces.VirtualMode && listTraces.SelectedItems.Count != 0)
				{
					return (TraceRecord)listTraces.SelectedItems[0].Tag;
				}
				return null;
			}
		}

		public EventHandler MessageTraceItemClick
		{
			get
			{
				return messageTraceItemClick;
			}
			set
			{
				messageTraceItemClick = value;
			}
		}

		public EventHandler MessageTraceItemDoubleClick
		{
			get
			{
				return messageTraceItemDoubleClick;
			}
			set
			{
				messageTraceItemDoubleClick = value;
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

		public MessageViewControl()
		{
			InitializeComponent();
		}

		internal void Initialize(TraceViewerForm parent)
		{
			parent.DataSourceChangedHandler = (TraceViewerForm.DataSourceChanged)Delegate.Combine(parent.DataSourceChangedHandler, new TraceViewerForm.DataSourceChanged(DataSource_OnChanged));
			traceCountStatusLabel.Text = SR.GetString("MsgView_Count");
			listTraces.SmallImageList = TraceViewerForm.GetSharedImageList();
			objectStateController = new StateMachineController(this);
			parent.ObjectStateController.RegisterStateSwitchListener(objectStateController);
			traceGroupByStateController = new StateMachineController(this, "TraceGroupBy");
			traceGroupByStateController.SwitchState("GroupByNoneState");
			ReGroupTraceList();
		}

		private void ReGroupTraceList()
		{
			if (listTraces.SelectedIndices.Count != 0)
			{
				listTraces.SelectedIndices.Clear();
			}
			string currentStateName = traceGroupByStateController.CurrentStateName;
			switch (currentStateName)
			{
			default:
				if (currentStateName == "GroupByInOutState")
				{
					ReGroupTraceListByIO();
					groupByStripMenuItem.Text = SR.GetString("MainFrm_GroupByIO2");
				}
				break;
			case "GroupByNoneState":
				ReGroupTraceListByNone();
				groupByStripMenuItem.Text = SR.GetString("MainFrm_GroupByNone2");
				break;
			case "GroupByActivityState":
				ReGroupTraceListByActivity();
				groupByStripMenuItem.Text = SR.GetString("MainFrm_GroupByActivity2");
				break;
			case "GroupByProcessState":
				ReGroupTraceListByProcess();
				groupByStripMenuItem.Text = SR.GetString("MainFrm_GroupByProcess2");
				break;
			}
		}

		private ListViewItem ComposeTraceListItem(TraceRecord tr)
		{
			InternalMessageInfo internalMessageInfo = ExtraceMessageLog(tr);
			int num = -1;
			if (tr.MessageProperties == MessageProperty.MessageIn)
			{
				num = TraceViewerForm.GetImageIndexFromImageList(Images.MessageReceiveTrace);
			}
			else if (tr.MessageProperties == MessageProperty.MessageOut)
			{
				num = TraceViewerForm.GetImageIndexFromImageList(Images.MessageSentTrace);
			}
			ListViewItem listViewItem = null;
			listViewItem = ((num == -1) ? new ListViewItem(new string[5]
			{
				(internalMessageInfo != null && !string.IsNullOrEmpty(internalMessageInfo.Action)) ? internalMessageInfo.Action : string.Empty,
				Utilities.GetShortTimeStringFromDateTime(tr.Time),
				string.IsNullOrEmpty(tr.Execution.ComputerName) ? tr.Execution.ProcessName : (tr.Execution.ComputerName + SR.GetString("SL_ExecutionSep") + tr.Execution.ProcessName),
				TraceViewerForm.GetActivityDisplayName(tr.ActivityID),
				(internalMessageInfo != null && !string.IsNullOrEmpty(internalMessageInfo.To)) ? internalMessageInfo.To : string.Empty
			}) : new ListViewItem(new string[5]
			{
				(internalMessageInfo != null && !string.IsNullOrEmpty(internalMessageInfo.Action)) ? internalMessageInfo.Action : string.Empty,
				Utilities.GetShortTimeStringFromDateTime(tr.Time),
				string.IsNullOrEmpty(tr.Execution.ComputerName) ? tr.Execution.ProcessName : (tr.Execution.ComputerName + SR.GetString("SL_ExecutionSep") + tr.Execution.ProcessName),
				TraceViewerForm.GetActivityDisplayName(tr.ActivityID),
				(internalMessageInfo != null && !string.IsNullOrEmpty(internalMessageInfo.To)) ? internalMessageInfo.To : string.Empty
			}, num));
			listViewItem.Tag = tr;
			if (currentDataSource != null && currentDataSource.Activities.ContainsKey(tr.ActivityID))
			{
				if (currentDataSource.Activities[tr.ActivityID].HasError)
				{
					listViewItem.ForeColor = Color.Red;
					ListViewItem listViewItem2 = listViewItem;
					listViewItem2.Font = new Font(listViewItem2.Font, listViewItem.Font.Style | FontStyle.Bold);
				}
				else if (currentDataSource.Activities[tr.ActivityID].HasWarning)
				{
					listViewItem.BackColor = Color.Yellow;
				}
			}
			return listViewItem;
		}

		private void ReGroupTraceListByProcess()
		{
			try
			{
				listTraces.BeginUpdate();
				listTraces.VirtualMode = false;
				listTraces.Items.Clear();
				listTraces.Groups.Clear();
				Dictionary<int, ListViewGroup> dictionary = new Dictionary<int, ListViewGroup>();
				foreach (ListViewItem currentListViewItem in currentListViewItems)
				{
					TraceRecord traceRecord = (TraceRecord)currentListViewItem.Tag;
					ListViewItem listViewItem = ComposeTraceListItem(traceRecord);
					int executionID = traceRecord.Execution.ExecutionID;
					if (!dictionary.ContainsKey(executionID))
					{
						dictionary.Add(executionID, new ListViewGroup(traceRecord.Execution.ComputerName + SR.GetString("CF_Sep") + traceRecord.Execution.ProcessName + (TraceViewerForm.IsThreadExecutionMode ? (SR.GetString("CF_LeftB") + traceRecord.Execution.ThreadID + SR.GetString("CF_RightB")) : string.Empty)));
						listTraces.Groups.Add(dictionary[executionID]);
					}
					listViewItem.Group = dictionary[executionID];
					listTraces.Items.Add(listViewItem);
				}
				SortByColumn( true, 1);
			}
			catch (ArgumentOutOfRangeException)
			{
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
			finally
			{
				listTraces.EndUpdate();
				traceCountStatusLabel.Text = SR.GetString("MsgView_Count") + listTraces.Items.Count.ToString(CultureInfo.InvariantCulture);
			}
		}

		private void ReGroupTraceListByIO()
		{
			try
			{
				listTraces.BeginUpdate();
				listTraces.VirtualMode = false;
				listTraces.Items.Clear();
				listTraces.Groups.Clear();
				Dictionary<int, ListViewGroup> dictionary = new Dictionary<int, ListViewGroup>();
				foreach (ListViewItem currentListViewItem in currentListViewItems)
				{
					TraceRecord traceRecord = (TraceRecord)currentListViewItem.Tag;
					ListViewItem listViewItem = ComposeTraceListItem(traceRecord);
					int messageProperties = (int)traceRecord.MessageProperties;
					if (!dictionary.ContainsKey(messageProperties))
					{
						switch (messageProperties)
						{
						case 0:
							dictionary.Add(messageProperties, new ListViewGroup(SR.GetString("GP_MsgIn")));
							break;
						case 1:
							dictionary.Add(messageProperties, new ListViewGroup(SR.GetString("GP_MsgOut")));
							break;
						case 2:
							dictionary.Add(messageProperties, new ListViewGroup(SR.GetString("GP_MsgUnkn")));
							break;
						}
						listTraces.Groups.Add(dictionary[messageProperties]);
					}
					listViewItem.Group = dictionary[messageProperties];
					listTraces.Items.Add(listViewItem);
				}
				SortByColumn( true, 1);
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
			finally
			{
				listTraces.EndUpdate();
				traceCountStatusLabel.Text = SR.GetString("MsgView_Count") + listTraces.Items.Count.ToString(CultureInfo.InvariantCulture);
			}
		}

		private void ReGroupTraceListByNone()
		{
			listTraces.VirtualMode = false;
			listTraces.Items.Clear();
			listTraces.VirtualListSize = currentListViewItems.Count;
			listTraces.VirtualMode = true;
		}

		private void ReGroupTraceListByActivity()
		{
			try
			{
				listTraces.BeginUpdate();
				listTraces.VirtualMode = false;
				listTraces.Items.Clear();
				listTraces.Groups.Clear();
				Dictionary<string, ListViewGroup> dictionary = new Dictionary<string, ListViewGroup>();
				foreach (ListViewItem currentListViewItem in currentListViewItems)
				{
					TraceRecord traceRecord = (TraceRecord)currentListViewItem.Tag;
					ListViewItem listViewItem = ComposeTraceListItem(traceRecord);
					if (!string.IsNullOrEmpty(traceRecord.MessageActivityID))
					{
						if (!dictionary.ContainsKey(traceRecord.MessageActivityID))
						{
							ListViewGroup value = listTraces.Groups.Add(TraceViewerForm.GetActivityDisplayName(traceRecord.MessageActivityID), TraceViewerForm.GetActivityDisplayName(traceRecord.MessageActivityID));
							dictionary.Add(traceRecord.MessageActivityID, value);
						}
						listViewItem.Group = dictionary[traceRecord.MessageActivityID];
					}
					listTraces.Items.Add(listViewItem);
				}
				SortByColumn( true, 1);
			}
			catch (ArgumentOutOfRangeException)
			{
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
			finally
			{
				listTraces.EndUpdate();
				traceCountStatusLabel.Text = SR.GetString("MsgView_Count") + listTraces.Items.Count.ToString(CultureInfo.InvariantCulture);
			}
		}

		private void groupByNoneStripMenuItem_Click(object sender, EventArgs e)
		{
			if (traceGroupByStateController.CurrentStateName != "GroupByNoneState" && traceGroupByStateController.SwitchState("GroupByNoneState"))
			{
				ReGroupTraceList();
			}
			EnsureHighlight();
		}

		private void groupByActivityStripMenuItem_Click(object sender, EventArgs e)
		{
			if (traceGroupByStateController.CurrentStateName != "GroupByActivityState" && traceGroupByStateController.SwitchState("GroupByActivityState"))
			{
				ReGroupTraceList();
			}
			EnsureHighlight();
		}

		private void DataSource_OnChanged(TraceDataSource dataSource)
		{
			if (dataSource != null)
			{
				currentDataSource = dataSource;
				dataSource.MessageLoggedTracesAppendedCallback = (TraceDataSource.MessageLoggedTracesAppended)Delegate.Combine(dataSource.MessageLoggedTracesAppendedCallback, new TraceDataSource.MessageLoggedTracesAppended(DataSource_OnMessageLoggedTracesAppended));
				dataSource.MessageLoggedTracesClearedCallback = (TraceDataSource.MessageLoggedTracesCleared)Delegate.Combine(dataSource.MessageLoggedTracesClearedCallback, new TraceDataSource.MessageLoggedTracesCleared(DataSource_OnMessageLoggedTracesCleared));
				dataSource.AppendFileFinishedCallback = (TraceDataSource.AppendFileFinished)Delegate.Combine(dataSource.AppendFileFinishedCallback, new TraceDataSource.AppendFileFinished(DataSource_OnAppendFileFinished));
				dataSource.RemoveFileFinishedCallback = (TraceDataSource.RemoveFileFinished)Delegate.Combine(dataSource.RemoveFileFinishedCallback, new TraceDataSource.RemoveFileFinished(DataSource_OnRemoveFileFinished));
			}
		}

		public void Clear()
		{
			try
			{
				listTraces.SuspendLayout();
				listTraces.SelectedIndices.Clear();
				listTraces.VirtualMode = false;
				listTraces.Items.Clear();
				listTraces.Groups.Clear();
				currentListViewItems.Clear();
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
			finally
			{
				listTraces.ResumeLayout();
				traceCountStatusLabel.Text = SR.GetString("MsgView_Count");
			}
		}

		private void DataSource_OnMessageLoggedTracesAppended(List<TraceRecord> traces)
		{
			if (traces != null)
			{
				foreach (TraceRecord trace in traces)
				{
					currentListViewItems.Add(ComposeTraceListItem(trace));
				}
			}
		}

		private void DataSource_OnMessageLoggedTracesCleared()
		{
			Clear();
		}

		private void DataSource_OnRemoveFileFinished(string[] filesName)
		{
			ReGroupTraceList();
		}

		private void DataSource_OnAppendFileFinished(string[] fileNames, TaskInfoBase task)
		{
			ReGroupTraceList();
		}

		private InternalMessageInfo ExtraceMessageLog(TraceRecord trace)
		{
			if (trace != null && trace.IsMessageLogged)
			{
				bool xml = false;
				StringBuilder stringBuilder = new StringBuilder(trace.GetLoggedMessageString(out xml));
				stringBuilder.Insert(0, "<MessageLogged>");
				stringBuilder.Append("</MessageLogged>");
				stringBuilder.ToString();
				if (xml)
				{
					try
					{
						return new InternalMessageInfo
						{
							Action = trace.Action,
							To = trace.To
						};
					}
					catch (XmlException)
					{
						return null;
					}
				}
			}
			return null;
		}

		private void groupByInOutStripMenuItem_Click(object sender, EventArgs e)
		{
			if (traceGroupByStateController.CurrentStateName != "GroupByInOutState" && traceGroupByStateController.SwitchState("GroupByInOutState"))
			{
				ReGroupTraceList();
			}
			EnsureHighlight();
		}

		private void groupByProcessStripMenuItem_Click(object sender, EventArgs e)
		{
			if (traceGroupByStateController.CurrentStateName != "GroupByProcessState" && traceGroupByStateController.SwitchState("GroupByProcessState"))
			{
				ReGroupTraceList();
			}
			EnsureHighlight();
		}

		private void SortByColumn(bool isAsc, int columnIndex)
		{
			if (columnIndex == 1)
			{
				listTraces.ListViewItemSorter = new ListViewItemTagComparer(isAsc, ListViewItemTagComparerTarget.TraceTime);
			}
			else
			{
				listTraces.ListViewItemSorter = new ListViewItemStringComparer(columnIndex, isAsc);
			}
			listTraces.Columns[columnIndex].Tag = !isAsc;
			if (listTraces.VirtualMode)
			{
				InternalQuickSort(currentListViewItems, listTraces.ListViewItemSorter);
				listTraces.Invalidate();
				listTraces_SelectedIndexChanged(null, null);
			}
		}

		private void InternalQuickSort(List<ListViewItem> items, IComparer comparer)
		{
			if (items != null && items.Count > 1)
			{
				items.Sort(new InternalListViewItemComparer(comparer));
			}
		}

		private void EnsureHighlight()
		{
			listTraces.Select();
		}

		private void listTraces_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			bool flag = false;
			flag = (listTraces.Columns[e.Column].Tag != null && (bool)listTraces.Columns[e.Column].Tag);
			SortByColumn(flag, e.Column);
		}

		private void listTraces_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listTraces.VirtualMode && listTraces.SelectedIndices.Count != 0 && listTraces.SelectedIndices[0] >= 0 && listTraces.SelectedIndices[0] < currentListViewItems.Count)
			{
				MessageTraceItemClick(currentListViewItems[listTraces.SelectedIndices[0]].Tag, null);
			}
			else if (!listTraces.VirtualMode && listTraces.SelectedItems.Count != 0)
			{
				MessageTraceItemClick(listTraces.SelectedItems[0].Tag, null);
			}
			else
			{
				MessageTraceItemClick(null, null);
			}
		}

		private void listTraces_DoubleClick(object sender, EventArgs e)
		{
			if (listTraces.VirtualMode && listTraces.SelectedIndices.Count != 0 && listTraces.SelectedIndices[0] >= 0 && listTraces.SelectedIndices[0] < currentListViewItems.Count)
			{
				MessageTraceItemDoubleClick(currentListViewItems[listTraces.SelectedIndices[0]].Tag, null);
			}
			else if (!listTraces.VirtualMode && listTraces.SelectedItems.Count != 0)
			{
				MessageTraceItemDoubleClick(listTraces.SelectedItems[0].Tag, null);
			}
		}

		private void listTraces_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
		{
			e.Item = currentListViewItems[e.ItemIndex];
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
			listTraces = new System.Windows.Forms.ListView();
			dateTimeHeader = new System.Windows.Forms.ColumnHeader();
			executionHeader = new System.Windows.Forms.ColumnHeader();
			activityHeader = new System.Windows.Forms.ColumnHeader();
			actionHeader = new System.Windows.Forms.ColumnHeader();
			toHeader = new System.Windows.Forms.ColumnHeader();
			statusStrip = new System.Windows.Forms.StatusStrip();
			traceCountStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			groupByStripMenu = new System.Windows.Forms.MenuStrip();
			groupByStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			groupByNoneStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			groupByActivityStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			groupByProcessStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			groupByInOutStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			mainPanel.SuspendLayout();
			statusStrip.SuspendLayout();
			SuspendLayout();
			mainPanel.Controls.Add(listTraces);
			mainPanel.Controls.Add(groupByStripMenu);
			mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			mainPanel.Location = new System.Drawing.Point(0, 0);
			mainPanel.Name = "mainPanel";
			mainPanel.Size = new System.Drawing.Size(300, 533);
			mainPanel.TabIndex = 0;
			listTraces.AllowColumnReorder = true;
			listTraces.Columns.AddRange(new System.Windows.Forms.ColumnHeader[5]
			{
				actionHeader,
				dateTimeHeader,
				executionHeader,
				activityHeader,
				toHeader
			});
			listTraces.Dock = System.Windows.Forms.DockStyle.Fill;
			listTraces.FullRowSelect = true;
			listTraces.Location = new System.Drawing.Point(0, 25);
			listTraces.MultiSelect = false;
			listTraces.Name = "listTraces";
			listTraces.ShowItemToolTips = true;
			listTraces.Size = new System.Drawing.Size(300, 486);
			listTraces.TabIndex = 2;
			listTraces.HideSelection = false;
			listTraces.View = System.Windows.Forms.View.Details;
			listTraces.SelectedIndexChanged += new System.EventHandler(listTraces_SelectedIndexChanged);
			listTraces.DoubleClick += new System.EventHandler(listTraces_DoubleClick);
			listTraces.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(listTraces_ColumnClick);
			listTraces.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(listTraces_RetrieveVirtualItem);
			dateTimeHeader.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MsgView_Header0");
			dateTimeHeader.Width = 150;
			executionHeader.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MsgView_Header1");
			executionHeader.Width = 120;
			activityHeader.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MsgView_Header2");
			activityHeader.Width = 100;
			actionHeader.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MsgView_Header3");
			actionHeader.Width = 150;
			toHeader.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MsgView_Header4");
			statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[1]
			{
				traceCountStatusLabel
			});
			statusStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Table;
			statusStrip.Location = new System.Drawing.Point(0, 511);
			statusStrip.Name = "statusStrip";
			statusStrip.ShowItemToolTips = true;
			statusStrip.Size = new System.Drawing.Size(300, 22);
			statusStrip.SizingGrip = false;
			statusStrip.TabStop = false;
			traceCountStatusLabel.Name = "traceCountStatusLabel";
			traceCountStatusLabel.ToolTipText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MsgView_CountTip");
			groupByNoneStripMenuItem.Name = "groupMessageByNoneStripMenuItem";
			groupByNoneStripMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GNone");
			groupByNoneStripMenuItem.Click += new System.EventHandler(groupByNoneStripMenuItem_Click);
			groupByNoneStripMenuItem.ToolTipText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GroupByNoneTip");
			groupByActivityStripMenuItem.Name = "groupMessageByActivityStripMenuItem";
			groupByActivityStripMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GActivity");
			groupByActivityStripMenuItem.Click += new System.EventHandler(groupByActivityStripMenuItem_Click);
			groupByActivityStripMenuItem.ToolTipText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GroupByActivityTip");
			groupByProcessStripMenuItem.Name = "groupByProcessStripMenuItem";
			groupByProcessStripMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GProcess");
			groupByProcessStripMenuItem.Click += new System.EventHandler(groupByProcessStripMenuItem_Click);
			groupByProcessStripMenuItem.ToolTipText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GroupByProcessTip");
			groupByInOutStripMenuItem.Name = "groupByInOutStripMenuItem";
			groupByInOutStripMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GIO");
			groupByInOutStripMenuItem.Click += new System.EventHandler(groupByInOutStripMenuItem_Click);
			groupByInOutStripMenuItem.ToolTipText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GroupByInOutTip");
			groupByStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[4]
			{
				groupByNoneStripMenuItem,
				groupByActivityStripMenuItem,
				groupByProcessStripMenuItem,
				groupByInOutStripMenuItem
			});
			groupByStripMenuItem.Name = "groupMessageByStripMenuItem";
			groupByStripMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GroupBy");
			groupByStripMenuItem.ToolTipText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GroupByTip");
			groupByStripMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[1]
			{
				groupByStripMenuItem
			});
			groupByStripMenu.Location = new System.Drawing.Point(0, 0);
			groupByStripMenu.Name = "groupMessageByStripMenu";
			groupByStripMenu.Size = new System.Drawing.Size(636, 24);
			groupByStripMenu.TabIndex = 1;
			groupByStripMenu.ShowItemToolTips = true;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(mainPanel);
			base.Name = "MessageViewControl";
			base.Size = new System.Drawing.Size(300, 533);
			mainPanel.ResumeLayout(performLayout: false);
			mainPanel.PerformLayout();
			statusStrip.ResumeLayout(performLayout: false);
			ResumeLayout(performLayout: false);
		}
	}
}
