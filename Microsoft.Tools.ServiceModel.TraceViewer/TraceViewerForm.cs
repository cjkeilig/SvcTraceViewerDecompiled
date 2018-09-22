using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[ObjectStateMachine(typeof(EmptyProjectState), true, typeof(FileLoadingProjectState))]
	[ObjectStateMachine(typeof(FileLoadingProjectState), false, typeof(IdleProjectState))]
	[ObjectStateMachine(typeof(TraceLoadingProjectState), false, typeof(IdleProjectState))]
	[ObjectStateMachine(typeof(IdleProjectState), false, typeof(IdleProjectState))]
	[ObjectStateMachine(typeof(FileRemovingProjectState), false, typeof(IdleProjectState))]
	[ObjectStateMachine(typeof(FileReloadingProjectState), false, typeof(IdleProjectState))]
	[ObjectStateMachine(typeof(NoFileProjectState), false, typeof(IdleProjectState))]
	[ObjectStateMachine(typeof(LeftPanelActivityViewState), false, typeof(LeftPanelActivityViewState), "LeftPanelTableStateScope")]
	[ObjectStateMachine(typeof(LeftPanelProjectViewState), false, typeof(LeftPanelActivityViewState), "LeftPanelTableStateScope")]
	[ObjectStateMachine(typeof(LeftPanelTreeViewState), false, typeof(LeftPanelActivityViewState), "LeftPanelTableStateScope")]
	[ObjectStateMachine(typeof(LeftPanelMessageViewState), false, typeof(LeftPanelActivityViewState), "LeftPanelTableStateScope")]
	[ObjectStateMachine(typeof(FilePartialLoadingState), false, null, "FilePartialLoading")]
	[ObjectStateMachine(typeof(FileEntireLoadingState), false, null, "FilePartialLoading")]
	[ObjectStateMachine(typeof(SourceFileChangedState), false, typeof(SourceFileUnChangedState), "SourceFileChanged")]
	[ObjectStateMachine(typeof(SourceFileUnChangedState), false, typeof(SourceFileChangedState), "SourceFileChanged")]
	[ObjectStateMachine(typeof(GroupByNoneState), false, null, "TraceGroupBy")]
	[ObjectStateMachine(typeof(GroupBySourceFileState), false, null, "TraceGroupBy")]
	[ObjectStateMachine(typeof(TraceDataSourceIdleState), false, null, "TraceDataSourceState")]
	[ObjectStateMachine(typeof(TraceDataSourceValidatingState), false, typeof(TraceDataSourceIdleState), "TraceDataSourceState")]
	internal class TraceViewerForm : Form, IStateAwareObject, IPersistStatus
	{
		internal delegate void FindTextChanged(string newText);

		internal delegate void InternalBeginToolStripProgressBar(int expectedSteps);

		internal delegate void InternalCompleteToolStripProgressBar();

		internal delegate void InternalIndicateProgressToolStripProgressBar(int activities, int traces);

		internal delegate DialogResult InternalShowMessageBox(string message, string title, MessageBoxIcon icon, MessageBoxButtons btn);

		internal delegate void InternalStepProgressToolStripProgressBar();

		internal delegate void SelectedTraceRecordChanged(TraceRecord trace, string activityId);

		private class InternalActivitySelector
		{
			public string ActivityID;
		}

		private class InternalTraceRecordSelector
		{
			public bool IsHighlight = true;

			public Control NextFocus;

			public long TraceID;
		}

		internal delegate void DataSourceChanged(TraceDataSource dataSource);

		private enum TabFocusIndex
		{
			LeftPanelViewTab,
			TraceListStripMenu,
			TraceList,
			TraceDetailedTab
		}

		internal FindTextChanged FindTextChangedCallback;

		internal InternalBeginToolStripProgressBar InternalBeginToolStripProgressBarProxy;

		internal InternalCompleteToolStripProgressBar InternalCompleteToolStripProgressBarProxy;

		internal InternalIndicateProgressToolStripProgressBar InternalIndicateToolStripProgressBarProxy;

		internal InternalShowMessageBox InternalShowMessageBoxProxy;

		internal InternalStepProgressToolStripProgressBar InternalStepToolStripProgressBarProxy;

		internal StateMachineController LeftPanelStateController;

		internal StateMachineController ObjectStateController;

		internal StateMachineController PartialLoadingStateController;

		internal SelectedTraceRecordChanged SelectedTraceRecordChangedCallback;

		internal StateMachineController SourceFileChangedStateController;

		internal StateMachineController TraceDataSourceStateController;

		private const int ACTIVITY_LIST_DURATION_COLUMN_INDEX = 2;

		private const int ACTIVITY_LIST_NAME_COLUMN_INDEX = 0;

		private const int ACTIVITY_LIST_START_COLUMN_INDEX = 3;

		private const int ACTIVITY_LIST_STOP_COLUMN_INDEX = 4;

		private const int ACTIVITY_LIST_TRACECOUNT_COLUMN_INDEX = 1;

		private const int ACTIVITY_VIEW_INDEX = 0;

		private const int GRAPHIC_VIEW_INDEX = 3;

		private const int MAX_DISPLAYED_ACTIVITY_NAME_ON_TOOLBAR = 50;

		private const int MESSAGE_VIEW_INDEX = 2;

		private const int OLD_ICON_MAX_INDEX = 21;

		private const int PROJECT_VIEW_INDEX = 1;

		private const int TRACELIST_TAG_COLUMN_INDEX = 4;

		private const string chmName = "SvcTraceViewer.chm";

		private static Dictionary<string, string> activityIDToNameDictionary = new Dictionary<string, string>();

		private static TraceViewerForm currentTraceViewerForm = null;

		private static bool isThreadExecutionMode = false;

		private static TraceDataSource traceDataSource = null;

		private Dictionary<string, Activity> cachedActivities;

		private AppConfigManager configManager;

		private string defaultWindowTitle = SR.GetString("MainFrm_WindowTitle");

		private InternalActivitySelector intendSelectedActivity;

		private InternalTraceRecordSelector intendSelectedTrace;

		private InternalTraceRecordSelector intendSelectedTransferTrace;

		private ContextMenuStrip listTracesDetailMenuStrip;

		[UIToolStripItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private ToolStripMenuItem menuCopyToClipboard;

		[UIToolStripItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private ToolStripMenuItem menuCreateCustomFilter;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private MenuItem menuSelectAllActivities;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState",
			"TraceLoadingProjectState"
		})]
		private MenuItem menuSortByEndTime;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState",
			"TraceLoadingProjectState"
		})]
		private MenuItem menuSortByStartTime;

		private ProjectManager projectManager;

		private TraceViewerFormProxy thisProxy;

		private StateMachineController traceGroupByStateController;

		private List<ListViewItem> currentWholeTraceItems;

		private object thisLock = new object();

		internal DataSourceChanged DataSourceChangedHandler;

		private string[] startupCommandArgs;

		private static ImageList currentSharedImageList = null;

		private bool isSelectAllActivitiesState;

		private static Color[] alternativeTraceItemBackColor = new Color[2]
		{
			Color.FromArgb(234, 244, 255),
			SystemColors.Window
		};

		private FindDialog findDialog = new FindDialog();

		private const string DropDataFormat = "FileDrop";

		private IContainer components;

		private MainMenu mainMenu;

		private MenuItem fileMenu;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"EmptyProjectState",
			"IdleProjectState",
			"NoFileProjectState"
		})]
		private MenuItem fileAddMenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState",
			"NoFileProjectState"
		})]
		private MenuItem fileCloseAllMenuItem;

		private MenuItem fileSeparator1MenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"EmptyProjectState",
			"IdleProjectState",
			"NoFileProjectState"
		})]
		private MenuItem fileRecentMenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"EmptyProjectState",
			"IdleProjectState",
			"NoFileProjectState"
		})]
		private MenuItem fileProjectRecentMenuItem;

		private MenuItem fileSeparator2MenuItem;

		private MenuItem fileExitMenuItem;

		private MenuItem activityMenu;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private MenuItem activityStepToNextTransferMenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private MenuItem activityStepForwardMenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private MenuItem activityFollowTransferMenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private MenuItem activityStepBackwardMenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private MenuItem activityStepToPreviousTransferMenuItem;

		private MenuItem activitySeparatorMenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private MenuItem activityGraphViewMenuItem;

		private MenuItem viewMenu;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"LeftPanelActivityViewState"
		}, "Checked")]
		private MenuItem activityViewMenuItem;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"LeftPanelProjectViewState"
		}, "Checked")]
		private MenuItem projectViewMenuItem;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"LeftPanelMessageViewState"
		}, "Checked")]
		private MenuItem messageViewMenuItem;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"LeftPanelTreeViewState"
		}, "Checked")]
		private MenuItem treeViewMenuItem;

		private MenuItem viewSeperator1MenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState",
			"EmptyProjectState",
			"NoFileProjectState"
		})]
		private MenuItem viewFilterBarMenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState",
			"EmptyProjectState",
			"NoFileProjectState"
		})]
		private MenuItem viewFindBarMenuItem;

		private MenuItem viewSeperator2MenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private MenuItem filterNowMenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState",
			"EmptyProjectState",
			"NoFileProjectState"
		})]
		private MenuItem customFiltersMenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState",
			"EmptyProjectState",
			"NoFileProjectState"
		})]
		private MenuItem filterOptionsMenuItem;

		private MenuItem viewSeperator3MenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private MenuItem refreshMenuItem;

		private MenuItem editMenu;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private MenuItem findMenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private MenuItem findNextMenuItem;

		private MenuItem helpMenu;

		private MenuItem helpHelpMenuItem;

		private MenuItem helpAboutMenuItem;

		private ToolStripProgressBar toolStripProgressBar;

		private StatusStrip statusStrip;

		private ToolStripStatusLabel numActivities;

		private ToolStripStatusLabel numTraces;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"EmptyProjectState",
			"IdleProjectState",
			"NoFileProjectState"
		})]
		private MenuItem fileOpenProjectMenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState",
			"NoFileProjectState"
		})]
		private MenuItem fileSaveProjectMenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState",
			"NoFileProjectState"
		})]
		private MenuItem fileSaveProjectAsMenuItem;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState",
			"NoFileProjectState"
		})]
		private MenuItem fileCloseProjectMenuItem;

		private MenuItem fileSeparator3MenuItem;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"FileLoadingProjectState",
			"TraceLoadingProjectState",
			"FileRemovingProjectState",
			"FileReloadingProjectState"
		}, "Visible")]
		private ToolStripDropDownButton toolStripOperationsMenu;

		private ToolStripMenuItem toolStripOperationsCancelMenuItem;

		private ImageList imageList;

		[UIMenuItemEnablePropertyState(new string[]
		{
			"IdleProjectState",
			"EmptyProjectState",
			"NoFileProjectState"
		})]
		private MenuItem fileOpenMenuItem;

		[UIControlVisiblePropertyState(new string[]
		{
			"FilePartialLoadingState"
		})]
		private FilePartialLoadingStrip fileTimeRangeStrip;

		private TraceFilterControl traceFilterBar;

		private FindToolBar findToolbar;

		private Panel workAreaPanel;

		private Panel mainPanel;

		private Panel traceViewerMainPanel;

		private Panel rightPanel;

		private Panel traceDetailPanel;

		private TabControl tabs;

		private TabPage tabStylesheet;

		private TabPage tabBrowser;

		private TabPage tabMessage;

		private Splitter vertiSplitter;

		private Panel traceListPanel;

		private Panel xmlViewPanel;

		private Panel messageViewPanel;

		private RichTextXmlRenderer xmlViewRenderer;

		private RichTextXmlRenderer messageViewRenderer;

		[UIControlSpecialStateEventHandler(new string[]
		{
			"IdleProjectState",
			"FileLoadingProjectState",
			"FileReloadingProjectState"
		}, "listTracesDetail_ColumnClick", "ColumnClick", "listTracesDetail_ColumnClick")]
		[UIControlSpecialStateEventHandler(new string[]
		{
			"TraceLoadingProjectState"
		}, "listTracesDetail_ColumnClick_TraceLoadingState", "ColumnClick", "listTracesDetail_ColumnClick")]
		[UIControlSpecialStateEventHandler(new string[]
		{
			"IdleProjectState",
			"TraceLoadingProjectState"
		}, "listTracesDetail_SelectedIndexChanged", "SelectedIndexChanged", "listTracesDetail_SelectedIndexChanged")]
		[UIControlSpecialStateEventHandler(new string[]
		{
			"IdleProjectState"
		}, "listTracesDetail_DoubleClick", "DoubleClick", "listTracesDetail_DoubleClick")]
		private ListView listTracesDetail;

		private ColumnHeader traceListDescriptionColumn;

		private ColumnHeader traceListActivityNameColumn;

		private ColumnHeader traceListListColumn;

		private ColumnHeader traceListThreadIDColumn;

		private ColumnHeader traceListProcessNameColumn;

		private ColumnHeader traceListTimeColumn;

		private ColumnHeader traceListTraceCodeColumn;

		private ColumnHeader traceListSourceColumn;

		private Splitter horizSplitter;

		private MenuStrip traceListStripMenu;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"IdleProjectState",
			"EmptyProjectState",
			"NoFileProjectState"
		}, "Enabled")]
		private ToolStripMenuItem groupByStripMenuItem;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"GroupByNoneState"
		}, "Checked")]
		private ToolStripMenuItem groupByNoneStripMenuItem;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"GroupBySourceFileState"
		}, "Checked")]
		private ToolStripMenuItem groupBySourceFileStripMenuItem;

		[UIToolStripItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private ToolStripButton traceListCreateCustomFilterByTemplateStripButton;

		private ToolStripLabel activityNameStripLabel;

		[FieldObjectSetPropertyState(new string[]
		{
			"LeftPanelActivityViewState"
		}, "SelectedTab", "leftPanelActivityTab")]
		[FieldObjectSetPropertyState(new string[]
		{
			"LeftPanelProjectViewState"
		}, "SelectedTab", "leftPanelProjectTab")]
		[FieldObjectSetPropertyState(new string[]
		{
			"LeftPanelTreeViewState"
		}, "SelectedTab", "leftPanelTreeTab")]
		[FieldObjectSetPropertyState(new string[]
		{
			"LeftPanelMessageViewState"
		}, "SelectedTab", "leftPanelMessageViewTab")]
		private TabControl leftPanelTab;

		private TabPage leftPanelActivityTab;

		[UIControlSpecialStateEventHandler(new string[]
		{
			"IdleProjectState",
			"TraceLoadingProjectState"
		}, "listActivities_ColumnClick", "ColumnClick", "listActivities_ColumnClick")]
		[UIControlSpecialStateEventHandler(new string[]
		{
			"IdleProjectState",
			"TraceLoadingProjectState"
		}, "listActivities_SelectedIndexChanged", "SelectedIndexChanged", "listActivities_SelectedIndexChanged")]
		[UIControlSpecialStateEventHandler(new string[]
		{
			"IdleProjectState"
		}, "listActivities_KeyDown", "KeyDown", "listActivities_KeyDown")]
		[UIControlSpecialStateEventHandler(new string[]
		{
			"IdleProjectState"
		}, "listActivities_DoubleClick", "DoubleClick", "listActivities_DoubleClick")]
		private ListView listActivities;

		private ColumnHeader activityListActivityColumn;

		private ColumnHeader activityListDurationColumn;

		private ColumnHeader activityListTraceColumn;

		private ColumnHeader activityListStartTickColumn;

		private ColumnHeader activityListEndTickColumn;

		private TabPage leftPanelProjectTab;

		private ProjectTreeViewControl projectTree;

		private MessageViewControl messageView;

		private TabPage leftPanelTreeTab;

		private TabPage leftPanelMessageViewTab;

		private SwimLanesControl swimLanesControl;

		private TraceDetailInfoControl traceDetailInfoControl;

		public string DefaultWindowTitle => defaultWindowTitle;

		internal static bool IsThreadExecutionMode => isThreadExecutionMode;

		private Icon ApplicationIcon
		{
			get
			{
				Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(SR.GetString("ApplicationIconResourceName"));
				Icon result = null;
				if (manifestResourceStream != null)
				{
					result = new Icon(manifestResourceStream);
				}
				return result;
			}
		}

		private object ThisLock => thisLock;

		internal TraceDataSource DataSource
		{
			get
			{
				if (TraceViewerForm.traceDataSource == null)
				{
					TraceViewerForm.traceDataSource = new TraceDataSource(new ExceptionManager(), thisProxy, thisProxy);
					TraceViewerForm.traceDataSource.BeginInit();
					TraceViewerForm.traceDataSource.PartialLoadingStateController.RegisterStateSwitchListener(PartialLoadingStateController);
					TraceViewerForm.traceDataSource.SourceFileChangedStateController.RegisterStateSwitchListener(SourceFileChangedStateController);
					TraceViewerForm.traceDataSource.DataSourceStateController.RegisterStateSwitchListener(TraceDataSourceStateController);
					TraceDataSource traceDataSource = TraceViewerForm.traceDataSource;
					traceDataSource.SourceFileModifiedCallback = (TraceDataSource.SourceFileModified)Delegate.Combine(traceDataSource.SourceFileModifiedCallback, new TraceDataSource.SourceFileModified(DataSource_OnSourceFileModified));
					traceDataSource = TraceViewerForm.traceDataSource;
					traceDataSource.FileLoadingSelectedTimeRangeChangedCallback = (TraceDataSource.FileLoadingSelectedTimeRangeChanged)Delegate.Combine(traceDataSource.FileLoadingSelectedTimeRangeChangedCallback, new TraceDataSource.FileLoadingSelectedTimeRangeChanged(DataSource_OnFileLoadingSelectedTimeRangeChanged));
					traceDataSource = TraceViewerForm.traceDataSource;
					traceDataSource.FileLoadingTimeRangeChangedCallback = (TraceDataSource.FileLoadingTimeRangeChanged)Delegate.Combine(traceDataSource.FileLoadingTimeRangeChangedCallback, new TraceDataSource.FileLoadingTimeRangeChanged(DataSource_OnFileLoadingTimeRangeChanged));
					traceDataSource = TraceViewerForm.traceDataSource;
					traceDataSource.AppendFileBeginCallback = (TraceDataSource.AppendFileBegin)Delegate.Combine(traceDataSource.AppendFileBeginCallback, new TraceDataSource.AppendFileBegin(DataSource_OnAppendFilesBegin));
					traceDataSource = TraceViewerForm.traceDataSource;
					traceDataSource.AppendFileFinishedCallback = (TraceDataSource.AppendFileFinished)Delegate.Combine(traceDataSource.AppendFileFinishedCallback, new TraceDataSource.AppendFileFinished(DataSource_OnAppendFilesFinished));
					traceDataSource = TraceViewerForm.traceDataSource;
					traceDataSource.RemoveFileBeginCallback = (TraceDataSource.RemoveFileBegin)Delegate.Combine(traceDataSource.RemoveFileBeginCallback, new TraceDataSource.RemoveFileBegin(DataSource_OnRemoveFileBegin));
					traceDataSource = TraceViewerForm.traceDataSource;
					traceDataSource.RemoveFileFinishedCallback = (TraceDataSource.RemoveFileFinished)Delegate.Combine(traceDataSource.RemoveFileFinishedCallback, new TraceDataSource.RemoveFileFinished(DataSource_OnRemoveFileFinished));
					traceDataSource = TraceViewerForm.traceDataSource;
					traceDataSource.RemoveAllFileFinishedCallback = (TraceDataSource.RemoveAllFileFinished)Delegate.Combine(traceDataSource.RemoveAllFileFinishedCallback, new TraceDataSource.RemoveAllFileFinished(DataSource_OnRemoveAllFilesFinished));
					traceDataSource = TraceViewerForm.traceDataSource;
					traceDataSource.ActivitiesAppendedCallback = (TraceDataSource.ActivitiesAppended)Delegate.Combine(traceDataSource.ActivitiesAppendedCallback, new TraceDataSource.ActivitiesAppended(DataSource_OnActivitiesAppended));
					traceDataSource = TraceViewerForm.traceDataSource;
					traceDataSource.TraceRecordReadyCallback = (TraceDataSource.TraceRecordReady)Delegate.Combine(traceDataSource.TraceRecordReadyCallback, new TraceDataSource.TraceRecordReady(DataSource_OnTraceDataReady));
					traceDataSource = TraceViewerForm.traceDataSource;
					traceDataSource.TraceRecordLoadingFinishedCallback = (TraceDataSource.TraceRecordLoadingFinished)Delegate.Combine(traceDataSource.TraceRecordLoadingFinishedCallback, new TraceDataSource.TraceRecordLoadingFinished(DataSource_OnTraceLoadFinished));
					traceDataSource = TraceViewerForm.traceDataSource;
					traceDataSource.TraceRecordLoadingBeginCallback = (TraceDataSource.TraceRecordLoadingBegin)Delegate.Combine(traceDataSource.TraceRecordLoadingBeginCallback, new TraceDataSource.TraceRecordLoadingBegin(DataSource_OnTraceLoadBegin));
					try
					{
						if (DataSourceChangedHandler != null)
						{
							DataSourceChangedHandler(TraceViewerForm.traceDataSource);
						}
					}
					catch (Exception e)
					{
						ExceptionManager.GeneralExceptionFilter(e);
					}
					TraceViewerForm.traceDataSource.EngInit();
				}
				return TraceViewerForm.traceDataSource;
			}
		}

		internal Activity CurrentSelectedActivity
		{
			get
			{
				if (listActivities.SelectedItems.Count == 1)
				{
					return (Activity)listActivities.SelectedItems[0].Tag;
				}
				return null;
			}
		}

		internal TraceRecord CurrentSelectedTraceRecord
		{
			get
			{
				if (CurrentSelectedTraceListItem != null)
				{
					return (TraceRecord)CurrentSelectedTraceListItem.Tag;
				}
				return null;
			}
		}

		internal bool IsActivityGraphMode
		{
			get
			{
				if (LeftPanelStateController.CurrentStateName == "LeftPanelTreeViewState" || LeftPanelStateController.CurrentStateName == "LeftPanelProjectViewState")
				{
					return true;
				}
				return false;
			}
		}

		private ListViewItem CurrentSelectedTraceListItem
		{
			get
			{
				if (listTracesDetail.VirtualMode && listTracesDetail.SelectedIndices.Count != 0 && listTracesDetail.SelectedIndices[0] >= 0 && listTracesDetail.SelectedIndices[0] < currentWholeTraceItems.Count)
				{
					return currentWholeTraceItems[listTracesDetail.SelectedIndices[0]];
				}
				if (!listTracesDetail.VirtualMode && IsTraceSelected)
				{
					return listTracesDetail.SelectedItems[0];
				}
				return null;
			}
		}

		private int CurrentTraceListCount
		{
			get
			{
				if (listTracesDetail.VirtualMode && currentWholeTraceItems != null)
				{
					return currentWholeTraceItems.Count;
				}
				return listTracesDetail.Items.Count;
			}
		}

		private bool IsTraceSelected
		{
			get
			{
				if (listTracesDetail.SelectedIndices != null)
				{
					return listTracesDetail.SelectedIndices.Count != 0;
				}
				return false;
			}
		}

		void IStateAwareObject.PostStateSwitch(ObjectStateBase fromState, ObjectStateBase toState)
		{
			if (toState != null && toState.StateName == "IdleProjectState")
			{
				CheckCreateCustomFilterByTemplateStripButtonStatus();
			}
		}

		void IStateAwareObject.PreStateSwitch(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		void IStateAwareObject.StateSwitchFailed(ObjectStateBase fromState, ObjectStateBase toState, ObjectStateSwitchFailReason reason)
		{
		}

		void IStateAwareObject.StateSwitchSuccess(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		public void OutputToStream(XmlTextWriter writer)
		{
			if (writer != null)
			{
				try
				{
					writer.WriteStartElement("mainFormSettings");
					writer.WriteStartElement("window_state");
					writer.WriteString((base.WindowState == FormWindowState.Maximized) ? "1" : ((base.WindowState == FormWindowState.Minimized) ? "-1" : "0"));
					writer.WriteEndElement();
					writer.WriteStartElement("location_x");
					writer.WriteString(base.Location.X.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("location_y");
					writer.WriteString(base.Location.Y.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("size_width");
					writer.WriteString(base.Size.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("size_height");
					writer.WriteString(base.Size.Height.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("left_size_width");
					writer.WriteString(leftPanelTab.Size.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("left_size_height");
					writer.WriteString(leftPanelTab.Size.Height.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("viewFilterBarMenuItem");
					if (!viewFilterBarMenuItem.Checked)
					{
						writer.WriteString("0");
					}
					else
					{
						writer.WriteString("1");
					}
					writer.WriteEndElement();
					writer.WriteStartElement("viewFindBarMenuItem");
					if (!viewFindBarMenuItem.Checked)
					{
						writer.WriteString("0");
					}
					else
					{
						writer.WriteString("1");
					}
					writer.WriteEndElement();
					writer.WriteStartElement("activityListActivityColumnWidth");
					writer.WriteString(activityListActivityColumn.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("activityListDurationColumnWidth");
					writer.WriteString(activityListDurationColumn.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("activityListTraceColumnWidth");
					writer.WriteString(activityListTraceColumn.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("activityListStartTickColumnWidth");
					writer.WriteString(activityListStartTickColumn.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("activityListEndTickColumnWidth");
					writer.WriteString(activityListEndTickColumn.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("traceListDescriptionColumnWidth");
					writer.WriteString(traceListDescriptionColumn.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("traceListActivityNameColumnWidth");
					writer.WriteString(traceListActivityNameColumn.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("traceListListColumnWidth");
					writer.WriteString(traceListListColumn.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("traceListThreadIDColumnWidth");
					writer.WriteString(traceListThreadIDColumn.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("traceListProcessNameColumnWidth");
					writer.WriteString(traceListProcessNameColumn.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("traceListTimeColumnWidth");
					writer.WriteString(traceListTimeColumn.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("traceListTraceCodeColumnWidth");
					writer.WriteString(traceListTraceCodeColumn.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("traceListSourceColumnWidth");
					writer.WriteString(traceListSourceColumn.Width.ToString(CultureInfo.InvariantCulture));
					writer.WriteEndElement();
					writer.WriteEndElement();
				}
				catch (XmlException)
				{
					throw new AppSettingsException(SR.GetString("MsgAppSettingSaveError"), null);
				}
			}
		}

		internal static string GetActivityDisplayName(Activity activity)
		{
			if (activity != null)
			{
				if (!activityIDToNameDictionary.ContainsKey(activity.Id))
				{
					return Activity.ShortID(activity.Id);
				}
				return activityIDToNameDictionary[activity.Id];
			}
			return null;
		}

		internal static string GetActivityDisplayName(string activityId)
		{
			if (!string.IsNullOrEmpty(activityId))
			{
				if (!activityIDToNameDictionary.ContainsKey(activityId))
				{
					return Activity.ShortID(activityId);
				}
				return activityIDToNameDictionary[activityId];
			}
			return null;
		}

		internal static bool IsActivityDisplayNameInCache(string activityId)
		{
			if (!string.IsNullOrEmpty(activityId) && activityIDToNameDictionary.ContainsKey(activityId))
			{
				return true;
			}
			return false;
		}

		private void InternalBeginToolStripProgressBarImpl(int expectedSteps)
		{
			toolStripProgressBar.Visible = true;
			toolStripProgressBar.Minimum = 0;
			toolStripProgressBar.Maximum = expectedSteps;
			toolStripOperationsCancelMenuItem.Invalidate();
		}

		private void InternalCompleteToolStripProgressBarImpl()
		{
			toolStripProgressBar.Value = 0;
			toolStripProgressBar.Visible = false;
		}

		private void InternalIndicateToolStripProgressBarImpl(int activities, int traces)
		{
			numActivities.Text = SR.GetString("MainFrm_Activities") + activities.ToString(CultureInfo.CurrentCulture);
			numTraces.Text = SR.GetString("MainFrm_Traces") + traces.ToString(CultureInfo.CurrentCulture);
			numActivities.AccessibleName = numActivities.Text;
			numTraces.AccessibleName = numTraces.Text;
			statusStrip.Invalidate();
			statusStrip.Update();
		}

		private DialogResult InternalShowMessageBoxImpl(string message, string title, MessageBoxIcon icon, MessageBoxButtons btn)
		{
			return MessageBox.Show(this, message, string.IsNullOrEmpty(title) ? defaultWindowTitle : title, btn, icon, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
		}

		private void InternalStepToolStripProgressBarImpl()
		{
			toolStripProgressBar.Increment(1);
			if (toolStripProgressBar.Value >= toolStripProgressBar.Maximum)
			{
				toolStripProgressBar.Visible = false;
			}
		}

		public void RestoreFromXMLNode(XmlNode node)
		{
			if (node != null)
			{
				XmlElement xmlElement = node["window_state"];
				if (xmlElement != null)
				{
					try
					{
						switch (int.Parse(xmlElement.InnerText, CultureInfo.InvariantCulture))
						{
						case 1:
							base.WindowState = FormWindowState.Maximized;
							break;
						case -1:
							base.WindowState = FormWindowState.Minimized;
							break;
						default:
						{
							XmlElement xmlElement2 = node["location_x"];
							XmlElement xmlElement3 = node["location_y"];
							if (xmlElement2 != null && xmlElement3 != null)
							{
								try
								{
									base.Location = new Point(int.Parse(xmlElement2.InnerText, CultureInfo.InvariantCulture), int.Parse(xmlElement3.InnerText, CultureInfo.InvariantCulture));
								}
								catch (Exception e)
								{
									ExceptionManager.GeneralExceptionFilter(e);
								}
							}
							XmlElement xmlElement4 = node["size_width"];
							XmlElement xmlElement5 = node["size_height"];
							if (xmlElement4 != null && xmlElement5 != null)
							{
								try
								{
									base.Size = new Size(int.Parse(xmlElement4.InnerText, CultureInfo.InvariantCulture), int.Parse(xmlElement5.InnerText, CultureInfo.InvariantCulture));
								}
								catch (Exception e2)
								{
									ExceptionManager.GeneralExceptionFilter(e2);
								}
							}
							break;
						}
						}
					}
					catch (Exception e3)
					{
						ExceptionManager.GeneralExceptionFilter(e3);
					}
				}
				XmlElement xmlElement6 = node["left_size_width"];
				XmlElement xmlElement7 = node["left_size_height"];
				if (xmlElement6 != null && xmlElement7 != null)
				{
					try
					{
						leftPanelTab.Size = new Size(int.Parse(xmlElement6.InnerText, CultureInfo.InvariantCulture), int.Parse(xmlElement7.InnerText, CultureInfo.InvariantCulture));
					}
					catch (Exception e4)
					{
						ExceptionManager.GeneralExceptionFilter(e4);
					}
				}
				XmlElement xmlElement8 = node["viewFilterBarMenuItem"];
				if (xmlElement8 != null)
				{
					if (xmlElement8.InnerText == "0")
					{
						EnableFilterBar(isEnabling: false);
					}
					else
					{
						EnableFilterBar(isEnabling: true);
					}
				}
				XmlElement xmlElement9 = node["viewFindBarMenuItem"];
				if (xmlElement9 != null)
				{
					if (xmlElement9.InnerText == "0")
					{
						EnableFindBar(isEnabling: false);
					}
					else
					{
						EnableFindBar(isEnabling: true);
					}
				}
				XmlElement xmlElement10 = node["activityListActivityColumnWidth"];
				if (xmlElement10 != null && !string.IsNullOrEmpty(xmlElement10.InnerText))
				{
					try
					{
						int width = int.Parse(xmlElement10.InnerText, CultureInfo.InvariantCulture);
						activityListActivityColumn.Width = width;
					}
					catch (FormatException)
					{
					}
					catch (OverflowException)
					{
					}
				}
				XmlElement xmlElement11 = node["activityListDurationColumnWidth"];
				if (xmlElement11 != null && !string.IsNullOrEmpty(xmlElement11.InnerText))
				{
					try
					{
						int width2 = int.Parse(xmlElement11.InnerText, CultureInfo.InvariantCulture);
						activityListDurationColumn.Width = width2;
					}
					catch (FormatException)
					{
					}
					catch (OverflowException)
					{
					}
				}
				XmlElement xmlElement12 = node["activityListTraceColumnWidth"];
				if (xmlElement12 != null && !string.IsNullOrEmpty(xmlElement12.InnerText))
				{
					try
					{
						int width3 = int.Parse(xmlElement12.InnerText, CultureInfo.InvariantCulture);
						activityListTraceColumn.Width = width3;
					}
					catch (FormatException)
					{
					}
					catch (OverflowException)
					{
					}
				}
				XmlElement xmlElement13 = node["activityListStartTickColumnWidth"];
				if (xmlElement13 != null && !string.IsNullOrEmpty(xmlElement13.InnerText))
				{
					try
					{
						int width4 = int.Parse(xmlElement13.InnerText, CultureInfo.InvariantCulture);
						activityListStartTickColumn.Width = width4;
					}
					catch (FormatException)
					{
					}
					catch (OverflowException)
					{
					}
				}
				XmlElement xmlElement14 = node["activityListEndTickColumnWidth"];
				if (xmlElement14 != null && !string.IsNullOrEmpty(xmlElement14.InnerText))
				{
					try
					{
						int width5 = int.Parse(xmlElement14.InnerText, CultureInfo.InvariantCulture);
						activityListEndTickColumn.Width = width5;
					}
					catch (FormatException)
					{
					}
					catch (OverflowException)
					{
					}
				}
				XmlElement xmlElement15 = node["traceListDescriptionColumnWidth"];
				if (xmlElement15 != null && !string.IsNullOrEmpty(xmlElement15.InnerText))
				{
					try
					{
						int width6 = int.Parse(xmlElement15.InnerText, CultureInfo.InvariantCulture);
						traceListDescriptionColumn.Width = width6;
					}
					catch (FormatException)
					{
					}
					catch (OverflowException)
					{
					}
				}
				XmlElement xmlElement16 = node["traceListActivityNameColumnWidth"];
				if (xmlElement16 != null && !string.IsNullOrEmpty(xmlElement16.InnerText))
				{
					try
					{
						int width7 = int.Parse(xmlElement16.InnerText, CultureInfo.InvariantCulture);
						traceListActivityNameColumn.Width = width7;
					}
					catch (FormatException)
					{
					}
					catch (OverflowException)
					{
					}
				}
				XmlElement xmlElement17 = node["traceListListColumnWidth"];
				if (xmlElement17 != null && !string.IsNullOrEmpty(xmlElement17.InnerText))
				{
					try
					{
						int width8 = int.Parse(xmlElement17.InnerText, CultureInfo.InvariantCulture);
						traceListListColumn.Width = width8;
					}
					catch (FormatException)
					{
					}
					catch (OverflowException)
					{
					}
				}
				XmlElement xmlElement18 = node["traceListThreadIDColumnWidth"];
				if (xmlElement18 != null && !string.IsNullOrEmpty(xmlElement18.InnerText))
				{
					try
					{
						int width9 = int.Parse(xmlElement18.InnerText, CultureInfo.InvariantCulture);
						traceListThreadIDColumn.Width = width9;
					}
					catch (FormatException)
					{
					}
					catch (OverflowException)
					{
					}
				}
				XmlElement xmlElement19 = node["traceListProcessNameColumnWidth"];
				if (xmlElement19 != null && !string.IsNullOrEmpty(xmlElement19.InnerText))
				{
					try
					{
						int width10 = int.Parse(xmlElement19.InnerText, CultureInfo.InvariantCulture);
						traceListProcessNameColumn.Width = width10;
					}
					catch (FormatException)
					{
					}
					catch (OverflowException)
					{
					}
				}
				XmlElement xmlElement20 = node["traceListTimeColumnWidth"];
				if (xmlElement20 != null && !string.IsNullOrEmpty(xmlElement20.InnerText))
				{
					try
					{
						int width11 = int.Parse(xmlElement20.InnerText, CultureInfo.InvariantCulture);
						traceListTimeColumn.Width = width11;
					}
					catch (FormatException)
					{
					}
					catch (OverflowException)
					{
					}
				}
				XmlElement xmlElement21 = node["traceListTraceCodeColumnWidth"];
				if (xmlElement21 != null && !string.IsNullOrEmpty(xmlElement21.InnerText))
				{
					try
					{
						int width12 = int.Parse(xmlElement21.InnerText, CultureInfo.InvariantCulture);
						traceListTraceCodeColumn.Width = width12;
					}
					catch (FormatException)
					{
					}
					catch (OverflowException)
					{
					}
				}
				XmlElement xmlElement22 = node["traceListSourceColumnWidth"];
				if (xmlElement22 != null && !string.IsNullOrEmpty(xmlElement22.InnerText))
				{
					try
					{
						int width13 = int.Parse(xmlElement22.InnerText, CultureInfo.InvariantCulture);
						traceListSourceColumn.Width = width13;
					}
					catch (FormatException)
					{
					}
					catch (OverflowException)
					{
					}
				}
			}
		}

		public bool IsCurrentPersistNode(XmlNode node)
		{
			if (node != null && node.Name == "mainFormSettings")
			{
				return true;
			}
			return false;
		}

		internal IUserInterfaceProvider GetInterfaceProvider()
		{
			return thisProxy;
		}

		private void DataSource_OnFileLoadingSelectedTimeRangeChanged(DateTime start, DateTime end)
		{
			fileTimeRangeStrip.RefreshSelectedTimeRange(start, end);
		}

		private void DataSource_OnFileLoadingTimeRangeChanged(DateTime start, DateTime end)
		{
			fileTimeRangeStrip.RefreshTimeRange(start, end);
		}

		private void DataSource_OnSourceFileModified(string fileName)
		{
			GetInterfaceProvider().ShowMessageBox(SR.GetString("MainFrm_ReloadFile"), string.Empty, MessageBoxIcon.Asterisk, MessageBoxButtons.OK);
		}

		private void PreserveUserSelections()
		{
			intendSelectedTrace = null;
			intendSelectedActivity = null;
			if (!IsTraceSelected)
			{
				if (listActivities.SelectedItems.Count != 0)
				{
					intendSelectedActivity = new InternalActivitySelector();
					intendSelectedActivity.ActivityID = ((Activity)listActivities.SelectedItems[0].Tag).Id;
				}
			}
			else
			{
				intendSelectedTrace = new InternalTraceRecordSelector();
				intendSelectedTrace.TraceID = CurrentSelectedTraceRecord.TraceID;
				intendSelectedActivity = new InternalActivitySelector();
				if (!CurrentSelectedTraceRecord.IsTransfer)
				{
					intendSelectedActivity.ActivityID = CurrentSelectedTraceRecord.ActivityID;
				}
				else
				{
					foreach (ListViewItem selectedItem in listActivities.SelectedItems)
					{
						if (((Activity)selectedItem.Tag).Id == CurrentSelectedTraceRecord.ActivityID || ((Activity)selectedItem.Tag).Id == CurrentSelectedTraceRecord.RelatedActivityID)
						{
							intendSelectedActivity.ActivityID = ((Activity)selectedItem.Tag).Id;
							break;
						}
					}
				}
			}
		}

		private void DataSource_OnActivitiesAppended(List<Activity> activities)
		{
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				lock (ThisLock)
				{
					listActivities.BeginUpdate();
					Dictionary<string, Activity> dictionary = new Dictionary<string, Activity>();
					foreach (Activity activity in activities)
					{
						if (cachedActivities == null)
						{
							cachedActivities = new Dictionary<string, Activity>();
						}
						if (!cachedActivities.ContainsKey(activity.Id))
						{
							cachedActivities.Add(activity.Id, activity);
							dictionary.Add(activity.Id, activity);
						}
					}
					RefreshActivityList(dictionary, isClear: false);
				}
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
			finally
			{
				listActivities.EndUpdate();
			}
		}

		private void DataSource_OnAppendFilesBegin(string[] filePaths)
		{
			ObjectStateController.SwitchState("FileLoadingProjectState");
		}

		private void SetListViewItemFocus(ListViewItem lvi)
		{
			if (lvi != null)
			{
				lvi.Focused = true;
				lvi.Selected = true;
				lvi.EnsureVisible();
			}
		}

		private void DataSource_OnAppendFilesFinished(string[] fileNames, TaskInfoBase task)
		{
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				ResetWindowTitle();
				UpdateActivities();
				if (DataSource.LoadedFileNames != null && DataSource.LoadedFileNames.Count != 0)
				{
					ObjectStateController.SwitchState();
				}
				else
				{
					ObjectStateController.SwitchState("NoFileProjectState");
				}
				if (intendSelectedActivity != null)
				{
					foreach (ListViewItem item in listActivities.Items)
					{
						if (intendSelectedActivity.ActivityID == ((Activity)item.Tag).Id)
						{
							SetListViewItemFocus(item);
							listActivities.Select();
						}
					}
					intendSelectedActivity = null;
				}
				else
				{
					listActivities_SelectedIndexChanged(null, null);
				}
				if (task.GetExceptionList.Count != 0)
				{
					FileLoadingReport fileLoadingReport = new FileLoadingReport();
					fileLoadingReport.Initialize(task.GetExceptionList);
					thisProxy.ShowDialog(fileLoadingReport, this);
				}
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
		}

		private bool DataSource_OnTraceDataReady(List<TraceRecord> traceRecords, Activity activity, List<Activity> activities)
		{
			lock (ThisLock)
			{
				foreach (TraceRecord traceRecord in traceRecords)
				{
					if (traceRecord != null)
					{
						RuntimeHelpers.PrepareConstrainedRegions();
						try
						{
							Images images = Images.Unknown;
							if (traceRecord.IsMessageSentRecord)
							{
								images = Images.MessageSentTrace;
							}
							else if (traceRecord.IsMessageReceivedRecord)
							{
								images = Images.MessageReceiveTrace;
							}
							else if (traceRecord.IsMessageLogged)
							{
								images = Images.Message;
							}
							if (traceRecord.IsFunctionRelated)
							{
								images = ((traceRecord.CallingDirection != 0) ? Images.Reply : Images.Call);
							}
							switch (traceRecord.Level)
							{
							case TraceEventType.Start:
								images = Images.ActivityStartTrace;
								break;
							case TraceEventType.Stop:
								images = Images.ActivityStopTrace;
								break;
							case TraceEventType.Resume:
								images = Images.ActivityResumeTrace;
								break;
							case TraceEventType.Suspend:
								images = Images.ActivityPauseTrace;
								break;
							case TraceEventType.Critical:
							case TraceEventType.Error:
								images = Images.ErrorTrace;
								break;
							case TraceEventType.Warning:
								images = Images.WarningTrace;
								break;
							}
							string empty = string.Empty;
							if (traceRecord.IsTransfer)
							{
								if (activity.Id == traceRecord.ActivityID)
								{
									empty = ((activities.Count != 1) ? (GetActivityDisplayName(traceRecord.ActivityID) + "->" + GetActivityDisplayName(traceRecord.RelatedActivityID)) : (SR.GetString("MainFrm_ToString") + GetActivityDisplayName(traceRecord.RelatedActivityID)));
									images = Images.TransferOut;
								}
								else if (activity.Id == traceRecord.RelatedActivityID)
								{
									empty = ((activities.Count != 1) ? (GetActivityDisplayName(traceRecord.ActivityID) + "->" + GetActivityDisplayName(traceRecord.RelatedActivityID)) : (SR.GetString("MainFrm_FromString") + GetActivityDisplayName(traceRecord.ActivityID)));
									images = Images.TransferIn;
								}
								else
								{
									empty = GetActivityDisplayName(traceRecord.ActivityID) + "->" + GetActivityDisplayName(traceRecord.RelatedActivityID);
									images = Images.Transfer;
								}
							}
							else
							{
								empty = ((traceRecord.Description == null) ? string.Empty : traceRecord.Description);
							}
							ListViewItem listViewItem = null;
							listViewItem = ((images != Images.Unknown) ? new ListViewItem(new string[8]
							{
								empty,
								traceRecord.Level.ToString(),
								traceRecord.ThreadId.ToString(CultureInfo.CurrentCulture),
								traceRecord.ProcessName,
								Utilities.GetShortTimeStringFromDateTime(traceRecord.Time),
								(traceRecord.TraceCode == null) ? string.Empty : traceRecord.TraceCode,
								string.Empty,
								traceRecord.SourceName
							}, (int)((images < Images.ActivityStartTrace) ? images : (images - 1))) : new ListViewItem(new string[8]
							{
								empty,
								traceRecord.Level.ToString(),
								traceRecord.ThreadId.ToString(CultureInfo.CurrentCulture),
								traceRecord.ProcessName,
								Utilities.GetShortTimeStringFromDateTime(traceRecord.Time),
								(traceRecord.TraceCode == null) ? string.Empty : traceRecord.TraceCode,
								string.Empty,
								traceRecord.SourceName
							}));
							if (currentWholeTraceItems != null)
							{
								currentWholeTraceItems.Add(listViewItem);
							}
							listViewItem.Tag = traceRecord;
							listViewItem.ToolTipText = empty + SR.GetString("MainFrm_ReturnSingle") + SR.GetString("MainFrm_ReturnSingle") + (traceRecord.IsTransfer ? (SR.GetString("MainFrm_TraceFromActivity") + GetActivityDisplayName(traceRecord.ActivityID) + SR.GetString("MainFrm_ReturnSingle") + SR.GetString("MainFrm_TabSingle") + GetActivityDisplayName(traceRecord.RelatedActivityID)) : (SR.GetString("MainFrm_TraceFromActivity") + GetActivityDisplayName(traceRecord.ActivityID))) + SR.GetString("MainFrm_ReturnSingle") + SR.GetString("MainFrm_ReturnSingle") + SR.GetString("MainFrm_TraceFrom") + traceRecord.TraceRecordPos.RelatedFileDescriptor.FilePath;
							if (((TraceEventType)3 & traceRecord.Level) != 0)
							{
								listViewItem.ForeColor = Color.Red;
								ListViewItem listViewItem2 = listViewItem;
								listViewItem2.Font = new Font(listViewItem2.Font, listViewItem.Font.Style | FontStyle.Bold);
							}
							else if (((TraceEventType)7 & traceRecord.Level) != 0)
							{
								listViewItem.BackColor = Color.Yellow;
								ListViewItem listViewItem3 = listViewItem;
								listViewItem3.Font = new Font(listViewItem3.Font, listViewItem.Font.Style | FontStyle.Bold);
							}
							if (CurrentTraceListCount <= 100)
							{
								listTracesDetail.Items.Add(listViewItem);
							}
							InternalStepToolStripProgressBarImpl();
						}
						catch (Exception e)
						{
							ExceptionManager.GeneralExceptionFilter(e);
						}
					}
				}
			}
			return true;
		}

		[SecurityCritical]
		public TraceViewerForm(string[] args)
		{
			currentTraceViewerForm = this;
			InitializeComponent();
			InitializeInternalImpls();
			thisProxy = new TraceViewerFormProxy(this);
			ExceptionManager.Initialize(thisProxy);
			TempFileManager.Initialize();
			configManager = AppConfigManager.GetInstance(new ExceptionManager(), thisProxy);
			FilterEngine.SetCustomFilterOptionSettingsReference(configManager.LoadCustomFilterOptionSettings());
			AdjustSettings();
			this.projectManager = new ProjectManager(this);
			ProjectManager projectManager = this.projectManager;
			projectManager.ProjectChangedCallback = (ProjectManager.ProjectChanged)Delegate.Combine(projectManager.ProjectChangedCallback, new ProjectManager.ProjectChanged(ProjectManager_OnProjectNameChanged));
			ObjectStateController = new StateMachineController(this);
			LeftPanelStateController = new StateMachineController(this, "LeftPanelTableStateScope");
			LeftPanelStateController.SwitchState("LeftPanelActivityViewState");
			PartialLoadingStateController = new StateMachineController(this, "FilePartialLoading");
			PartialLoadingStateController.SwitchState("FileEntireLoadingState");
			SourceFileChangedStateController = new StateMachineController(this, "SourceFileChanged");
			SourceFileChangedStateController.SwitchState("SourceFileUnChangedState");
			traceGroupByStateController = new StateMachineController(this, "TraceGroupBy");
			traceGroupByStateController.SwitchState("GroupByNoneState");
			ReGroupTraceList();
			TraceDataSourceStateController = new StateMachineController(this, "TraceDataSourceState");
			TraceDataSourceStateController.SwitchState("TraceDataSourceIdleState");
			InitializeInternalComponents();
			ResetWindowTitle();
			findDialog.Initialize(this);
			if (args != null && args.Length != 0)
			{
				startupCommandArgs = args;
			}
		}

		private void DataSource_OnRemoveAllFilesFinished()
		{
			ClearAll();
		}

		private void DataSource_OnRemoveFileBegin(string[] filesName)
		{
			ObjectStateController.SwitchState("FileRemovingProjectState");
			ClearAll();
		}

		private void DataSource_OnRemoveFileFinished(string[] filesName)
		{
			ResetWindowTitle();
			UpdateActivities();
			List<string> loadedFileNames = DataSource.LoadedFileNames;
			if (loadedFileNames == null || loadedFileNames.Count == 0)
			{
				if (string.IsNullOrEmpty(projectManager.CurrentProjectFilePath))
				{
					ObjectStateController.SwitchState("EmptyProjectState");
				}
				else
				{
					ObjectStateController.SwitchState("NoFileProjectState");
				}
			}
			else
			{
				ObjectStateController.SwitchState("IdleProjectState");
			}
			UpdateCurrentTraceForLoadedActivityList();
		}

		private void DataSource_OnTraceLoadBegin(List<Activity> activities)
		{
			ObjectStateController.SwitchState("TraceLoadingProjectState");
			ClearTraceList();
			int num = 0;
			lock (ThisLock)
			{
				currentWholeTraceItems = new List<ListViewItem>();
				if (activities != null)
				{
					foreach (Activity activity in activities)
					{
						num += activity.TraceCount;
					}
				}
				InternalBeginToolStripProgressBarImpl(num);
			}
		}

		private void DataSource_OnTraceLoadFinished(List<Activity> activities)
		{
			lock (ThisLock)
			{
				InternalCompleteToolStripProgressBarImpl();
				ReGroupTraceList();
				listActivities.Select();
				HighlightTraceRecord();
				UpdateCurrentTraceForLoadedActivityList(activities);
				ObjectStateController.SwitchState();
				intendSelectedTransferTrace = null;
				intendSelectedTrace = null;
				SetFindDialogState();
			}
		}

		private void TraceViewerForm_Load(object sender, EventArgs e)
		{
			OpenFileOrProjectByStartupCommand();
		}

		private void OpenFileOrProjectByStartupCommand()
		{
			if (startupCommandArgs != null && startupCommandArgs.Length != 0)
			{
				try
				{
					if (startupCommandArgs.Length == 1 && startupCommandArgs[0] != null && startupCommandArgs[0].EndsWith(SR.GetString("PJ_Extension"), StringComparison.OrdinalIgnoreCase))
					{
						FileInfo fileInfo = null;
						try
						{
							fileInfo = Utilities.CreateFileInfoHelper(startupCommandArgs[0]);
						}
						catch (LogFileException e)
						{
							ExceptionManager.ReportCommonErrorToUser(e);
							return;
						}
						if (fileInfo.Exists)
						{
							OpenProject(fileInfo.FullName);
						}
						else
						{
							GetInterfaceProvider().ShowMessageBox(SR.GetString("MsgPrjNotExist"), string.Empty, MessageBoxIcon.Hand, MessageBoxButtons.OK);
						}
					}
					else
					{
						List<string> list = new List<string>();
						string[] array = startupCommandArgs;
						foreach (string text in array)
						{
							if (!string.IsNullOrEmpty(text))
							{
								FileInfo fileInfo2 = null;
								try
								{
									fileInfo2 = Utilities.CreateFileInfoHelper(text);
								}
								catch (LogFileException e2)
								{
									ExceptionManager.ReportCommonErrorToUser(e2);
									continue;
								}
								if (fileInfo2.Exists)
								{
									list.Add(fileInfo2.FullName);
								}
								else
								{
									ExceptionManager.ReportCommonErrorToUser(new TraceViewerException(SR.GetString("MsgFileNotExist") + text));
								}
							}
						}
						if (list.Count != 0)
						{
							string[] array2 = new string[list.Count];
							list.CopyTo(array2);
							AddFiles(array2);
						}
					}
				}
				catch (Exception e3)
				{
					ExceptionManager.GeneralExceptionFilter(e3);
					ExceptionManager.ReportCommonErrorToUser(new TraceViewerException(SR.GetString("MsgUnknownOpenParam")));
				}
			}
		}

		internal static ImageList GetImageFromImageList(Images image, out int index)
		{
			index = GetImageIndexFromImageList(image);
			return currentSharedImageList;
		}

		internal static int GetImageIndexFromImageList(Images image)
		{
			int result = -1;
			switch (image)
			{
			case Images.HostActivityIcon:
				result = 12;
				break;
			case Images.MessageActivityIcon:
				result = 13;
				break;
			case Images.ExecutionActivityIcon:
				result = 14;
				break;
			case Images.ConnectionActivityIcon:
				result = 15;
				break;
			case Images.PlusIcon:
				result = 16;
				break;
			case Images.MinusIcon:
				result = 17;
				break;
			case Images.SL_Backward:
				result = 18;
				break;
			case Images.SL_Forward:
				result = 19;
				break;
			case Images.MessageSentTrace:
				result = 24;
				break;
			case Images.MessageReceiveTrace:
				result = 25;
				break;
			case Images.RootActivity:
				result = 26;
				break;
			case Images.ListenActivity:
				result = 27;
				break;
			case Images.DefaultActivityIcon:
				result = 28;
				break;
			}
			return result;
		}

		internal static ImageList GetSharedImageList()
		{
			return currentSharedImageList;
		}

		private void InitializeAppSettings()
		{
			AppConfigManager.RegisterPersistObject(this);
			List<IPersistStatus> persistStatusObjects = GraphViewProvider.GetPersistStatusObjects();
			if (persistStatusObjects != null && persistStatusObjects.Count != 0)
			{
				foreach (IPersistStatus item in persistStatusObjects)
				{
					AppConfigManager.RegisterPersistObject(item);
				}
			}
			configManager.RestoreUISettings();
		}

		private void InitializeCacheSystem()
		{
			ActivityAnalyzerHelper.Initialize(this);
		}

		private void InitializeFileTimeRangeControl()
		{
			ObjectStateController.RegisterStateSwitchListener(fileTimeRangeStrip.ObjectStateController);
			FilePartialLoadingStrip filePartialLoadingStrip = fileTimeRangeStrip;
			filePartialLoadingStrip.TimeRangeChangedCallback = (FilePartialLoadingStrip.TimeRangeChanged)Delegate.Combine(filePartialLoadingStrip.TimeRangeChangedCallback, new FilePartialLoadingStrip.TimeRangeChanged(fileTimeRangeStrip_OnTimeRangeChanged));
		}

		private void InitializeFilterBar()
		{
			EnableFilterBar(isEnabling: true);
			traceFilterBar.Initialize(this);
			traceFilterBar.UpdateSearchInList(configManager.LoadCustomFilterManager().CurrentFilters);
			traceFilterBar.TraceFilterChangedEvent += traceFilterBar_TraceFilterChangedEvent;
		}

		private void InitializeFindBar()
		{
			EnableFindBar(isEnabling: true);
			findToolbar.Initialize(this, thisProxy);
		}

		private void InitializeInternalComponents()
		{
			InitializeImageList();
			InitializeProjectTree();
			InitializeMessageView();
			InitializeFindBar();
			InitializeFilterBar();
			InitializeFileTimeRangeControl();
			InitializeSwimLanes();
			InitializeAppSettings();
			InitializeTraceDetailedView();
			InitializeMenuItems();
			InitializeCacheSystem();
			InitializeStatusBar();
		}

		private void InitializeInternalImpls()
		{
			toolStripProgressBar.Visible = false;
			InternalShowMessageBoxProxy = (InternalShowMessageBox)Delegate.Combine(InternalShowMessageBoxProxy, new InternalShowMessageBox(InternalShowMessageBoxImpl));
			InternalBeginToolStripProgressBarProxy = (InternalBeginToolStripProgressBar)Delegate.Combine(InternalBeginToolStripProgressBarProxy, new InternalBeginToolStripProgressBar(InternalBeginToolStripProgressBarImpl));
			InternalCompleteToolStripProgressBarProxy = (InternalCompleteToolStripProgressBar)Delegate.Combine(InternalCompleteToolStripProgressBarProxy, new InternalCompleteToolStripProgressBar(InternalCompleteToolStripProgressBarImpl));
			InternalIndicateToolStripProgressBarProxy = (InternalIndicateProgressToolStripProgressBar)Delegate.Combine(InternalIndicateToolStripProgressBarProxy, new InternalIndicateProgressToolStripProgressBar(InternalIndicateToolStripProgressBarImpl));
			InternalStepToolStripProgressBarProxy = (InternalStepProgressToolStripProgressBar)Delegate.Combine(InternalStepToolStripProgressBarProxy, new InternalStepProgressToolStripProgressBar(InternalStepToolStripProgressBarImpl));
		}

		private void InitializeMenuItems()
		{
			fileRecentMenuItem_Select(fileRecentMenuItem, null);
			fileProjectRecentMenuItem_Select(fileProjectRecentMenuItem, null);
		}

		private void InitializeMessageView()
		{
			messageView.Initialize(this);
			MessageViewControl messageViewControl = messageView;
			messageViewControl.MessageTraceItemClick = (EventHandler)Delegate.Combine(messageViewControl.MessageTraceItemClick, new EventHandler(messageView_MessageTraceItemClick));
			messageViewControl = messageView;
			messageViewControl.MessageTraceItemDoubleClick = (EventHandler)Delegate.Combine(messageViewControl.MessageTraceItemDoubleClick, new EventHandler(messageView_MessageTraceItemDoubleClick));
		}

		private void InitializeProjectTree()
		{
			projectTree.Initialize(this);
			ProjectTreeViewControl projectTreeViewControl = projectTree;
			projectTreeViewControl.AddFileMenuItemClick = (EventHandler)Delegate.Combine(projectTreeViewControl.AddFileMenuItemClick, new EventHandler(fileAddMenuItem_Click));
			projectTreeViewControl = projectTree;
			projectTreeViewControl.CloseAllMenuItemClick = (EventHandler)Delegate.Combine(projectTreeViewControl.CloseAllMenuItemClick, new EventHandler(fileRemoveAllFilesMenuItem));
			projectTreeViewControl = projectTree;
			projectTreeViewControl.OpenProjectMenuItemClick = (EventHandler)Delegate.Combine(projectTreeViewControl.OpenProjectMenuItemClick, new EventHandler(fileOpenProjectMenuItem_Click));
			projectTreeViewControl = projectTree;
			projectTreeViewControl.SaveProjectMenuItemClick = (EventHandler)Delegate.Combine(projectTreeViewControl.SaveProjectMenuItemClick, new EventHandler(fileSaveProjectMenuItem_Click));
			projectTreeViewControl = projectTree;
			projectTreeViewControl.SaveProjectAsMenuItemClick = (EventHandler)Delegate.Combine(projectTreeViewControl.SaveProjectAsMenuItemClick, new EventHandler(fileSaveProjectAsMenuItem_Click));
			projectTreeViewControl = projectTree;
			projectTreeViewControl.CloseProjectMenuItemClick = (EventHandler)Delegate.Combine(projectTreeViewControl.CloseProjectMenuItemClick, new EventHandler(fileCloseProjectMenuItem_Click));
			ProjectManager projectManager = this.projectManager;
			projectManager.ProjectChangedCallback = (ProjectManager.ProjectChanged)Delegate.Combine(projectManager.ProjectChangedCallback, new ProjectManager.ProjectChanged(projectTree.OnProjectNameChange));
		}

		private void InitializeStatusBar()
		{
			InternalIndicateToolStripProgressBarImpl(0, 0);
		}

		private void InitializeSwimLanes()
		{
			this.swimLanesControl.Initialize(this, thisProxy, new ExceptionManager());
			SwimLanesControl swimLanesControl = this.swimLanesControl;
			swimLanesControl.ClickTraceRecordItemCallback = (SwimLanesControl.ClickTraceRecordItem)Delegate.Combine(swimLanesControl.ClickTraceRecordItemCallback, new SwimLanesControl.ClickTraceRecordItem(swinLanes_OnClickTraceRecordItem));
		}

		private void InitializeTraceDetailedView()
		{
			traceDetailInfoControl.Initialize(this, thisProxy, new ExceptionManager());
		}

		private void InitializeImageList()
		{
			if (imageList != null)
			{
				try
				{
					imageList.Images.Add("0", TempFileManager.GetImageFromEmbededResources(Images.Call));
					imageList.Images.Add("1", TempFileManager.GetImageFromEmbededResources(Images.Helper));
					imageList.Images.Add("2", TempFileManager.GetImageFromEmbededResources(Images.Operation, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("3", TempFileManager.GetImageFromEmbededResources(Images.Reply, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("4", TempFileManager.GetImageFromEmbededResources(Images.HttpSys));
					imageList.Images.Add("5", TempFileManager.GetImageFromEmbededResources(Images.Unknown));
					imageList.Images.Add("6", TempFileManager.GetImageFromEmbededResources(Images.Message, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("7", TempFileManager.GetImageFromEmbededResources(Images.MessageHandling));
					imageList.Images.Add("8", TempFileManager.GetImageFromEmbededResources(Images.Transfer, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("9", TempFileManager.GetImageFromEmbededResources(Images.CallWithMessage));
					imageList.Images.Add("10", TempFileManager.GetImageFromEmbededResources(Images.TransferIn, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("11", TempFileManager.GetImageFromEmbededResources(Images.TransferOut, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("12", TempFileManager.GetImageFromEmbededResources(Images.HostActivityIcon, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("13", TempFileManager.GetImageFromEmbededResources(Images.MessageActivityIcon, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("14", TempFileManager.GetImageFromEmbededResources(Images.ExecutionActivityIcon, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("15", TempFileManager.GetImageFromEmbededResources(Images.ConnectionActivityIcon, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("16", TempFileManager.GetImageFromEmbededResources(Images.PlusIcon, Color.White, isMakeTransparent: true));
					imageList.Images.Add("17", TempFileManager.GetImageFromEmbededResources(Images.MinusIcon, Color.White, isMakeTransparent: true));
					imageList.Images.Add("18", TempFileManager.GetImageFromEmbededResources(Images.SL_Backward, Color.White, isMakeTransparent: true));
					imageList.Images.Add("19", TempFileManager.GetImageFromEmbededResources(Images.SL_Forward, Color.White, isMakeTransparent: true));
					imageList.Images.Add("20", TempFileManager.GetImageFromEmbededResources(Images.ActivityStartTrace, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("21", TempFileManager.GetImageFromEmbededResources(Images.ActivityStopTrace, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("22", TempFileManager.GetImageFromEmbededResources(Images.ActivityResumeTrace, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("23", TempFileManager.GetImageFromEmbededResources(Images.ActivityPauseTrace, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("24", TempFileManager.GetImageFromEmbededResources(Images.MessageSentTrace, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("25", TempFileManager.GetImageFromEmbededResources(Images.MessageReceiveTrace, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("26", TempFileManager.GetImageFromEmbededResources(Images.RootActivity, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("27", TempFileManager.GetImageFromEmbededResources(Images.ListenActivity, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("28", TempFileManager.GetImageFromEmbededResources(Images.DefaultActivityIcon, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("29", TempFileManager.GetImageFromEmbededResources(Images.ErrorTrace, Color.Black, isMakeTransparent: true));
					imageList.Images.Add("30", TempFileManager.GetImageFromEmbededResources(Images.WarningTrace, Color.Black, isMakeTransparent: true));
					currentSharedImageList = imageList;
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
					currentSharedImageList = null;
				}
			}
		}

		private void CloseProject()
		{
			if (projectManager.CloseProject())
			{
				CloseAllFiles();
			}
		}

		private void OpenProject()
		{
			string[] array = projectManager.OpenProject();
			if (array != null)
			{
				LoadFiles(array);
			}
		}

		private void OpenProject(string path)
		{
			string[] array = projectManager.OpenProject(path);
			if (array != null)
			{
				LoadFiles(array);
			}
		}

		private void SaveProject()
		{
			if (!projectManager.SaveProject())
			{
				thisProxy.ShowMessageBox(SR.GetString("MsgFailToSaveProject"), null, MessageBoxIcon.Hand, MessageBoxButtons.OK);
			}
		}

		private void SaveProjectAs()
		{
			if (!projectManager.SaveProjectAs())
			{
				thisProxy.ShowMessageBox(SR.GetString("MsgFailToSaveProject"), null, MessageBoxIcon.Hand, MessageBoxButtons.OK);
			}
		}

		internal void ClearDataSource()
		{
			if (traceDataSource != null)
			{
				cachedActivities = null;
				traceDataSource.Clear();
				traceDataSource.Dispose();
				traceDataSource = null;
				try
				{
					if (DataSourceChangedHandler != null)
					{
						DataSourceChangedHandler(null);
					}
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
				}
			}
		}

		private void AdjustSettings()
		{
			toolStripProgressBar.Width = 200;
			menuSortByStartTime = new MenuItem();
			menuSortByEndTime = new MenuItem();
			menuSelectAllActivities = new MenuItem();
			menuCreateCustomFilter = new ToolStripMenuItem();
			menuCopyToClipboard = new ToolStripMenuItem();
			menuSortByStartTime.Index = -1;
			menuSortByStartTime.Text = SR.GetString("SortByStartTime");
			menuSortByStartTime.Click += menuSortByStartTime_Click;
			menuSortByEndTime.Index = -1;
			menuSortByEndTime.Text = SR.GetString("SortByEndTime");
			menuSortByEndTime.Click += menuSortByEndTime_Click;
			menuSelectAllActivities.Index = -1;
			menuSelectAllActivities.Text = SR.GetString("SelectAllActivities");
			menuSelectAllActivities.Click += menuSelectAllActivities_Click;
			menuCreateCustomFilter.Text = SR.GetString("CreateCustomFilter");
			menuCreateCustomFilter.Click += menuCreateCustomFilter_Click;
			menuCopyToClipboard.Text = SR.GetString("CopyTraceToClipboard");
			menuCopyToClipboard.Click += menuCopyToClipboard_Click;
			listActivities.ContextMenu = new ContextMenu(new MenuItem[3]
			{
				menuSortByStartTime,
				menuSortByEndTime,
				menuSelectAllActivities
			});
			listTracesDetailMenuStrip = new ContextMenuStrip();
			listTracesDetailMenuStrip.Items.AddRange(new ToolStripItem[2]
			{
				menuCopyToClipboard,
				menuCreateCustomFilter
			});
		}

		private void SelectAllActivitiesInList()
		{
			string currentStateName = ObjectStateController.CurrentStateName;
			if (listActivities.Items.Count != 0 && currentStateName == "IdleProjectState")
			{
				ObjectStateController.SwitchState("FileLoadingProjectState");
				listActivities.BeginUpdate();
				try
				{
					foreach (ListViewItem item in listActivities.Items)
					{
						item.Selected = true;
					}
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
				}
				finally
				{
					listActivities.EndUpdate();
					isSelectAllActivitiesState = true;
					ObjectStateController.SwitchState(currentStateName);
					listActivities_SelectedIndexChanged(true, null);
				}
			}
		}

		private bool UnselectAllActivitiesInListOnCondition()
		{
			string currentStateName = ObjectStateController.CurrentStateName;
			if (currentStateName != "IdleProjectState")
			{
				return false;
			}
			if (isSelectAllActivitiesState)
			{
				ObjectStateController.SwitchState("FileLoadingProjectState");
				listActivities.BeginUpdate();
				try
				{
					foreach (ListViewItem item in listActivities.Items)
					{
						item.Selected = false;
					}
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
				}
				finally
				{
					listActivities.EndUpdate();
					isSelectAllActivitiesState = false;
					ObjectStateController.SwitchState(currentStateName);
					listActivities_SelectedIndexChanged(false, null);
				}
				return false;
			}
			return true;
		}

		internal void SyncFindingOptions(FindingScope scope, object issueObject)
		{
			if (issueObject != findDialog)
			{
				findDialog.UpdateFindOptions(scope);
			}
			if (issueObject != findToolbar)
			{
				findToolbar.UpdateFindOptions(scope);
			}
		}

		private void activityFollowTransferMenuItem_Click(object sender, EventArgs e)
		{
			if (listTracesDetail.SelectedItems != null && listTracesDetail.SelectedItems.Count != 0 && !((TraceRecord)listTracesDetail.SelectedItems[0].Tag).IsTransfer)
			{
				activityStepForwardMenuItem_Click(null, null);
			}
			else
			{
				listTracesDetail_DoubleClick(null, null);
			}
		}

		private void activityStepForwardMenuItem_Click(object sender, EventArgs e)
		{
			if (CurrentTraceListCount != 0)
			{
				if (listTracesDetail.SelectedIndices.Count == 0)
				{
					SetListViewItemFocus(listTracesDetail.Items[0]);
				}
				else
				{
					int num = listTracesDetail.SelectedIndices[0];
					if (++num < CurrentTraceListCount)
					{
						SetListViewItemFocus(listTracesDetail.Items[num]);
					}
				}
			}
		}

		private void AddFiles(string[] fileNames)
		{
			if (fileNames != null && fileNames.Length != 0)
			{
				List<string> list = new List<string>();
				for (int i = 0; i < fileNames.Length; i++)
				{
					string text = fileNames[i].Trim().ToLower(CultureInfo.CurrentCulture);
					string text2 = null;
					SupportedFileFormat supportedFileFormat = SupportedFileFormat.NotSupported;
					try
					{
						supportedFileFormat = FileConverter.DetectFileSchema(text);
					}
					catch (LogFileException e)
					{
						ExceptionManager.ReportCommonErrorToUser(e);
						projectManager.OpenedFilePaths.Remove(text);
						continue;
					}
					switch (supportedFileFormat)
					{
					case SupportedFileFormat.EtlBinary:
					case SupportedFileFormat.CrimsonSchema:
						using (SaveFileDialog saveFileDialog = new SaveFileDialog())
						{
							saveFileDialog.Filter = SR.GetString("MainFrm_FileSaveDlg1");
							saveFileDialog.FilterIndex = 0;
							saveFileDialog.OverwritePrompt = true;
							saveFileDialog.FileName = text + SR.GetString("MainFrm_FileE2e");
							text2 = ((thisProxy.ShowDialog(saveFileDialog, this) != DialogResult.OK) ? null : saveFileDialog.FileNames[0].ToLowerInvariant());
						}
						if (!string.IsNullOrEmpty(text2))
						{
							FileConverter fileConverter = new FileConverter(new ExceptionManager(), thisProxy);
							try
							{
								fileConverter.ConvertFileToE2ESchema(text, text2, supportedFileFormat);
							}
							catch (FileConverterException e2)
							{
								ExceptionManager.ReportCommonErrorToUser(e2);
								continue;
							}
							if (!list.Contains(text2))
							{
								list.Add(text2);
							}
						}
						break;
					case SupportedFileFormat.E2ETraceEventSchema:
						if (!list.Contains(text))
						{
							list.Add(text);
						}
						break;
					case SupportedFileFormat.NotSupported:
						thisProxy.ShowMessageBox(SR.GetString("MsgUnsupportedSchema") + SR.GetString("MainFrm_ReturnSingle") + text, string.Empty, MessageBoxIcon.Hand, MessageBoxButtons.OK);
						break;
					case SupportedFileFormat.UnknownFormat:
						thisProxy.ShowMessageBox(SR.GetString("MsgUnknownFileFormat") + SR.GetString("MainFrm_ReturnSingle") + text, string.Empty, MessageBoxIcon.Hand, MessageBoxButtons.OK);
						break;
					}
				}
				InternalCompleteToolStripProgressBarImpl();
				if (list.Count > 0)
				{
					SwitchFilterMessageHelper();
					fileNames = list.ToArray();
					PersistedSettings.SaveRecentFiles(fileNames, isProject: false);
					DataSource.AppendFiles(fileNames);
				}
			}
			if (!string.IsNullOrEmpty(projectManager.CurrentProjectFilePath) && (DataSource.LoadedFileNames == null || DataSource.LoadedFileNames.Count == 0))
			{
				ObjectStateController.SwitchState("NoFileProjectState");
			}
		}

		private void CloseAllFiles()
		{
			ObjectStateController.SwitchState("FileRemovingProjectState");
			ClearDataSource();
			ClearAll();
			ResetWindowTitle();
			InternalIndicateToolStripProgressBarImpl(0, 0);
			if (string.IsNullOrEmpty(projectManager.CurrentProjectFilePath))
			{
				ObjectStateController.SwitchState("EmptyProjectState");
			}
			else
			{
				ObjectStateController.SwitchState("NoFileProjectState");
			}
		}

		private bool FindFirstTransfer(out string toActivityId, out string fromActivityId, out long traceID, out DateTime timestamp, bool direction)
		{
			toActivityId = null;
			fromActivityId = null;
			traceID = 0L;
			timestamp = DateTime.MinValue;
			bool result = false;
			for (int i = (listTracesDetail.SelectedIndices.Count > 0) ? listTracesDetail.SelectedIndices[0] : 0; direction ? (i < CurrentTraceListCount) : (i >= 0 && CurrentTraceListCount > 0); i += (direction ? 1 : (-1)))
			{
				TraceRecord traceRecord = (TraceRecord)listTracesDetail.Items[i].Tag;
				if (traceRecord.IsTransfer)
				{
					bool flag = false;
					foreach (ListViewItem selectedItem in listActivities.SelectedItems)
					{
						if (direction ? (((Activity)selectedItem.Tag).Id == traceRecord.RelatedActivityID) : (((Activity)selectedItem.Tag).Id == traceRecord.ActivityID))
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						toActivityId = (direction ? traceRecord.RelatedActivityID : traceRecord.ActivityID);
						fromActivityId = (direction ? traceRecord.ActivityID : traceRecord.RelatedActivityID);
						timestamp = traceRecord.Time;
						result = true;
						traceID = traceRecord.TraceID;
						break;
					}
				}
			}
			return result;
		}

		private void FollowTransfer(bool direction)
		{
            string toActivityId, _1;
            long traceID;
            DateTime _2;
			if (FindFirstTransfer(out toActivityId, out _1, out traceID, out _2, direction))
			{
				listActivities.SelectedItems.Clear();
				foreach (ListViewItem item in listActivities.Items)
				{
					if (((Activity)item.Tag).Id == toActivityId)
					{
						intendSelectedTransferTrace = new InternalTraceRecordSelector();
						intendSelectedTransferTrace.TraceID = traceID;
						intendSelectedTransferTrace.NextFocus = listTracesDetail;
						SetListViewItemFocus(item);
						break;
					}
				}
			}
		}

		private int GetPreviousOrNextTransferInTraceList(bool nextTransfer)
		{
			if (CurrentTraceListCount != 0 && listTracesDetail.SelectedIndices.Count != 0)
			{
				int num = listTracesDetail.SelectedIndices[0] + (nextTransfer ? 1 : (-1));
				while (num < CurrentTraceListCount && num >= 0)
				{
					if (((TraceRecord)listTracesDetail.Items[num].Tag).IsTransfer)
					{
						return num;
					}
					num = (nextTransfer ? (num + 1) : (num - 1));
				}
			}
			return -1;
		}

		private void LoadFiles(string[] fileNames)
		{
			ClearDataSource();
			ClearAll();
			InternalIndicateToolStripProgressBarImpl(0, 0);
			AddFiles(fileNames);
		}

		private void menuCopyToClipboard_Click(object sender, EventArgs e)
		{
			CopyCurrentTraceToClipboard();
		}

		private void menuSelectAllActivities_Click(object sender, EventArgs e)
		{
			SelectAllActivitiesInList();
		}

		private void ResetWindowTitle()
		{
			string text = defaultWindowTitle;
			if (!string.IsNullOrEmpty(projectManager.CurrentProjectFilePath))
			{
				text += SR.GetString("TxtPrjSeperator");
				text += projectManager.CurrentProjectFilePath;
			}
			else
			{
				List<string> loadedFileNames = DataSource.LoadedFileNames;
				if (loadedFileNames.Count != 0)
				{
					text += SR.GetString("TxtPrjSeperator");
					string text2 = string.Empty;
					foreach (string item in loadedFileNames)
					{
						text2 = text2 + item + SR.GetString("MainFrm_FileNameSep");
					}
					string str = text;
					object str2;
					if (!text2.EndsWith(SR.GetString("MainFrm_FileNameSep"), StringComparison.OrdinalIgnoreCase))
					{
						str2 = text2;
					}
					else
					{
						string text3 = text2;
						str2 = text3.Remove(text3.Length - 1, 1);
					}
					text = str + (string)str2;
				}
			}
			Text = text;
		}

		private void SwitchFilterMessageHelper()
		{
			if (traceFilterBar.IsFilterEnabled())
			{
				string @string = SR.GetString("MainFrm_FilterEnabled");
				@string += SR.GetString("MainFrm_FilterDisable");
				if (thisProxy.ShowMessageBox(@string, null, MessageBoxIcon.Question, MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					traceFilterBar.ClearFilter();
				}
			}
		}

		private void UpdateActivities()
		{
			foreach (ListViewItem item in listActivities.Items)
			{
				Activity activity = (Activity)item.Tag;
				item.ImageIndex = Utilities.GetImageIndex(activity);
				item.SubItems[1].Text = activity.TraceCount.ToString(CultureInfo.CurrentCulture);
				item.SubItems[2].Text = GetActivityDurationString(activity.StartTime, activity.EndTime);
				item.SubItems[3].Text = activity.StartTime.ToLongTimeString();
				item.SubItems[4].Text = activity.EndTime.ToLongTimeString();
				if (!string.IsNullOrEmpty(activity.Name))
				{
					item.SubItems[0].Text = activity.Name;
					if (activityIDToNameDictionary.ContainsKey(activity.Id))
					{
						activityIDToNameDictionary[activity.Id] = activity.Name;
					}
					else
					{
						activityIDToNameDictionary.Add(activity.Id, activity.Name);
					}
					if (activity.IsMultipleName)
					{
						string text = SR.GetString("MainFrm_MultiNamedActivityTip");
						foreach (string name in activity.NameList)
						{
							text = text + SR.GetString("MainFrm_TabSingle") + name + SR.GetString("MainFrm_ReturnSingle");
						}
						item.ToolTipText = text;
					}
					else
					{
						item.ToolTipText = (string.IsNullOrEmpty(activity.Name) ? activity.Id : activity.Name);
					}
				}
				if (activity.HasError)
				{
					item.ForeColor = Color.Red;
					ListViewItem listViewItem2 = item;
					listViewItem2.Font = new Font(listViewItem2.Font, item.Font.Style | FontStyle.Bold);
				}
				else if (activity.HasWarning)
				{
					item.BackColor = Color.Yellow;
					ListViewItem listViewItem3 = item;
					listViewItem3.Font = new Font(listViewItem3.Font, item.Font.Style | FontStyle.Bold);
				}
			}
			SortActivitiesByColumn( true, 3);
		}

		private bool ReloadTrace(TraceRecord trace)
		{
			if (trace != null)
			{
				try
				{
					bool flag = tabs.SelectedTab == tabMessage;
					tabs.TabPages.Remove(tabMessage);
					string xml = trace.Xml;
					if (!string.IsNullOrEmpty(xml))
					{
						xmlViewRenderer.SetXmlText(xml);
						if (!tabs.TabPages.Contains(tabStylesheet))
						{
							tabs.TabPages.Insert(0, tabStylesheet);
						}
						try
						{
							traceDetailInfoControl.ReloadTraceDetailedInfo(trace);
						}
						catch (TraceViewerException e)
						{
							ExceptionManager.ReportCommonErrorToUser(e);
							tabs.TabPages.Remove(tabStylesheet);
						}
						if (trace.IsMessageLogged)
						{
							bool xml2 = false;
							string loggedMessageString = trace.GetLoggedMessageString(out xml2);
							if (string.IsNullOrEmpty(loggedMessageString))
							{
								loggedMessageString = SR.GetString("TxtCannotLoadMessage");
							}
							else
							{
								loggedMessageString = SR.GetString("XML_MessageLogTraceRecordStart") + loggedMessageString + SR.GetString("XML_MessageLogTraceRecordStop");
								messageViewRenderer.SetXmlText(loggedMessageString);
							}
							tabs.TabPages.Insert(2, tabMessage);
							if (flag)
							{
								tabs.SelectedTab = tabMessage;
							}
						}
						else
						{
							messageViewRenderer.SetXmlText(string.Empty);
						}
						return true;
					}
					string text = trace.TryAndGetXmlString();
					string text2 = SR.GetString("MainFrm_ErrTraceRecord") + trace.TraceRecordPos.FileOffset.ToString(CultureInfo.CurrentCulture);
					if (!string.IsNullOrEmpty(text))
					{
						text2 = text2 + SR.GetString("MainFrm_ErrTraceRecord2") + text;
					}
					xmlViewRenderer.SetXmlText(text2);
					traceDetailInfoControl.CleanUp();
					messageViewRenderer.SetXmlText(text2);
					tabs.TabPages.Remove(tabStylesheet);
					ExceptionManager.ReportCommonErrorToUser(new TraceViewerException(SR.GetString("MsgErrorTraceLoading")));
					tabs.SelectedTab = tabBrowser;
				}
				catch (Exception e2)
				{
					ExceptionManager.GeneralExceptionFilter(e2);
					ExceptionManager.ReportCommonErrorToUser(new TraceViewerException(SR.GetString("MsgRefreshTraceSource")));
				}
			}
			return false;
		}

		private void HighlightTraceRecord()
		{
			try
			{
				if (intendSelectedTransferTrace != null)
				{
					foreach (ListViewItem item in listTracesDetail.Items)
					{
						TraceRecord traceRecord = (TraceRecord)item.Tag;
						if (traceRecord.IsTransfer && traceRecord.TraceID == intendSelectedTransferTrace.TraceID)
						{
							SetListViewItemFocus(item);
							listTracesDetail.Select();
							intendSelectedTransferTrace = null;
							return;
						}
					}
				}
				if (intendSelectedTrace != null)
				{
					foreach (ListViewItem item2 in listTracesDetail.Items)
					{
						if (((TraceRecord)item2.Tag).TraceID == intendSelectedTrace.TraceID)
						{
							SetListViewItemFocus(item2);
							if (intendSelectedTrace.IsHighlight)
							{
								listTracesDetail.Select();
							}
							if (intendSelectedTrace.NextFocus != null && !intendSelectedTrace.NextFocus.Focused)
							{
								intendSelectedTrace.NextFocus.Select();
							}
							intendSelectedTrace = null;
							break;
						}
					}
				}
				else if (CurrentTraceListCount != 0)
				{
					listTracesDetail.Items[0].Selected = true;
				}
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
		}

		private Dictionary<TraceRecord, string> GetActivityNameDictionaryForTraces(List<ListViewItem> traceItems)
		{
			Dictionary<TraceRecord, string> dictionary = new Dictionary<TraceRecord, string>();
			if (traceItems != null && traceItems.Count != 0)
			{
				Dictionary<int, List<TraceRecord>> dictionary2 = new Dictionary<int, List<TraceRecord>>();
				List<TraceRecord> list = null;
				string value = string.Empty;
				int num = 0;
				string a = string.Empty;
				foreach (ListViewItem traceItem in traceItems)
				{
					TraceRecord traceRecord = (TraceRecord)traceItem.Tag;
					if (!dictionary2.ContainsKey(traceRecord.Execution.ExecutionID))
					{
						dictionary2[traceRecord.Execution.ExecutionID] = new List<TraceRecord>();
					}
					dictionary2[traceRecord.Execution.ExecutionID].Add(traceRecord);
				}
				{
					foreach (List<TraceRecord> value2 in dictionary2.Values)
					{
						foreach (TraceRecord item in value2)
						{
							if (item.IsActivityBoundary && item.Level == TraceEventType.Start)
							{
								if (list != null && list.Count != 0)
								{
									foreach (TraceRecord item2 in list)
									{
										if (!dictionary.ContainsKey(item2))
										{
											dictionary.Add(item2, value);
										}
									}
									list = null;
								}
								list = new List<TraceRecord>();
								list.Add(item);
								num = item.Execution.ExecutionID;
								a = item.ActivityID;
								value = item.ActivityName;
							}
							else if (item.IsActivityBoundary && item.Level == TraceEventType.Stop)
							{
								if (list != null && num == item.Execution.ExecutionID && a == item.ActivityID)
								{
									list.Add(item);
									foreach (TraceRecord item3 in list)
									{
										if (!dictionary.ContainsKey(item3))
										{
											dictionary.Add(item3, value);
										}
									}
									list = null;
								}
							}
							else if (list != null)
							{
								if (!item.IsTransfer)
								{
									if (num != item.Execution.ExecutionID || a != item.ActivityID)
									{
										list.Add(item);
										foreach (TraceRecord item4 in list)
										{
											if (!dictionary.ContainsKey(item4))
											{
												dictionary.Add(item4, value);
											}
										}
										list = null;
										num = item.Execution.ExecutionID;
									}
									else
									{
										list.Add(item);
									}
								}
								else if (num != item.Execution.ExecutionID)
								{
									foreach (TraceRecord item5 in list)
									{
										if (!dictionary.ContainsKey(item5))
										{
											dictionary.Add(item5, value);
										}
									}
									num = item.Execution.ExecutionID;
								}
								else
								{
									list.Add(item);
								}
							}
							else if (!dictionary.ContainsKey(item))
							{
								dictionary.Add(item, null);
							}
						}
						if (list != null && list.Count != 0)
						{
							foreach (TraceRecord item6 in list)
							{
								if (!dictionary.ContainsKey(item6))
								{
									dictionary[item6] = value;
								}
							}
						}
					}
					return dictionary;
				}
			}
			return dictionary;
		}

		private void ReGroupTraceList()
		{
			string currentStateName = ObjectStateController.CurrentStateName;
			ObjectStateController.SwitchState("FileReloadingProjectState");
			string currentStateName2 = traceGroupByStateController.CurrentStateName;
			if (!(currentStateName2 == "GroupByNoneState"))
			{
				if (currentStateName2 == "GroupBySourceFileState")
				{
					ReGroupTraceListBySourceFile();
					groupByStripMenuItem.Text = SR.GetString("MainFrm_GroupBySource");
				}
			}
			else
			{
				ReGroupTraceListByNone();
				groupByStripMenuItem.Text = SR.GetString("MainFrm_GroupByNone");
				UpdateTraceRecords(onlyUpdateBackColor: false);
			}
			ObjectStateController.SwitchState(currentStateName);
			listTracesDetail.Select();
		}

		private void UpdateTraceItemsBackColorByTimeRange(ListView.ListViewItemCollection itemCollection)
		{
			if (itemCollection != null && itemCollection.Count != 0)
			{
				DateTime d = DateTime.MinValue;
				int num = -1;
				for (int i = 0; i < CurrentTraceListCount; i++)
				{
					ListViewItem listViewItem = listTracesDetail.Items[i];
					TraceRecord traceRecord = (TraceRecord)listViewItem.Tag;
					if (traceRecord.Level != TraceEventType.Warning)
					{
						if (d != traceRecord.Time)
						{
							num = ((num + 1 != alternativeTraceItemBackColor.Length) ? (num + 1) : 0);
							d = traceRecord.Time;
						}
						if (num == -1)
						{
							num = 0;
						}
						listViewItem.BackColor = alternativeTraceItemBackColor[num];
					}
				}
			}
		}

		private void UpdateTraceRecords(bool onlyUpdateBackColor)
		{
			if (CurrentTraceListCount != 0)
			{
				List<ListViewItem> list = null;
				Dictionary<TraceRecord, string> dictionary = null;
				if (!onlyUpdateBackColor)
				{
					list = new List<ListViewItem>();
					foreach (ListViewItem item in listTracesDetail.Items)
					{
						list.Add(item);
					}
					dictionary = GetActivityNameDictionaryForTraces(list);
					foreach (ListViewItem item2 in listTracesDetail.Items)
					{
						TraceRecord key = (TraceRecord)item2.Tag;
						if (dictionary.ContainsKey(key) && !string.IsNullOrEmpty(dictionary[key]))
						{
							item2.SubItems[traceListActivityNameColumn.Index].Text = dictionary[key];
						}
						else
						{
							item2.SubItems[traceListActivityNameColumn.Index].Text = string.Empty;
						}
					}
				}
				if (listTracesDetail.Groups.Count == 0)
				{
					UpdateTraceItemsBackColorByTimeRange(listTracesDetail.Items);
				}
			}
		}

		private void ReGroupTraceListByNone()
		{
			if (currentWholeTraceItems != null)
			{
				listTracesDetail.BeginUpdate();
				try
				{
					listTracesDetail.Items.Clear();
					listTracesDetail.Groups.Clear();
					listTracesDetail.Items.AddRange(currentWholeTraceItems.ToArray());
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
				}
				finally
				{
					listTracesDetail.EndUpdate();
				}
			}
		}

		private void ReGroupTraceListBySourceFile()
		{
			if (currentWholeTraceItems != null && DataSource != null && DataSource.LoadedFileNames.Count != 0)
			{
				listTracesDetail.BeginUpdate();
				try
				{
					listTracesDetail.Items.Clear();
					listTracesDetail.Groups.Clear();
					Dictionary<string, ListViewGroup> dictionary = new Dictionary<string, ListViewGroup>();
					foreach (ListViewItem currentWholeTraceItem in currentWholeTraceItems)
					{
						currentWholeTraceItem.Group = null;
						TraceRecord traceRecord = (TraceRecord)currentWholeTraceItem.Tag;
						if (!dictionary.ContainsKey(traceRecord.FileDescriptor.FilePath))
						{
							dictionary.Add(traceRecord.FileDescriptor.FilePath, new ListViewGroup(traceRecord.FileDescriptor.FilePath));
							listTracesDetail.Groups.Add(dictionary[traceRecord.FileDescriptor.FilePath]);
						}
						currentWholeTraceItem.Group = dictionary[traceRecord.FileDescriptor.FilePath];
						currentWholeTraceItem.BackColor = Color.Transparent;
					}
					listTracesDetail.Items.AddRange(currentWholeTraceItems.ToArray());
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
				}
				finally
				{
					listTracesDetail.EndUpdate();
				}
			}
		}

		private string GetActivityDurationString(DateTime start, DateTime end)
		{
			string text = string.Empty;
			bool flag = true;
			if (!(end > start))
			{
				text = ((!(end == start)) ? SR.GetString("MainFrm_DurationWrong") : SR.GetString("MainFrm_DurationZero"));
			}
			else
			{
				TimeSpan timeSpan = end - start;
				if (timeSpan.Days != 0)
				{
					text = text + timeSpan.Days.ToString(CultureInfo.CurrentUICulture) + SR.GetString("MainFrm_DurationDay");
					flag = false;
				}
				if (timeSpan.Hours != 0)
				{
					text = text + (flag ? string.Empty : " ") + timeSpan.Hours.ToString(CultureInfo.CurrentUICulture) + SR.GetString("MainFrm_DurationHour");
					if (flag)
					{
						flag = false;
					}
				}
				if (timeSpan.Minutes != 0)
				{
					text = text + (flag ? string.Empty : " ") + timeSpan.Minutes.ToString(CultureInfo.CurrentUICulture) + SR.GetString("MainFrm_DurationMinute");
					if (flag)
					{
						flag = false;
					}
				}
				if (timeSpan.Seconds != 0)
				{
					text = text + (flag ? string.Empty : " ") + timeSpan.Seconds.ToString(CultureInfo.CurrentUICulture) + SR.GetString("MainFrm_DurationSecond");
				}
				if (string.IsNullOrEmpty(text))
				{
					text = ((timeSpan.Milliseconds != 0) ? (timeSpan.Milliseconds.ToString(CultureInfo.CurrentUICulture) + SR.GetString("MainFrm_DurationMillSecond")) : SR.GetString("MainFrm_DurationZero"));
				}
			}
			return text;
		}

		private void RefreshActivityList(Dictionary<string, Activity> activities, bool isClear)
		{
			if (activities != null)
			{
				listActivities.BeginUpdate();
				try
				{
					if (isClear)
					{
						listActivities.Items.Clear();
					}
					foreach (Activity value in activities.Values)
					{
						int imageIndex = Utilities.GetImageIndex(value);
						string text;
						if (value.Name == null)
						{
							text = ((!activityIDToNameDictionary.ContainsKey(value.Id)) ? Activity.ShortID(value.Id) : activityIDToNameDictionary[value.Id]);
						}
						else
						{
							text = value.Name;
							if (activityIDToNameDictionary.ContainsKey(value.Id))
							{
								activityIDToNameDictionary[value.Id] = text;
							}
							else
							{
								activityIDToNameDictionary.Add(value.Id, text);
							}
						}
						ListViewItem listViewItem = null;
						listViewItem = ((5 == imageIndex) ? new ListViewItem(new string[5]
						{
							text,
							value.TraceCount.ToString(CultureInfo.CurrentCulture),
							GetActivityDurationString(value.StartTime, value.EndTime),
							value.StartTime.ToLongTimeString(),
							value.EndTime.ToLongTimeString()
						}) : new ListViewItem(new string[5]
						{
							text,
							value.TraceCount.ToString(CultureInfo.CurrentCulture),
							GetActivityDurationString(value.StartTime, value.EndTime),
							value.StartTime.ToLongTimeString(),
							value.EndTime.ToLongTimeString()
						}, imageIndex));
						listViewItem.Tag = value;
						if (value.IsMultipleName)
						{
							string text2 = SR.GetString("MainFrm_MultiNamedActivityTip");
							foreach (string name in value.NameList)
							{
								text2 = text2 + SR.GetString("MainFrm_TabSingle") + name + SR.GetString("MainFrm_ReturnSingle");
							}
							listViewItem.ToolTipText = text2;
						}
						else
						{
							listViewItem.ToolTipText = (string.IsNullOrEmpty(value.Name) ? value.Id : value.Name);
						}
						if (value.HasError)
						{
							listViewItem.ForeColor = Color.Red;
							ListViewItem listViewItem2 = listViewItem;
							listViewItem2.Font = new Font(listViewItem2.Font, listViewItem.Font.Style | FontStyle.Bold);
						}
						else if (value.HasWarning)
						{
							listViewItem.BackColor = Color.Yellow;
							ListViewItem listViewItem3 = listViewItem;
							listViewItem3.Font = new Font(listViewItem3.Font, listViewItem.Font.Style | FontStyle.Bold);
						}
						listActivities.Items.Add(listViewItem);
					}
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
				}
				finally
				{
					listActivities.EndUpdate();
					listActivities.Invalidate();
					listTracesDetail.Invalidate();
				}
			}
		}

		private void ClearAll()
		{
			ClearActivityList();
			ClearTraceList();
			ClearTraceDetail();
			ClearMessageView();
			ResetWindowTitle();
			UpdateCurrentTraceForLoadedActivityList();
		}

		private void ClearMessageView()
		{
			messageView.Clear();
		}

		private void ClearTraceDetail()
		{
			try
			{
				traceDetailInfoControl.CleanUp();
				xmlViewRenderer.ClearXmlText();
				messageViewRenderer.ClearXmlText();
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
		}

		private void ClearActivityList()
		{
			listActivities.SuspendLayout();
			try
			{
				listActivities.Items.Clear();
				listActivities.ListViewItemSorter = null;
				cachedActivities = null;
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
			finally
			{
				listActivities.ResumeLayout();
			}
		}

		private void ClearTraceList()
		{
			listTracesDetail.SuspendLayout();
			try
			{
				listTracesDetail.Groups.Clear();
				listTracesDetail.Items.Clear();
				currentWholeTraceItems = null;
				listTracesDetail.ListViewItemSorter = null;
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
			finally
			{
				listTracesDetail.ResumeLayout();
			}
		}

		private void activityViewMenuItem_Click(object sender, EventArgs e)
		{
			LeftPanelStateController.SwitchState("LeftPanelActivityViewState");
		}

		private void CheckCreateCustomFilterByTemplateStripButtonStatus()
		{
			if (IsTraceSelected)
			{
				traceListCreateCustomFilterByTemplateStripButton.Enabled = true;
			}
			else
			{
				traceListCreateCustomFilterByTemplateStripButton.Enabled = false;
			}
		}

		private void CreateCustomFilterOnCurrentSelectedTrace()
		{
			if (IsTraceSelected)
			{
				string xml = CurrentSelectedTraceRecord.Xml;
				if (!string.IsNullOrEmpty(xml) && ValidateXmlTemplate(xml))
				{
					using (CustomFilterDialog customFilterDialog = new CustomFilterDialog(new ExceptionManager()))
					{
						customFilterDialog.Initialize(configManager.LoadCustomFilterManager(), thisProxy, this);
						if (customFilterDialog.CreateFilterByTemplate(xml) == DialogResult.OK)
						{
							traceFilterBar.UpdateSearchInList(customFilterDialog.ResultFilters);
						}
					}
				}
			}
		}

		private void customFiltersMenuItem_Click(object sender, EventArgs e)
		{
			using (CustomFilterDialog customFilterDialog = new CustomFilterDialog(new ExceptionManager()))
			{
				customFilterDialog.Initialize(configManager.LoadCustomFilterManager(), thisProxy, this);
				if (thisProxy.ShowDialog(customFilterDialog, this) == DialogResult.OK)
				{
					traceFilterBar.UpdateSearchInList(customFilterDialog.ResultFilters);
				}
			}
		}

		private void fileCloseAllMenuItem_Click(object sender, EventArgs e)
		{
			CloseProject();
		}

		private void fileCloseProjectMenuItem_Click(object sender, EventArgs e)
		{
			CloseProject();
		}

		private void fileExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void fileOpenProjectMenuItem_Click(object sender, EventArgs e)
		{
			OpenProject();
		}

		private void fileRemoveAllFilesMenuItem(object sender, EventArgs e)
		{
			CloseAllFiles();
		}

		private void fileSaveProjectAsMenuItem_Click(object sender, EventArgs e)
		{
			SaveProjectAs();
		}

		private void fileSaveProjectMenuItem_Click(object sender, EventArgs e)
		{
			SaveProject();
		}

		private void filterNowMenuItem_Click(object sender, EventArgs e)
		{
			traceFilterBar.FilterNow();
		}

		private void filterOptionsMenuItem_Click(object sender, EventArgs e)
		{
			FilterOptionsDialog filterOptionsDialog = new FilterOptionsDialog();
			filterOptionsDialog.Initialize(configManager, (!(ObjectStateController.CurrentStateName == "NoFileProjectState") && !(ObjectStateController.CurrentStateName == "EmptyProjectState")) || (DataSource != null && DataSource.Activities.Count != 0));
			if (thisProxy.ShowDialog(filterOptionsDialog, this) == DialogResult.Yes)
			{
				DataSource.ReloadFiles();
			}
		}

		private void helpAboutMenuItem_Click(object sender, EventArgs e)
		{
			AboutBox dlg = new AboutBox();
			thisProxy.ShowDialog(dlg, this);
		}

		private void helpHelpMenuItem_Click(object sender, EventArgs e)
		{
			string directoryName = Path.GetDirectoryName(GetType().Assembly.Location);
			string twoLetterISOLanguageName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
			string text = Path.Combine(directoryName, twoLetterISOLanguageName, "SvcTraceViewer.chm");
			if (File.Exists(text))
			{
				Help.ShowHelp(this, text);
			}
			else
			{
				Help.ShowHelp(this, "SvcTraceViewer.chm");
			}
		}

		private void InternalAnalysisActivityStub()
		{
			if (ObjectStateController.CurrentStateName == "IdleProjectState")
			{
				leftPanelTab.SelectedIndex = 3;
			}
		}

		private void leftPanelTab_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch (leftPanelTab.SelectedIndex)
			{
			case 0:
				LeftPanelStateController.SwitchState("LeftPanelActivityViewState");
				break;
			case 1:
				LeftPanelStateController.SwitchState("LeftPanelProjectViewState");
				break;
			case 2:
				LeftPanelStateController.SwitchState("LeftPanelMessageViewState");
				break;
			case 3:
				if (LeftPanelStateController.CurrentStateName == "LeftPanelActivityViewState")
				{
					if (listActivities.SelectedItems.Count != 0 && ObjectStateController.CurrentStateName == "IdleProjectState")
					{
						AnaylsisActivityInTraceMode((Activity)listActivities.SelectedItems[0].Tag);
					}
					else
					{
						AnaylsisActivityInTraceMode(null);
					}
				}
				else if (LeftPanelStateController.CurrentStateName == "LeftPanelMessageViewState")
				{
					if (ObjectStateController.CurrentStateName == "IdleProjectState" && messageView.CurrentSelectedTraceRecord != null && DataSource.Activities[messageView.CurrentSelectedTraceRecord.ActivityID] != null)
					{
						AnaylsisActivityInTraceMode(DataSource.Activities[messageView.CurrentSelectedTraceRecord.ActivityID], messageView.CurrentSelectedTraceRecord);
					}
					else
					{
						AnaylsisActivityInTraceMode(null);
					}
				}
				LeftPanelStateController.SwitchState("LeftPanelTreeViewState");
				break;
			}
			SetFindDialogState();
		}

		private void listActivities_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			bool flag = false;
			flag = (listActivities.Columns[e.Column].Tag != null && (bool)listActivities.Columns[e.Column].Tag);
			SortActivitiesByColumn(flag, e.Column);
		}

		private void listActivities_DoubleClick(object sender, EventArgs e)
		{
			InternalAnalysisActivityStub();
		}

		private void listActivities_SelectedIndexChanged(object sender, EventArgs e)
		{
			lock (ThisLock)
			{
				if ((sender != null && sender is bool) || UnselectAllActivitiesInListOnCondition())
				{
					SelectedTraceRecordChangedCallback(null, null);
					if (listActivities.SelectedItems.Count == 0)
					{
						currentWholeTraceItems = null;
						ClearTraceDetail();
						DataSource.LoadTracesFromActivities(null);
					}
					else
					{
						List<Activity> list = new List<Activity>();
						foreach (ListViewItem selectedItem in listActivities.SelectedItems)
						{
							list.Add((Activity)selectedItem.Tag);
						}
						DataSource.LoadTracesFromActivities(list);
					}
				}
			}
		}

		private void listTracesDetail_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			bool isAsc = true;
			if (listTracesDetail.Columns[e.Column].Tag == null)
			{
				listTracesDetail.Columns[e.Column].Tag = false;
			}
			else
			{
				isAsc = (((bool)listTracesDetail.Columns[e.Column].Tag) ? true : false);
				listTracesDetail.Columns[e.Column].Tag = !(bool)listTracesDetail.Columns[e.Column].Tag;
			}
			listTracesDetail.ListViewItemSorter = ((e.Column == 4) ? new ListViewItemTagComparer(isAsc, ListViewItemTagComparerTarget.TraceTime) : new ListViewItemStringComparer(e.Column, isAsc));
			listTracesDetail.Sort();
			UpdateTraceRecords(onlyUpdateBackColor: true);
			listTracesDetail.Update();
		}

		private void listTracesDetail_ColumnClick_TraceLoadingState(object sender, ColumnClickEventArgs e)
		{
			if (thisProxy.ShowMessageBox(SR.GetString("MsgSortingTraceWarning"), null, MessageBoxIcon.Question, MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				listTracesDetail_ColumnClick(sender, e);
			}
		}

		private void listTracesDetail_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (IsTraceSelected && listActivities.SelectedItems.Count != 0)
			{
				TraceRecord currentSelectedTraceRecord = CurrentSelectedTraceRecord;
				if (currentSelectedTraceRecord != null)
				{
					if (ReloadTrace(currentSelectedTraceRecord))
					{
						CheckCreateCustomFilterByTemplateStripButtonStatus();
						UpdateCurrentTraceForLoadedActivityList();
					}
					SelectedTraceRecordChangedCallback(currentSelectedTraceRecord, ((Activity)listActivities.SelectedItems[0].Tag).Id);
				}
			}
			else
			{
				CheckCreateCustomFilterByTemplateStripButtonStatus();
				UpdateCurrentTraceForLoadedActivityList();
				if (SelectedTraceRecordChangedCallback != null)
				{
					SelectedTraceRecordChangedCallback(null, null);
				}
			}
		}

		private void menuCreateCustomFilter_Click(object sender, EventArgs e)
		{
			CreateCustomFilterOnCurrentSelectedTrace();
		}

		private void menuSortByEndTime_Click(object sender, EventArgs e)
		{
			listActivities_ColumnClick(sender, new ColumnClickEventArgs(4));
		}

		private void menuSortByStartTime_Click(object sender, EventArgs e)
		{
			listActivities_ColumnClick(sender, new ColumnClickEventArgs(3));
		}

		private void messageView_MessageTraceItemClick(object o, EventArgs e)
		{
			if (o == null && IsTraceSelected)
			{
				CurrentSelectedTraceListItem.Focused = false;
				CurrentSelectedTraceListItem.Selected = false;
				ClearTraceDetail();
			}
			else if (o != null && o is TraceRecord)
			{
				TraceRecord traceRecord = (TraceRecord)o;
				if (listActivities.SelectedItems.Count != 0 && ((Activity)listActivities.SelectedItems[0].Tag).Id == traceRecord.ActivityID)
				{
					HighlightTraceRecord(traceRecord.TraceRecordPos, false);
				}
				else
				{
					foreach (ListViewItem item in listActivities.Items)
					{
						if (((Activity)item.Tag).Id == traceRecord.ActivityID)
						{
							HighlightTraceRecord(traceRecord, item, false,  messageView);
						}
					}
				}
				messageView.Select();
			}
		}

		private void messageView_MessageTraceItemDoubleClick(object o, EventArgs e)
		{
			InternalAnalysisActivityStub();
		}

		private void messageViewMenuItem_Click(object sender, EventArgs e)
		{
			LeftPanelStateController.SwitchState("LeftPanelMessageViewState");
		}

		private void ProjectManager_OnProjectNameChanged(string projectName)
		{
			ResetWindowTitle();
		}

		private void projectViewMenuItem_Click(object sender, EventArgs e)
		{
			LeftPanelStateController.SwitchState("LeftPanelProjectViewState");
		}

		private void SetFindDialogState()
		{
			if (ObjectStateController.CurrentStateName == "IdleProjectState")
			{
				if (LeftPanelStateController.CurrentStateName == "LeftPanelActivityViewState" || LeftPanelStateController.CurrentStateName == "LeftPanelTreeViewState")
				{
					findMenuItem.Enabled = true;
					findNextMenuItem.Enabled = true;
				}
				else
				{
					findMenuItem.Enabled = false;
					findNextMenuItem.Enabled = false;
				}
			}
		}

		private void SortActivitiesByColumn(bool isAsc, int columnIndex)
		{
			switch (columnIndex)
			{
			case 0:
				listActivities.ListViewItemSorter = new ListViewItemStringComparer(columnIndex, isAsc);
				break;
			case 1:
				listActivities.ListViewItemSorter = new ListViewItemNumberComparer(columnIndex, isAsc);
				break;
			case 3:
				listActivities.ListViewItemSorter = new ListViewItemTagComparer(isAsc, ListViewItemTagComparerTarget.ActivityStartTime);
				break;
			case 4:
				listActivities.ListViewItemSorter = new ListViewItemTagComparer(isAsc, ListViewItemTagComparerTarget.ActivityEndTime);
				break;
			case 2:
				listActivities.ListViewItemSorter = new ListViewItemTagComparer(isAsc, ListViewItemTagComparerTarget.ActivityDuration);
				break;
			default:
				listActivities.ListViewItemSorter = new ListViewItemStringComparer(columnIndex, isAsc);
				break;
			}
			listActivities.Columns[columnIndex].Tag = !isAsc;
		}

		private void swinLanes_OnClickTraceRecordItem(TraceRecord trace, string activityID)
		{
			if (trace != null && !string.IsNullOrEmpty(activityID))
			{
				if (listActivities.SelectedItems.Count != 0 && ((Activity)listActivities.SelectedItems[0].Tag).Id == activityID)
				{
					HighlightTraceRecord(trace.TraceRecordPos);
				}
				else
				{
					foreach (ListViewItem item in listActivities.Items)
					{
						if (((Activity)item.Tag).Id == activityID)
						{
							HighlightTraceRecord(trace, item, false, swimLanesControl.FocusControl);
						}
					}
				}
			}
		}

		private void toolStripOperationsCancelMenuItem_Click(object sender, EventArgs e)
		{
			lock (ThisLock)
			{
				Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
				DataSource.CancelCurrentTask();
				Thread.CurrentThread.Priority = ThreadPriority.Normal;
			}
		}

		private void TraceViewerForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			DataSource.CancelAllTasks();
			if (!projectManager.CloseProject())
			{
				e.Cancel = true;
			}
			if (!configManager.UpdateConfigFile())
			{
				e.Cancel = true;
			}
		}

		private void treeViewMenuItem_Click(object sender, EventArgs e)
		{
			LeftPanelStateController.SwitchState("LeftPanelTreeViewState");
		}

		private bool ValidateXmlTemplate(string xml)
		{
			if (!string.IsNullOrEmpty(xml))
			{
				try
				{
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.LoadXml(xml);
					if (xmlDocument.DocumentElement.Name != "E2ETraceEvent")
					{
						return false;
					}
					return true;
				}
				catch (XmlException)
				{
					return false;
				}
			}
			return false;
		}

		private void CopyCurrentTraceToClipboard()
		{
			try
			{
				if (listTracesDetail.SelectedItems.Count != 0)
				{
					TraceRecord traceRecord = (TraceRecord)listTracesDetail.SelectedItems[0].Tag;
					string xml = traceRecord.Xml;
					if (!string.IsNullOrEmpty(xml))
					{
						Utilities.CopyTextToClipboard(xml);
					}
					else
					{
						string text = traceRecord.TryAndGetXmlString();
						if (!string.IsNullOrEmpty(text))
						{
							Utilities.CopyTextToClipboard(text);
						}
					}
				}
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
		}

		internal bool FindNextTraceRecord(FindCriteria findingCriteria)
		{
			bool num = InternalFindNextTraceRecord(findingCriteria);
			if (num)
			{
				PerformHighlight(findingCriteria);
			}
			UpdateFindText(findingCriteria.FindingText);
			return num;
		}

		private void activityStepBackwardMenuItem_Click(object sender, EventArgs e)
		{
			if (CurrentTraceListCount != 0 && listTracesDetail.SelectedIndices.Count != 0)
			{
				int num = listTracesDetail.SelectedIndices[0];
				if (--num >= 0)
				{
					SetListViewItemFocus(listTracesDetail.Items[num]);
				}
			}
		}

		private void activityStepToNextTransferMenuItem_Click(object sender, EventArgs e)
		{
			int previousOrNextTransferInTraceList = GetPreviousOrNextTransferInTraceList(nextTransfer: true);
			if (previousOrNextTransferInTraceList != -1)
			{
				SetListViewItemFocus(listTracesDetail.Items[previousOrNextTransferInTraceList]);
			}
		}

		private void activityStepToPreviousTransferMenuItem_Click(object sender, EventArgs e)
		{
			int previousOrNextTransferInTraceList = GetPreviousOrNextTransferInTraceList(nextTransfer: false);
			if (previousOrNextTransferInTraceList != -1)
			{
				SetListViewItemFocus(listTracesDetail.Items[previousOrNextTransferInTraceList]);
			}
		}

		private void fileAddMenuItem_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog openFileDialog = new OpenFileDialog())
			{
				openFileDialog.CheckFileExists = true;
				openFileDialog.CheckPathExists = true;
				openFileDialog.ValidateNames = true;
				openFileDialog.Filter = SR.GetString("MainFrm_FileOpenDlg1");
				openFileDialog.FilterIndex = 0;
				openFileDialog.Multiselect = true;
				if (thisProxy.ShowDialog(openFileDialog, this) == DialogResult.OK)
				{
					AddFiles(openFileDialog.FileNames);
				}
			}
		}

		private void fileOpenMenuItem_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog openFileDialog = new OpenFileDialog())
			{
				openFileDialog.CheckFileExists = true;
				openFileDialog.CheckPathExists = true;
				openFileDialog.ValidateNames = true;
				openFileDialog.Filter = SR.GetString("MainFrm_FileOpenDlg2");
				openFileDialog.FilterIndex = 0;
				openFileDialog.Multiselect = true;
				if (thisProxy.ShowDialog(openFileDialog, this) == DialogResult.OK && projectManager.CloseProject())
				{
					LoadFiles(openFileDialog.FileNames);
				}
			}
		}

		private void fileProjectRecentMenuItem_Click(object sender, EventArgs e)
		{
			MenuItem menuItem = (MenuItem)sender;
			if (menuItem != null)
			{
				OpenProject((string)menuItem.Tag);
			}
		}

		private void fileProjectRecentMenuItem_Select(object sender, EventArgs e)
		{
			MenuItem menuItem = (MenuItem)sender;
			if (menuItem.IsParent)
			{
				menuItem.MenuItems.Clear();
			}
			Queue<string> queue = PersistedSettings.LoadRecentFiles(isProject: true);
			Stack<string> stack = new Stack<string>();
			while (queue.Count != 0)
			{
				stack.Push(queue.Dequeue());
			}
			while (stack.Count != 0)
			{
				string text = stack.Pop();
				MenuItem menuItem2 = new MenuItem(PersistedSettings.ParseFileName(text), fileProjectRecentMenuItem_Click);
				menuItem2.Tag = text;
				menuItem.MenuItems.Add(menuItem2);
			}
		}

		private void fileRecentMenuItem_Click(object sender, EventArgs e)
		{
			MenuItem menuItem = (MenuItem)sender;
			if (menuItem != null && projectManager.CloseProject())
			{
				LoadFiles(new string[1]
				{
					(string)menuItem.Tag
				});
			}
		}

		private void fileRecentMenuItem_Select(object sender, EventArgs e)
		{
			MenuItem menuItem = (MenuItem)sender;
			if (menuItem.IsParent)
			{
				menuItem.MenuItems.Clear();
			}
			Queue<string> queue = PersistedSettings.LoadRecentFiles(isProject: false);
			Stack<string> stack = new Stack<string>();
			while (queue.Count != 0)
			{
				stack.Push(queue.Dequeue());
			}
			while (stack.Count != 0)
			{
				string text = stack.Pop();
				MenuItem menuItem2 = new MenuItem(PersistedSettings.ParseFileName(text), fileRecentMenuItem_Click);
				menuItem2.Tag = text;
				menuItem.MenuItems.Add(menuItem2);
			}
		}

		private void fileTimeRangeStrip_OnTimeRangeChanged(DateTime start, DateTime end)
		{
			SwitchFilterMessageHelper();
			DataSource.ResetPartialLoadingTimeRange(start, end);
		}

		private void findMenuItem_Click(object sender, EventArgs e)
		{
			if (!findDialog.IsShown)
			{
				findDialog.Show(this);
			}
		}

		private void findNextMenuItem_Click(object sender, EventArgs e)
		{
			if (findDialog.CurrentFindCriteria == null && !findDialog.IsShown)
			{
				findDialog.Show(this);
			}
			else
			{
				findDialog.FindNext();
			}
		}

		private void groupByNoneStripMenuItem_Click(object sender, EventArgs e)
		{
			if (traceGroupByStateController.CurrentStateName != "GroupByNoneState" && traceGroupByStateController.SwitchState("GroupByNoneState"))
			{
				ReGroupTraceList();
			}
		}

		private void groupBySourceFileStripMenuItem_Click(object sender, EventArgs e)
		{
			if (traceGroupByStateController.CurrentStateName != "GroupBySourceFileState" && traceGroupByStateController.SwitchState("GroupBySourceFileState"))
			{
				ReGroupTraceList();
			}
		}

		private void HighlightTraceRecord(TraceRecordPosition tracePosition, bool isHighlight)
		{
			if (listActivities.SelectedItems.Count != 0)
			{
				if (IsTraceSelected)
				{
					TraceRecord currentSelectedTraceRecord = CurrentSelectedTraceRecord;
					if (currentSelectedTraceRecord.TraceRecordPos == tracePosition || (currentSelectedTraceRecord.TraceRecordPos.RelatedFileDescriptor == tracePosition.RelatedFileDescriptor && currentSelectedTraceRecord.TraceRecordPos.FileOffset == tracePosition.FileOffset))
					{
						CurrentSelectedTraceListItem.EnsureVisible();
						if (isHighlight)
						{
							Select();
						}
						return;
					}
				}
				foreach (ListViewItem item in listTracesDetail.Items)
				{
					TraceRecord traceRecord = (TraceRecord)item.Tag;
					if (traceRecord.TraceRecordPos == tracePosition || (traceRecord.TraceRecordPos.RelatedFileDescriptor == tracePosition.RelatedFileDescriptor && traceRecord.TraceRecordPos.FileOffset == tracePosition.FileOffset))
					{
						SetListViewItemFocus(item);
						if (isHighlight)
						{
							Select();
						}
						break;
					}
				}
			}
		}

		private void HighlightTraceRecord(TraceRecordPosition tracePosition)
		{
			HighlightTraceRecord(tracePosition, isHighlight: true);
		}

		private void HighlightTraceRecord(TraceRecord trace, ListViewItem activityItem, bool isHighlight, Control nextFocus)
		{
			if (activityItem.Selected)
			{
				HighlightTraceRecord(trace.TraceRecordPos, isHighlight);
			}
			else
			{
				listActivities.SelectedItems.Clear();
				intendSelectedTrace = new InternalTraceRecordSelector();
				intendSelectedTrace.TraceID = trace.TraceID;
				intendSelectedTrace.IsHighlight = isHighlight;
				intendSelectedTrace.NextFocus = nextFocus;
				SetListViewItemFocus(activityItem);
				if (isHighlight)
				{
					Select();
				}
			}
		}

		private void HighlightTraceRecord(TraceRecord trace, ListViewItem activityItem)
		{
			HighlightTraceRecord(trace, activityItem, true, null);
		}

		private void listActivities_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.A && e.Control)
			{
				SelectAllActivitiesInList();
			}
			if (e.KeyCode == Keys.C && e.Control && listActivities.SelectedItems.Count != 0)
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (ListViewItem selectedItem in listActivities.SelectedItems)
				{
					stringBuilder.AppendLine(selectedItem.SubItems[0].Text + SR.GetString("MainFrm_TabSingle") + selectedItem.SubItems[1].Text + SR.GetString("MainFrm_TabSingle") + selectedItem.SubItems[2].Text + SR.GetString("MainFrm_TabSingle") + selectedItem.SubItems[3].Text + SR.GetString("MainFrm_TabSingle") + selectedItem.SubItems[4].Text);
				}
				Utilities.CopyTextToClipboard(stringBuilder.ToString());
			}
		}

		private void listTracesDetail_DoubleClick(object o, EventArgs e)
		{
			bool flag = false;
			bool flag2 = false;
			if (listTracesDetail.SelectedItems != null && listTracesDetail.SelectedItems.Count != 0 && ((TraceRecord)listTracesDetail.SelectedItems[0].Tag).IsTransfer)
			{
				TraceRecord traceRecord = (TraceRecord)listTracesDetail.SelectedItems[0].Tag;
				foreach (ListViewItem selectedItem in listActivities.SelectedItems)
				{
					if (((Activity)selectedItem.Tag).Id == traceRecord.RelatedActivityID)
					{
						flag2 = true;
					}
					if (((Activity)selectedItem.Tag).Id == traceRecord.ActivityID)
					{
						flag = true;
					}
				}
				if (!flag2 || !flag)
				{
					if (flag2)
					{
						FollowTransfer(direction: false);
					}
					if (flag)
					{
						FollowTransfer(direction: true);
					}
				}
			}
		}

		private void listTracesDetail_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Apps && IsTraceSelected)
			{
				listTracesDetailMenuStrip.Show(listTracesDetail, new Point(listTracesDetail.Size.Width / 2, listTracesDetail.Size.Height / 2));
			}
			if (e.KeyCode == Keys.C && e.Control && listTracesDetail.SelectedItems.Count != 0)
			{
				CopyCurrentTraceToClipboard();
			}
			if ((e.KeyCode == Keys.Right || e.KeyCode == Keys.Left) && listTracesDetail.SelectedItems != null && listTracesDetail.SelectedItems.Count != 0)
			{
				TraceRecord traceRecord = (TraceRecord)listTracesDetail.SelectedItems[0].Tag;
				if (traceRecord != null && traceRecord.IsTransfer && listTracesDetail.SelectedItems[0].ImageIndex == 11 && e.KeyCode == Keys.Right)
				{
					FollowTransfer(direction: true);
				}
				else if (traceRecord != null && traceRecord.IsTransfer && listTracesDetail.SelectedItems[0].ImageIndex == 10 && e.KeyCode == Keys.Left)
				{
					FollowTransfer(direction: false);
				}
			}
		}

		private void listTracesDetail_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && IsTraceSelected)
			{
				listTracesDetailMenuStrip.Show(listTracesDetail, new Point(e.X, e.Y));
			}
		}

		private void refreshMenuItem_Click(object sender, EventArgs e)
		{
			PreserveUserSelections();
			DataSource.RefreshDataSource();
		}

		private void traceFilterBar_TraceFilterChangedEvent(object o, TraceFilterChangedEventArgs e)
		{
			PreserveUserSelections();
			DataSource.ReloadFiles();
		}

		private void UpdateFindText(string newText)
		{
			if (!string.IsNullOrEmpty(newText) && FindTextChangedCallback != null)
			{
				try
				{
					FindTextChangedCallback(newText);
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
				}
			}
		}

		internal bool FindNextTraceRecordInSwinLanes(FindCriteria findingCriteria)
		{
			if (LeftPanelStateController.CurrentStateName == "LeftPanelTreeViewState")
			{
				bool num = swimLanesControl.FindTraceRecord(findingCriteria);
				if (num)
				{
					PerformHighlight(findingCriteria);
				}
				return num;
			}
			UpdateFindText(findingCriteria.FindingText);
			return false;
		}

		internal void PerformHighlight(FindCriteria findingCriteria)
		{
			xmlViewRenderer.PrepareHighlight(findingCriteria);
			messageViewRenderer.PrepareHighlight(findingCriteria);
		}

		private bool FindNextTraceRecordInCurrentLoadedActivities(FindCriteria findingCriteria)
		{
			if (findingCriteria.Scope == FindingScope.CurrentLoadedActivities && !string.IsNullOrEmpty(findingCriteria.FindingText))
			{
				if (findingCriteria.Token == null)
				{
					findingCriteria.Token = GetCurrentFindingToken(true, true, ((findingCriteria.Options & FindingOptions.IgnoreRootActivity) <= FindingOptions.None) ? true : false);
				}
				else
				{
					findingCriteria.Token = GetCurrentFindingToken(true, false, ((findingCriteria.Options & FindingOptions.IgnoreRootActivity) <= FindingOptions.None) ? true : false);
				}
				foreach (ListViewItem findTraceRecordLVISequence in findingCriteria.Token.FindTraceRecordLVISequenceList)
				{
					TraceRecord traceRecord = (TraceRecord)findTraceRecordLVISequence.Tag;
					if (traceRecord.FindTraceRecord(findingCriteria))
					{
						HighlightTraceRecord(traceRecord.TraceRecordPos);
						return true;
					}
				}
				foreach (ListViewItem findActivitySequence in findingCriteria.Token.FindActivitySequenceList)
				{
					foreach (TraceRecordPosition traceRecordPosition in ((Activity)findActivitySequence.Tag).TraceRecordPositionList)
					{
						TraceRecord traceRecordFromPosition = TraceRecord.GetTraceRecordFromPosition(traceRecordPosition);
						if (traceRecordFromPosition != null && traceRecordFromPosition.FindTraceRecord(findingCriteria))
						{
							HighlightTraceRecord(traceRecordFromPosition, findActivitySequence);
							return true;
						}
					}
				}
			}
			return false;
		}

		private bool FindNextTraceRecordInCurrentLoadedTraces(FindCriteria findingCriteria)
		{
			if (findingCriteria.Scope == FindingScope.CurrentLoadedTraces && !string.IsNullOrEmpty(findingCriteria.FindingText))
			{
				if (findingCriteria.Token == null)
				{
					findingCriteria.Token = GetCurrentFindingToken(isNeedActivity: false, currentCounted: true, isRootCounted: false);
				}
				else
				{
					findingCriteria.Token = GetCurrentFindingToken(isNeedActivity: false, currentCounted: false, isRootCounted: false);
				}
				foreach (ListViewItem findTraceRecordLVISequence in findingCriteria.Token.FindTraceRecordLVISequenceList)
				{
					TraceRecord traceRecord = (TraceRecord)findTraceRecordLVISequence.Tag;
					if (traceRecord.FindTraceRecord(findingCriteria))
					{
						HighlightTraceRecord(traceRecord.TraceRecordPos);
						return true;
					}
				}
			}
			return false;
		}

		private FindingToken GetCurrentFindingToken(bool isNeedActivity, bool currentCounted, bool isRootCounted)
		{
			FindingToken findingToken = new FindingToken();
			if (CurrentTraceListCount != 0)
			{
				int num = 0;
				num = ((!IsTraceSelected) ? ((!currentCounted) ? (-1) : 0) : listTracesDetail.SelectedIndices[0]);
				for (int i = currentCounted ? num : (num + 1); i < CurrentTraceListCount; i++)
				{
					findingToken.FindTraceRecordLVISequenceList.Add(listTracesDetail.Items[i]);
				}
			}
			if (isNeedActivity && listActivities.Items.Count != 0)
			{
				int num2 = 0;
				num2 = ((listActivities.SelectedItems.Count == 0) ? (-1) : listActivities.SelectedIndices[0]);
				for (int j = num2 + 1; j < listActivities.Items.Count; j++)
				{
					if (isRootCounted || ((Activity)listActivities.Items[j].Tag).ActivityType != 0)
					{
						findingToken.FindActivitySequenceList.Add(listActivities.Items[j]);
					}
				}
			}
			return findingToken;
		}

		private bool InternalFindNextTraceRecord(FindCriteria findingCriteria)
		{
			switch (findingCriteria.Scope)
			{
			case FindingScope.CurrentLoadedTraces:
				return FindNextTraceRecordInCurrentLoadedTraces(findingCriteria);
			case FindingScope.CurrentLoadedActivities:
				return FindNextTraceRecordInCurrentLoadedActivities(findingCriteria);
			default:
				return false;
			}
		}

		private void AnaylsisActivityInTraceMode(Activity activity, TraceRecord trace)
		{
			if (ObjectStateController.CurrentStateName == "IdleProjectState")
			{
				leftPanelTab.SelectedIndex = 3;
				try
				{
					if (activity != null)
					{
						swimLanesControl.AnalysisActivityInTraceMode(activity, trace);
					}
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
				}
			}
		}

		public static void SwitchExecutionMode(bool isThreadMode)
		{
			if (currentTraceViewerForm != null)
			{
				isThreadExecutionMode = isThreadMode;
				currentTraceViewerForm.swimLanesControl.ReloadGraph();
			}
		}

		private void activityGraphViewMenuItem_Click(object sender, EventArgs e)
		{
			InternalAnalysisActivityStub();
		}

		private void AnaylsisActivityInTraceMode(Activity activity)
		{
			AnaylsisActivityInTraceMode(activity, null);
		}

		private void EnableFilterBar(bool isEnabling)
		{
			if (isEnabling)
			{
				traceFilterBar.Visible = true;
				viewFilterBarMenuItem.Checked = true;
			}
			else
			{
				traceFilterBar.Visible = false;
				viewFilterBarMenuItem.Checked = false;
			}
		}

		private void EnableFindBar(bool isEnabling)
		{
			if (isEnabling)
			{
				findToolbar.Visible = true;
				viewFindBarMenuItem.Checked = true;
			}
			else
			{
				findToolbar.Visible = false;
				viewFindBarMenuItem.Checked = false;
			}
		}

		private string[] GetDraggedFileNames(DragEventArgs e)
		{
			if ((ObjectStateController.CurrentStateName == "IdleProjectState" || ObjectStateController.CurrentStateName == "EmptyProjectState" || ObjectStateController.CurrentStateName == "NoFileProjectState") && TraceDataSourceStateController.CurrentStateName == "TraceDataSourceIdleState" && (e.AllowedEffect & DragDropEffects.Copy) == DragDropEffects.Copy && e.Data.GetDataPresent("FileDrop"))
			{
				Array array = e.Data.GetData("FileDrop") as Array;
				if (array == null || array.Length == 0 || array.Rank != 1)
				{
					return null;
				}
				List<string> list = new List<string>();
				if (array.Length == 1 && Path.GetExtension((string)array.GetValue(0)) == SR.GetString("PJ_Extension"))
				{
					return new string[1]
					{
						(string)array.GetValue(0)
					};
				}
				for (int i = 0; i < array.Length; i++)
				{
					if (array.GetValue(i) is string && !string.IsNullOrEmpty((string)array.GetValue(i)) && File.Exists((string)array.GetValue(i)))
					{
						try
						{
							if (!Utilities.CreateFileInfoHelper((string)array.GetValue(i)).Exists)
							{
								continue;
							}
						}
						catch (LogFileException)
						{
							continue;
						}
						if (!(Path.GetExtension((string)array.GetValue(i)) == SR.GetString("PJ_Extension")))
						{
							list.Add((string)array.GetValue(i));
						}
					}
				}
				string[] array2 = new string[list.Count];
				list.CopyTo(array2);
				return array2;
			}
			return null;
		}

		private void traceListCreateCustomFilterByTemplateStripButton_Click(object sender, EventArgs e)
		{
			CreateCustomFilterOnCurrentSelectedTrace();
		}

		private void TraceViewerForm_DragDrop(object sender, DragEventArgs e)
		{
			if ((ObjectStateController.CurrentStateName == "IdleProjectState" || ObjectStateController.CurrentStateName == "EmptyProjectState" || ObjectStateController.CurrentStateName == "NoFileProjectState") && TraceDataSourceStateController.CurrentStateName == "TraceDataSourceIdleState")
			{
				string[] draggedFileNames = GetDraggedFileNames(e);
				if (draggedFileNames != null)
				{
					if (draggedFileNames.Length == 1 && Path.GetExtension(draggedFileNames[0]) == SR.GetString("PJ_Extension"))
					{
						OpenProject(draggedFileNames[0]);
					}
					else
					{
						AddFiles(draggedFileNames);
					}
				}
			}
		}

		private void TraceViewerForm_DragEnter(object sender, DragEventArgs e)
		{
			if (ObjectStateController.CurrentStateName == "IdleProjectState" || ObjectStateController.CurrentStateName == "EmptyProjectState" || ObjectStateController.CurrentStateName == "NoFileProjectState")
			{
				string[] draggedFileNames = GetDraggedFileNames(e);
				if (draggedFileNames != null && draggedFileNames.Length != 0)
				{
					e.Effect = DragDropEffects.Copy;
				}
				else
				{
					e.Effect = DragDropEffects.None;
				}
			}
		}

		private void UpdateCurrentTraceForLoadedActivityList(List<Activity> loadedActivities)
		{
			if (loadedActivities != null && loadedActivities.Count != 0 && CurrentTraceListCount != 0 && IsTraceSelected)
			{
				TraceRecord currentSelectedTraceRecord = CurrentSelectedTraceRecord;
				string text = null;
				if (!currentSelectedTraceRecord.IsTransfer)
				{
					text = GetActivityDisplayName(currentSelectedTraceRecord.ActivityID);
				}
				else
				{
					foreach (Activity loadedActivity in loadedActivities)
					{
						if (loadedActivity.Id == currentSelectedTraceRecord.ActivityID || loadedActivity.Id == currentSelectedTraceRecord.RelatedActivityID)
						{
							text = GetActivityDisplayName(loadedActivity.Id);
							break;
						}
					}
				}
				if (!string.IsNullOrEmpty(text))
				{
					activityNameStripLabel.Text = SR.GetString("MainFrm_ActivityMenuNonEmpty") + ((text.Length > 50) ? (text.Substring(0, 50) + SR.GetString("MainFrm_ActivityMenuRes")) : text);
					activityNameStripLabel.ToolTipText = SR.GetString("MainFrm_ActivityMenuNonEmpty") + text;
				}
			}
			else
			{
				activityNameStripLabel.Text = SR.GetString("MainFrm_ActivityMenuEmpty");
				activityNameStripLabel.ToolTipText = SR.GetString("MainFrm_ActivityMenuEmpty");
			}
		}

		private void UpdateCurrentTraceForLoadedActivityList()
		{
			if (listActivities.SelectedItems.Count != 0)
			{
				List<Activity> list = new List<Activity>();
				foreach (ListViewItem selectedItem in listActivities.SelectedItems)
				{
					if (selectedItem.Tag != null && selectedItem.Tag is Activity)
					{
						list.Add((Activity)selectedItem.Tag);
					}
				}
				UpdateCurrentTraceForLoadedActivityList(list);
			}
			else
			{
				UpdateCurrentTraceForLoadedActivityList(null);
			}
		}

		private void viewFilterBarMenuItem_Click(object sender, EventArgs e)
		{
			if (viewFilterBarMenuItem.Checked)
			{
				EnableFilterBar(isEnabling: false);
			}
			else
			{
				EnableFilterBar(isEnabling: true);
			}
		}

		private void viewFindBarMenuItem_Click(object sender, EventArgs e)
		{
			if (viewFindBarMenuItem.Checked)
			{
				EnableFindBar(isEnabling: false);
			}
			else
			{
				EnableFindBar(isEnabling: true);
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
			components = new System.ComponentModel.Container();
			//base.Icon = ApplicationIcon;
			mainMenu = new System.Windows.Forms.MainMenu(components);
			fileMenu = new System.Windows.Forms.MenuItem();
			fileOpenMenuItem = new System.Windows.Forms.MenuItem();
			fileAddMenuItem = new System.Windows.Forms.MenuItem();
			fileCloseAllMenuItem = new System.Windows.Forms.MenuItem();
			fileSeparator1MenuItem = new System.Windows.Forms.MenuItem();
			fileOpenProjectMenuItem = new System.Windows.Forms.MenuItem();
			fileSaveProjectMenuItem = new System.Windows.Forms.MenuItem();
			fileSaveProjectAsMenuItem = new System.Windows.Forms.MenuItem();
			fileCloseProjectMenuItem = new System.Windows.Forms.MenuItem();
			fileSeparator3MenuItem = new System.Windows.Forms.MenuItem();
			fileRecentMenuItem = new System.Windows.Forms.MenuItem();
			fileProjectRecentMenuItem = new System.Windows.Forms.MenuItem();
			fileSeparator2MenuItem = new System.Windows.Forms.MenuItem();
			fileExitMenuItem = new System.Windows.Forms.MenuItem();
			editMenu = new System.Windows.Forms.MenuItem();
			findMenuItem = new System.Windows.Forms.MenuItem();
			findNextMenuItem = new System.Windows.Forms.MenuItem();
			viewMenu = new System.Windows.Forms.MenuItem();
			activityViewMenuItem = new System.Windows.Forms.MenuItem();
			projectViewMenuItem = new System.Windows.Forms.MenuItem();
			messageViewMenuItem = new System.Windows.Forms.MenuItem();
			treeViewMenuItem = new System.Windows.Forms.MenuItem();
			viewSeperator1MenuItem = new System.Windows.Forms.MenuItem();
			viewFilterBarMenuItem = new System.Windows.Forms.MenuItem();
			viewFindBarMenuItem = new System.Windows.Forms.MenuItem();
			viewSeperator2MenuItem = new System.Windows.Forms.MenuItem();
			customFiltersMenuItem = new System.Windows.Forms.MenuItem();
			filterNowMenuItem = new System.Windows.Forms.MenuItem();
			filterOptionsMenuItem = new System.Windows.Forms.MenuItem();
			viewSeperator3MenuItem = new System.Windows.Forms.MenuItem();
			refreshMenuItem = new System.Windows.Forms.MenuItem();
			activityMenu = new System.Windows.Forms.MenuItem();
			activityStepToNextTransferMenuItem = new System.Windows.Forms.MenuItem();
			activityFollowTransferMenuItem = new System.Windows.Forms.MenuItem();
			activityStepForwardMenuItem = new System.Windows.Forms.MenuItem();
			activityStepBackwardMenuItem = new System.Windows.Forms.MenuItem();
			activityStepToPreviousTransferMenuItem = new System.Windows.Forms.MenuItem();
			activitySeparatorMenuItem = new System.Windows.Forms.MenuItem();
			activityGraphViewMenuItem = new System.Windows.Forms.MenuItem();
			helpMenu = new System.Windows.Forms.MenuItem();
			helpHelpMenuItem = new System.Windows.Forms.MenuItem();
			helpAboutMenuItem = new System.Windows.Forms.MenuItem();
			toolStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
			statusStrip = new System.Windows.Forms.StatusStrip();
			numActivities = new System.Windows.Forms.ToolStripStatusLabel();
			numTraces = new System.Windows.Forms.ToolStripStatusLabel();
			toolStripOperationsMenu = new System.Windows.Forms.ToolStripDropDownButton();
			toolStripOperationsCancelMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			imageList = new System.Windows.Forms.ImageList(components);
			fileTimeRangeStrip = new Microsoft.Tools.ServiceModel.TraceViewer.FilePartialLoadingStrip();
			traceFilterBar = new Microsoft.Tools.ServiceModel.TraceViewer.TraceFilterControl(new ExceptionManager(), this);
			findToolbar = new Microsoft.Tools.ServiceModel.TraceViewer.FindToolBar();
			workAreaPanel = new System.Windows.Forms.Panel();
			mainPanel = new System.Windows.Forms.Panel();
			traceViewerMainPanel = new System.Windows.Forms.Panel();
			rightPanel = new System.Windows.Forms.Panel();
			traceDetailPanel = new System.Windows.Forms.Panel();
			tabs = new System.Windows.Forms.TabControl();
			tabStylesheet = new System.Windows.Forms.TabPage();
			traceDetailInfoControl = new Microsoft.Tools.ServiceModel.TraceViewer.TraceDetailInfoControl();
			tabBrowser = new System.Windows.Forms.TabPage();
			tabMessage = new System.Windows.Forms.TabPage();
			vertiSplitter = new System.Windows.Forms.Splitter();
			traceListPanel = new System.Windows.Forms.Panel();
			listTracesDetail = new System.Windows.Forms.ListView();
			traceListDescriptionColumn = new System.Windows.Forms.ColumnHeader();
			traceListActivityNameColumn = new System.Windows.Forms.ColumnHeader();
			traceListListColumn = new System.Windows.Forms.ColumnHeader();
			traceListThreadIDColumn = new System.Windows.Forms.ColumnHeader();
			traceListProcessNameColumn = new System.Windows.Forms.ColumnHeader();
			traceListTimeColumn = new System.Windows.Forms.ColumnHeader();
			traceListTraceCodeColumn = new System.Windows.Forms.ColumnHeader();
			traceListSourceColumn = new System.Windows.Forms.ColumnHeader();
			horizSplitter = new System.Windows.Forms.Splitter();
			leftPanelTab = new System.Windows.Forms.TabControl();
			leftPanelActivityTab = new System.Windows.Forms.TabPage();
			listActivities = new System.Windows.Forms.ListView();
			activityListActivityColumn = new System.Windows.Forms.ColumnHeader();
			activityListTraceColumn = new System.Windows.Forms.ColumnHeader();
			activityListDurationColumn = new System.Windows.Forms.ColumnHeader();
			activityListStartTickColumn = new System.Windows.Forms.ColumnHeader();
			activityListEndTickColumn = new System.Windows.Forms.ColumnHeader();
			leftPanelProjectTab = new System.Windows.Forms.TabPage();
			projectTree = new Microsoft.Tools.ServiceModel.TraceViewer.ProjectTreeViewControl();
			messageView = new Microsoft.Tools.ServiceModel.TraceViewer.MessageViewControl();
			leftPanelTreeTab = new System.Windows.Forms.TabPage();
			leftPanelMessageViewTab = new System.Windows.Forms.TabPage();
			traceListStripMenu = new System.Windows.Forms.MenuStrip();
			groupByStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			groupByNoneStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			groupBySourceFileStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			traceListCreateCustomFilterByTemplateStripButton = new System.Windows.Forms.ToolStripButton();
			activityNameStripLabel = new System.Windows.Forms.ToolStripLabel();
			swimLanesControl = new Microsoft.Tools.ServiceModel.TraceViewer.SwimLanesControl();
			xmlViewPanel = new System.Windows.Forms.Panel();
			messageViewPanel = new System.Windows.Forms.Panel();
			xmlViewRenderer = new Microsoft.Tools.ServiceModel.TraceViewer.RichTextXmlRenderer();
			messageViewRenderer = new Microsoft.Tools.ServiceModel.TraceViewer.RichTextXmlRenderer();
			statusStrip.SuspendLayout();
			workAreaPanel.SuspendLayout();
			mainPanel.SuspendLayout();
			traceViewerMainPanel.SuspendLayout();
			rightPanel.SuspendLayout();
			traceDetailPanel.SuspendLayout();
			tabs.SuspendLayout();
			tabStylesheet.SuspendLayout();
			tabBrowser.SuspendLayout();
			tabMessage.SuspendLayout();
			traceListPanel.SuspendLayout();
			leftPanelTab.SuspendLayout();
			leftPanelActivityTab.SuspendLayout();
			leftPanelProjectTab.SuspendLayout();
			SuspendLayout();
			mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[5]
			{
				fileMenu,
				editMenu,
				viewMenu,
				activityMenu,
				helpMenu
			});
			fileMenu.Index = 0;
			fileMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[13]
			{
				fileOpenMenuItem,
				fileAddMenuItem,
				fileCloseAllMenuItem,
				fileSeparator1MenuItem,
				fileOpenProjectMenuItem,
				fileSaveProjectMenuItem,
				fileSaveProjectAsMenuItem,
				fileCloseProjectMenuItem,
				fileSeparator3MenuItem,
				fileRecentMenuItem,
				fileProjectRecentMenuItem,
				fileSeparator2MenuItem,
				fileExitMenuItem
			});
			fileMenu.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_FileMenu");
			fileOpenMenuItem.Index = 0;
			fileOpenMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_OpenMI");
			fileOpenMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlO;
			fileOpenMenuItem.Click += new System.EventHandler(fileOpenMenuItem_Click);
			fileAddMenuItem.Index = 1;
			fileAddMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_AddMI");
			fileAddMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlD;
			fileAddMenuItem.Click += new System.EventHandler(fileAddMenuItem_Click);
			fileCloseAllMenuItem.Index = 2;
			fileCloseAllMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_CloseAllMI");
			fileCloseAllMenuItem.Click += new System.EventHandler(fileCloseAllMenuItem_Click);
			fileSeparator1MenuItem.Index = 3;
			fileSeparator1MenuItem.Text = "-";
			fileOpenProjectMenuItem.Index = 4;
			fileOpenProjectMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_OpenPrjMI");
			fileOpenProjectMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlP;
			fileOpenProjectMenuItem.Click += new System.EventHandler(fileOpenProjectMenuItem_Click);
			fileSaveProjectMenuItem.Index = 5;
			fileSaveProjectMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_SavePrjMI");
			fileSaveProjectMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlS;
			fileSaveProjectMenuItem.Click += new System.EventHandler(fileSaveProjectMenuItem_Click);
			fileSaveProjectAsMenuItem.Index = 6;
			fileSaveProjectAsMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_SavePrjAsMI");
			fileSaveProjectAsMenuItem.Click += new System.EventHandler(fileSaveProjectAsMenuItem_Click);
			fileCloseProjectMenuItem.Index = 7;
			fileCloseProjectMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ClosePrj");
			fileCloseProjectMenuItem.Click += new System.EventHandler(fileCloseProjectMenuItem_Click);
			fileSeparator3MenuItem.Index = 8;
			fileSeparator3MenuItem.Text = "-";
			fileRecentMenuItem.Index = 9;
			fileRecentMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_RecentMI");
			fileRecentMenuItem.Select += new System.EventHandler(fileRecentMenuItem_Select);
			fileProjectRecentMenuItem.Index = 10;
			fileProjectRecentMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_RecentProjectMI");
			fileProjectRecentMenuItem.Select += new System.EventHandler(fileProjectRecentMenuItem_Select);
			fileSeparator2MenuItem.Index = 11;
			fileSeparator2MenuItem.Text = "-";
			fileExitMenuItem.Index = 12;
			fileExitMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ExitMI");
			fileExitMenuItem.Click += new System.EventHandler(fileExitMenuItem_Click);
			editMenu.Index = 1;
			editMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[2]
			{
				findMenuItem,
				findNextMenuItem
			});
			editMenu.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_EditMenu");
			findMenuItem.Index = 0;
			findMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_FindMI");
			findMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlF;
			findMenuItem.Click += new System.EventHandler(findMenuItem_Click);
			findNextMenuItem.Index = 1;
			findNextMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_FindNextMI");
			findNextMenuItem.Shortcut = System.Windows.Forms.Shortcut.F3;
			findNextMenuItem.Click += new System.EventHandler(findNextMenuItem_Click);
			viewMenu.Index = 2;
			viewMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[13]
			{
				activityViewMenuItem,
				projectViewMenuItem,
				messageViewMenuItem,
				treeViewMenuItem,
				viewSeperator1MenuItem,
				viewFilterBarMenuItem,
				viewFindBarMenuItem,
				viewSeperator2MenuItem,
				filterNowMenuItem,
				customFiltersMenuItem,
				filterOptionsMenuItem,
				viewSeperator3MenuItem,
				refreshMenuItem
			});
			viewMenu.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ViewMenu");
			activityViewMenuItem.Checked = true;
			activityViewMenuItem.Index = 0;
			activityViewMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ActivityViewMI");
			activityViewMenuItem.Shortcut = System.Windows.Forms.Shortcut.Alt1;
			activityViewMenuItem.Click += new System.EventHandler(activityViewMenuItem_Click);
			projectViewMenuItem.Index = 1;
			projectViewMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ProjectViewMI");
			projectViewMenuItem.Shortcut = System.Windows.Forms.Shortcut.Alt2;
			projectViewMenuItem.Click += new System.EventHandler(projectViewMenuItem_Click);
			messageViewMenuItem.Index = 2;
			messageViewMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_MessageViewMI");
			messageViewMenuItem.Shortcut = System.Windows.Forms.Shortcut.Alt3;
			messageViewMenuItem.Click += new System.EventHandler(messageViewMenuItem_Click);
			treeViewMenuItem.Index = 3;
			treeViewMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_TreeViewMI");
			treeViewMenuItem.Shortcut = System.Windows.Forms.Shortcut.Alt4;
			treeViewMenuItem.Click += new System.EventHandler(treeViewMenuItem_Click);
			viewSeperator1MenuItem.Index = 4;
			viewSeperator1MenuItem.Text = "-";
			viewFilterBarMenuItem.Index = 5;
			viewFilterBarMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_FilterBar");
			viewFilterBarMenuItem.Click += new System.EventHandler(viewFilterBarMenuItem_Click);
			viewFindBarMenuItem.Index = 6;
			viewFindBarMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_FindToolBar");
			viewFindBarMenuItem.Click += new System.EventHandler(viewFindBarMenuItem_Click);
			viewSeperator2MenuItem.Index = 7;
			viewSeperator2MenuItem.Text = "-";
			filterNowMenuItem.Index = 8;
			filterNowMenuItem.Shortcut = System.Windows.Forms.Shortcut.AltF5;
			filterNowMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_FilterNowMI");
			filterNowMenuItem.Click += new System.EventHandler(filterNowMenuItem_Click);
			customFiltersMenuItem.Index = 9;
			customFiltersMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlF1;
			customFiltersMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_CustomFilterMI");
			customFiltersMenuItem.Click += new System.EventHandler(customFiltersMenuItem_Click);
			filterOptionsMenuItem.Index = 10;
			filterOptionsMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlF2;
			filterOptionsMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_FilterOptionMI");
			filterOptionsMenuItem.Click += new System.EventHandler(filterOptionsMenuItem_Click);
			viewSeperator3MenuItem.Index = 11;
			viewSeperator3MenuItem.Text = "-";
			refreshMenuItem.Index = 12;
			refreshMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_RefreshMI");
			refreshMenuItem.Shortcut = System.Windows.Forms.Shortcut.F5;
			refreshMenuItem.Click += new System.EventHandler(refreshMenuItem_Click);
			activityMenu.Index = 3;
			activityMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[7]
			{
				activityStepForwardMenuItem,
				activityStepBackwardMenuItem,
				activityStepToNextTransferMenuItem,
				activityStepToPreviousTransferMenuItem,
				activityFollowTransferMenuItem,
				activitySeparatorMenuItem,
				activityGraphViewMenuItem
			});
			activityMenu.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ActivityMenu");
			activityStepForwardMenuItem.Index = 0;
			activityStepForwardMenuItem.Shortcut = System.Windows.Forms.Shortcut.F10;
			activityStepForwardMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_FollowForwJO");
			activityStepForwardMenuItem.Click += new System.EventHandler(activityStepForwardMenuItem_Click);
			activityStepBackwardMenuItem.Index = 1;
			activityStepBackwardMenuItem.Shortcut = System.Windows.Forms.Shortcut.F9;
			activityStepBackwardMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_FollowBackMI");
			activityStepBackwardMenuItem.Click += new System.EventHandler(activityStepBackwardMenuItem_Click);
			activityStepToNextTransferMenuItem.Index = 2;
			activityStepToNextTransferMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlF10;
			activityStepToNextTransferMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_FollowForwMI");
			activityStepToNextTransferMenuItem.Click += new System.EventHandler(activityStepToNextTransferMenuItem_Click);
			activityStepToPreviousTransferMenuItem.Index = 3;
			activityStepToPreviousTransferMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlF9;
			activityStepToPreviousTransferMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_FollowForwJT");
			activityStepToPreviousTransferMenuItem.Click += new System.EventHandler(activityStepToPreviousTransferMenuItem_Click);
			activityFollowTransferMenuItem.Index = 4;
			activityFollowTransferMenuItem.Shortcut = System.Windows.Forms.Shortcut.F11;
			activityFollowTransferMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_FollowBackJI");
			activityFollowTransferMenuItem.Click += new System.EventHandler(activityFollowTransferMenuItem_Click);
			activitySeparatorMenuItem.Index = 5;
			activitySeparatorMenuItem.Text = "-";
			activityGraphViewMenuItem.Index = 6;
			activityGraphViewMenuItem.Shortcut = System.Windows.Forms.Shortcut.F4;
			activityGraphViewMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ActivityGraphMenu");
			activityGraphViewMenuItem.Click += new System.EventHandler(activityGraphViewMenuItem_Click);
			helpMenu.Index = 4;
			helpMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[2]
			{
				helpHelpMenuItem,
				helpAboutMenuItem
			});
			helpMenu.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_HelpMenu");
			helpHelpMenuItem.Index = 0;
			helpHelpMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_HelpMI");
			helpHelpMenuItem.Click += new System.EventHandler(helpHelpMenuItem_Click);
			helpHelpMenuItem.Shortcut = System.Windows.Forms.Shortcut.F1;
			helpAboutMenuItem.Index = 1;
			helpAboutMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_AboutMI");
			helpAboutMenuItem.Click += new System.EventHandler(helpAboutMenuItem_Click);
			toolStripProgressBar.AccessibleName = string.Empty;
			toolStripProgressBar.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			toolStripProgressBar.AutoSize = false;
			toolStripProgressBar.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			toolStripProgressBar.Name = "toolStripProgressBar";
			toolStripProgressBar.Size = new System.Drawing.Size(200, 20);
			toolStripProgressBar.Step = 2;
			statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[4]
			{
				numActivities,
				numTraces,
				toolStripProgressBar,
				toolStripOperationsMenu
			});
			statusStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Table;
			statusStrip.Location = new System.Drawing.Point(0, 558);
			statusStrip.Name = "statusStrip";
			statusStrip.Size = new System.Drawing.Size(861, 27);
			statusStrip.TabStop = false;
			numActivities.AccessibleDescription = numActivities.Text;
			numActivities.AccessibleName = string.Empty;
			numActivities.AutoSize = false;
			numActivities.Name = "numActivities";
			numActivities.Size = new System.Drawing.Size(150, 22);
			numTraces.AccessibleDescription = numTraces.Text;
			numTraces.AccessibleName = string.Empty;
			numTraces.AutoSize = false;
			numTraces.Name = "numTraces";
			numTraces.Size = new System.Drawing.Size(150, 22);
			toolStripOperationsMenu.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			toolStripOperationsMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[1]
			{
				toolStripOperationsCancelMenuItem
			});
			toolStripOperationsMenu.ImageTransparentColor = System.Drawing.Color.Magenta;
			toolStripOperationsMenu.Name = "toolStripOperationsMenu";
			toolStripOperationsMenu.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_OperationsMenu");
			toolStripOperationsCancelMenuItem.AccessibleName = string.Empty;
			toolStripOperationsCancelMenuItem.Name = "toolStripOperationsCancelMenuItem";
			toolStripOperationsCancelMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_CancelMI");
			toolStripOperationsCancelMenuItem.Click += new System.EventHandler(toolStripOperationsCancelMenuItem_Click);
			imageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
			imageList.ImageSize = new System.Drawing.Size(16, 16);
			imageList.TransparentColor = System.Drawing.Color.Transparent;
			groupByNoneStripMenuItem.Name = "groupByNoneStripMenuItem";
			groupByNoneStripMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GNone");
			groupByNoneStripMenuItem.Click += new System.EventHandler(groupByNoneStripMenuItem_Click);
			groupByNoneStripMenuItem.ToolTipText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GroupByNoneTip");
			groupBySourceFileStripMenuItem.Name = "groupBySourceFileStripMenuItem";
			groupBySourceFileStripMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GSource");
			groupBySourceFileStripMenuItem.Click += new System.EventHandler(groupBySourceFileStripMenuItem_Click);
			groupBySourceFileStripMenuItem.ToolTipText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GroupBySourceTip");
			groupByStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[2]
			{
				groupByNoneStripMenuItem,
				groupBySourceFileStripMenuItem
			});
			groupByStripMenuItem.Name = "groupByStripMenuItem";
			groupByStripMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GroupBy");
			groupByStripMenuItem.ToolTipText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_GroupByTip");
			traceListCreateCustomFilterByTemplateStripButton.AutoSize = true;
			traceListCreateCustomFilterByTemplateStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			traceListCreateCustomFilterByTemplateStripButton.Name = "traceListCreateCustomFilterByTemplateStripButton";
			traceListCreateCustomFilterByTemplateStripButton.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_CreateCustomFilter");
			traceListCreateCustomFilterByTemplateStripButton.ToolTipText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_CreateCustomFilterToolTip");
			traceListCreateCustomFilterByTemplateStripButton.Click += new System.EventHandler(traceListCreateCustomFilterByTemplateStripButton_Click);
			activityNameStripLabel.Name = "activityNameStripLabel";
			activityNameStripLabel.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ActivityMenuEmpty");
			activityNameStripLabel.ToolTipText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ActivityMenuEmpty");
			traceListStripMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[3]
			{
				groupByStripMenuItem,
				traceListCreateCustomFilterByTemplateStripButton,
				activityNameStripLabel
			});
			traceListStripMenu.Location = new System.Drawing.Point(0, 0);
			traceListStripMenu.Name = "groupByStripMenu";
			traceListStripMenu.Size = new System.Drawing.Size(636, 24);
			traceListStripMenu.ShowItemToolTips = true;
			traceListStripMenu.TabStop = true;
			traceListStripMenu.TabIndex = 1;
			fileTimeRangeStrip.Dock = System.Windows.Forms.DockStyle.Top;
			fileTimeRangeStrip.Location = new System.Drawing.Point(0, 0);
			fileTimeRangeStrip.Name = "fileTimeRangeStrip";
			fileTimeRangeStrip.Size = new System.Drawing.Size(861, 25);
			fileTimeRangeStrip.TabStop = false;
			traceFilterBar.Dock = System.Windows.Forms.DockStyle.Top;
			traceFilterBar.Location = new System.Drawing.Point(0, 25);
			traceFilterBar.Name = "traceFilterBar";
			traceFilterBar.Size = new System.Drawing.Size(861, 25);
			traceFilterBar.TabStop = false;
			findToolbar.Dock = System.Windows.Forms.DockStyle.Top;
			findToolbar.Location = new System.Drawing.Point(0, 25);
			findToolbar.Name = "findToolbar";
			findToolbar.Size = new System.Drawing.Size(861, 25);
			findToolbar.TabStop = false;
			workAreaPanel.Controls.Add(mainPanel);
			workAreaPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			workAreaPanel.Location = new System.Drawing.Point(0, 50);
			workAreaPanel.Name = "workAreaPanel";
			workAreaPanel.Size = new System.Drawing.Size(861, 508);
			workAreaPanel.TabStop = false;
			mainPanel.Controls.Add(traceViewerMainPanel);
			mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			mainPanel.Location = new System.Drawing.Point(0, 0);
			mainPanel.Name = "mainPanel";
			mainPanel.Size = new System.Drawing.Size(861, 508);
			mainPanel.TabStop = false;
			traceViewerMainPanel.Controls.Add(rightPanel);
			traceViewerMainPanel.Controls.Add(horizSplitter);
			traceViewerMainPanel.Controls.Add(leftPanelTab);
			traceViewerMainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			traceViewerMainPanel.Location = new System.Drawing.Point(0, 0);
			traceViewerMainPanel.Name = "traceViewerMainPanel";
			traceViewerMainPanel.Size = new System.Drawing.Size(861, 508);
			traceViewerMainPanel.TabStop = false;
			rightPanel.Controls.Add(traceDetailPanel);
			rightPanel.Controls.Add(vertiSplitter);
			rightPanel.Controls.Add(traceListPanel);
			rightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			rightPanel.Location = new System.Drawing.Point(203, 0);
			rightPanel.Name = "rightPanel";
			rightPanel.Size = new System.Drawing.Size(658, 508);
			rightPanel.TabStop = false;
			traceDetailPanel.Controls.Add(tabs);
			traceDetailPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			traceDetailPanel.Location = new System.Drawing.Point(0, 271);
			traceDetailPanel.Name = "traceDetailPanel";
			traceDetailPanel.Size = new System.Drawing.Size(658, 237);
			traceDetailPanel.TabStop = false;
			tabs.Controls.Add(tabStylesheet);
			tabs.Controls.Add(tabBrowser);
			tabs.Controls.Add(tabMessage);
			tabs.Dock = System.Windows.Forms.DockStyle.Fill;
			tabs.Location = new System.Drawing.Point(0, 0);
			tabs.Name = "tabs";
			tabs.SelectedIndex = 0;
			tabs.Size = new System.Drawing.Size(658, 237);
			tabs.TabStop = true;
			tabs.TabIndex = 3;
			tabStylesheet.Controls.Add(traceDetailInfoControl);
			tabStylesheet.Location = new System.Drawing.Point(4, 22);
			tabStylesheet.BackColor = System.Drawing.SystemColors.Window;
			tabStylesheet.Name = "tabStylesheet";
			tabStylesheet.Padding = new System.Windows.Forms.Padding(3);
			tabStylesheet.Size = new System.Drawing.Size(650, 211);
			tabStylesheet.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_FormattedTab");
			tabStylesheet.TabStop = false;
			traceDetailInfoControl.Dock = System.Windows.Forms.DockStyle.Fill;
			tabBrowser.Location = new System.Drawing.Point(4, 22);
			tabBrowser.BackColor = System.Drawing.SystemColors.Window;
			tabBrowser.Name = "tabBrowser";
			tabBrowser.Padding = new System.Windows.Forms.Padding(3);
			tabBrowser.Size = new System.Drawing.Size(650, 211);
			tabBrowser.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_XMLTab");
			tabBrowser.TabStop = false;
			tabBrowser.Controls.Add(xmlViewPanel);
			xmlViewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			xmlViewPanel.Location = new System.Drawing.Point(3, 3);
			xmlViewPanel.Name = "xmlViewPanel";
			xmlViewPanel.AutoSize = true;
			xmlViewPanel.TabStop = false;
			xmlViewPanel.AutoScroll = true;
			xmlViewPanel.Controls.Add(xmlViewRenderer);
			xmlViewRenderer.Location = new System.Drawing.Point(0, 0);
			xmlViewRenderer.Name = "xmlViewRenderer";
			xmlViewRenderer.TabStop = false;
			tabMessage.Location = new System.Drawing.Point(4, 22);
			tabMessage.BackColor = System.Drawing.SystemColors.Window;
			tabMessage.Name = "tabMessage";
			tabMessage.Padding = new System.Windows.Forms.Padding(3);
			tabMessage.Size = new System.Drawing.Size(650, 211);
			tabMessage.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_MessageTab");
			tabMessage.TabStop = false;
			tabMessage.Controls.Add(messageViewPanel);
			messageViewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			messageViewPanel.Location = new System.Drawing.Point(3, 3);
			messageViewPanel.Name = "messageViewPanel";
			messageViewPanel.AutoSize = true;
			messageViewPanel.TabStop = false;
			messageViewPanel.AutoScroll = true;
			messageViewPanel.Controls.Add(messageViewRenderer);
			messageViewRenderer.Location = new System.Drawing.Point(0, 0);
			messageViewRenderer.Name = "messageViewRenderer";
			messageViewRenderer.TabStop = false;
			vertiSplitter.Dock = System.Windows.Forms.DockStyle.Top;
			vertiSplitter.Location = new System.Drawing.Point(0, 268);
			vertiSplitter.Name = "vertiSplitter";
			vertiSplitter.Size = new System.Drawing.Size(658, 3);
			vertiSplitter.TabStop = false;
			traceListPanel.Controls.Add(listTracesDetail);
			traceListPanel.Controls.Add(traceListStripMenu);
			traceListPanel.Dock = System.Windows.Forms.DockStyle.Top;
			traceListPanel.Location = new System.Drawing.Point(0, 0);
			traceListPanel.Name = "traceListPanel";
			traceListPanel.Size = new System.Drawing.Size(658, 268);
			traceListPanel.TabStop = true;
			listTracesDetail.AllowColumnReorder = true;
			listTracesDetail.AutoArrange = false;
			listTracesDetail.Columns.AddRange(new System.Windows.Forms.ColumnHeader[8]
			{
				traceListDescriptionColumn,
				traceListListColumn,
				traceListThreadIDColumn,
				traceListProcessNameColumn,
				traceListTimeColumn,
				traceListTraceCodeColumn,
				traceListActivityNameColumn,
				traceListSourceColumn
			});
			listTracesDetail.ShowItemToolTips = true;
			listTracesDetail.Dock = System.Windows.Forms.DockStyle.Fill;
			listTracesDetail.SmallImageList = imageList;
			listTracesDetail.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f);
			listTracesDetail.FullRowSelect = true;
			listTracesDetail.HideSelection = false;
			listTracesDetail.Location = new System.Drawing.Point(0, 0);
			listTracesDetail.MultiSelect = false;
			listTracesDetail.Name = "listTracesDetail";
			listTracesDetail.Size = new System.Drawing.Size(658, 268);
			listTracesDetail.TabStop = true;
			listTracesDetail.TabIndex = 2;
			listTracesDetail.View = System.Windows.Forms.View.Details;
			listTracesDetail.MouseUp += new System.Windows.Forms.MouseEventHandler(listTracesDetail_MouseUp);
			listTracesDetail.DoubleClick += new System.EventHandler(listTracesDetail_DoubleClick);
			listTracesDetail.KeyDown += new System.Windows.Forms.KeyEventHandler(listTracesDetail_KeyDown);
			listTracesDetail.SelectedIndexChanged += new System.EventHandler(listTracesDetail_SelectedIndexChanged);
			listTracesDetail.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(listTracesDetail_ColumnClick);
			traceListDescriptionColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_DescriptionClm");
			traceListDescriptionColumn.Width = 300;
			traceListListColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_LevelClm");
			traceListListColumn.Width = 80;
			traceListThreadIDColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ThreadIDClm");
			traceListThreadIDColumn.Width = 80;
			traceListThreadIDColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			traceListProcessNameColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ProcessNameClm");
			traceListProcessNameColumn.Width = 80;
			traceListTimeColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_TimeClm");
			traceListTimeColumn.Width = 120;
			traceListTraceCodeColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_TraceCodeClm");
			traceListTraceCodeColumn.Width = 150;
			traceListActivityNameColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ActivityNameClm");
			traceListActivityNameColumn.Width = 80;
			traceListSourceColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_SourceClm");
			traceListSourceColumn.Width = 80;
			horizSplitter.Location = new System.Drawing.Point(200, 0);
			horizSplitter.Name = "horizSplitter";
			horizSplitter.Size = new System.Drawing.Size(3, 508);
			horizSplitter.TabStop = false;
			leftPanelTab.Alignment = System.Windows.Forms.TabAlignment.Top;
			leftPanelTab.Controls.Add(leftPanelActivityTab);
			leftPanelTab.Controls.Add(leftPanelProjectTab);
			leftPanelTab.Controls.Add(leftPanelMessageViewTab);
			leftPanelTab.Controls.Add(leftPanelTreeTab);
			leftPanelTab.Dock = System.Windows.Forms.DockStyle.Left;
			leftPanelTab.Location = new System.Drawing.Point(0, 0);
			leftPanelTab.Name = "leftPanelTab";
			leftPanelTab.SelectedIndex = 0;
			leftPanelTab.Size = new System.Drawing.Size(200, 508);
			leftPanelTab.TabStop = true;
			leftPanelTab.TabIndex = 0;
			leftPanelTab.SelectedIndexChanged += new System.EventHandler(leftPanelTab_SelectedIndexChanged);
			leftPanelActivityTab.Controls.Add(listActivities);
			leftPanelActivityTab.Location = new System.Drawing.Point(4, 4);
			leftPanelActivityTab.Name = "leftPanelActivityTab";
			leftPanelActivityTab.Padding = new System.Windows.Forms.Padding(3);
			leftPanelActivityTab.Size = new System.Drawing.Size(192, 482);
			leftPanelActivityTab.TabStop = false;
			leftPanelActivityTab.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ActivityTab");
			listActivities.AllowColumnReorder = true;
			listActivities.AutoArrange = false;
			listActivities.Columns.AddRange(new System.Windows.Forms.ColumnHeader[5]
			{
				activityListActivityColumn,
				activityListTraceColumn,
				activityListDurationColumn,
				activityListStartTickColumn,
				activityListEndTickColumn
			});
			listActivities.ShowItemToolTips = true;
			listActivities.Dock = System.Windows.Forms.DockStyle.Fill;
			listActivities.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f);
			listActivities.FullRowSelect = true;
			listActivities.HideSelection = false;
			listActivities.Location = new System.Drawing.Point(3, 3);
			listActivities.Name = "listActivities";
			listActivities.Size = new System.Drawing.Size(186, 392);
			listActivities.SmallImageList = imageList;
			listActivities.View = System.Windows.Forms.View.Details;
			listActivities.SelectedIndexChanged += new System.EventHandler(listActivities_SelectedIndexChanged);
			listActivities.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(listActivities_ColumnClick);
			listActivities.KeyDown += new System.Windows.Forms.KeyEventHandler(listActivities_KeyDown);
			listActivities.DoubleClick += new System.EventHandler(listActivities_DoubleClick);
			activityListActivityColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ActivityClm");
			activityListActivityColumn.Width = 120;
			activityListTraceColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_TracesClm");
			activityListTraceColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			activityListDurationColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_DurationClm");
			activityListDurationColumn.Width = 120;
			activityListStartTickColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_StartTicksClm");
			activityListStartTickColumn.Width = 80;
			activityListEndTickColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_EndTicksClm");
			activityListEndTickColumn.Width = 80;
			leftPanelProjectTab.Controls.Add(projectTree);
			leftPanelProjectTab.Location = new System.Drawing.Point(4, 4);
			leftPanelProjectTab.Name = "leftPanelProjectTab";
			leftPanelProjectTab.Padding = new System.Windows.Forms.Padding(3);
			leftPanelProjectTab.TabStop = false;
			leftPanelProjectTab.Size = new System.Drawing.Size(192, 482);
			leftPanelProjectTab.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_ProjectTab");
			projectTree.Dock = System.Windows.Forms.DockStyle.Fill;
			projectTree.Location = new System.Drawing.Point(3, 3);
			projectTree.Name = "projectTree";
			messageView.Dock = System.Windows.Forms.DockStyle.Fill;
			messageView.Location = new System.Drawing.Point(3, 3);
			messageView.Name = "messageView";
			leftPanelMessageViewTab.Controls.Add(messageView);
			leftPanelMessageViewTab.Location = new System.Drawing.Point(4, 4);
			leftPanelMessageViewTab.Name = "leftPanelMessageViewTab";
			leftPanelMessageViewTab.Padding = new System.Windows.Forms.Padding(3);
			leftPanelMessageViewTab.Size = new System.Drawing.Size(192, 482);
			leftPanelMessageViewTab.TabStop = false;
			leftPanelMessageViewTab.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_MessageViewTab");
			leftPanelTreeTab.Controls.Add(swimLanesControl);
			leftPanelTreeTab.Location = new System.Drawing.Point(4, 4);
			leftPanelTreeTab.Name = "leftPanelTreeTab";
			leftPanelTreeTab.Padding = new System.Windows.Forms.Padding(3);
			leftPanelTreeTab.Size = new System.Drawing.Size(192, 482);
			leftPanelTreeTab.TabStop = false;
			leftPanelTreeTab.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("MainFrm_TreeTab");
			swimLanesControl.Dock = System.Windows.Forms.DockStyle.Fill;
			swimLanesControl.Location = new System.Drawing.Point(3, 3);
			swimLanesControl.Name = "swimLanesControl";
			swimLanesControl.TabStop = false;
			AllowDrop = true;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(861, 585);
			base.Controls.Add(workAreaPanel);
			base.Controls.Add(findToolbar);
			base.Controls.Add(traceFilterBar);
			base.Controls.Add(fileTimeRangeStrip);
			base.Controls.Add(statusStrip);
			base.Menu = mainMenu;
			base.Name = "TraceViewerForm";
			MinimumSize = new System.Drawing.Size(400, 300);
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(TraceViewerForm_FormClosing);
			base.DragDrop += new System.Windows.Forms.DragEventHandler(TraceViewerForm_DragDrop);
			base.DragEnter += new System.Windows.Forms.DragEventHandler(TraceViewerForm_DragEnter);
			base.Load += new System.EventHandler(TraceViewerForm_Load);
			statusStrip.ResumeLayout(performLayout: false);
			workAreaPanel.ResumeLayout(performLayout: false);
			mainPanel.ResumeLayout(performLayout: false);
			traceViewerMainPanel.ResumeLayout(performLayout: false);
			rightPanel.ResumeLayout(performLayout: false);
			traceDetailPanel.ResumeLayout(performLayout: false);
			tabs.ResumeLayout(performLayout: false);
			tabStylesheet.ResumeLayout(performLayout: false);
			tabBrowser.ResumeLayout(performLayout: false);
			tabMessage.ResumeLayout(performLayout: false);
			traceListPanel.ResumeLayout(performLayout: false);
			leftPanelTab.ResumeLayout(performLayout: false);
			leftPanelActivityTab.ResumeLayout(performLayout: false);
			leftPanelProjectTab.ResumeLayout(performLayout: false);
			ResumeLayout(performLayout: false);
			PerformLayout();
		}
	}
}
