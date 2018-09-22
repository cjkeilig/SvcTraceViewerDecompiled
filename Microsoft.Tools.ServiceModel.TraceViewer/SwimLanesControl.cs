using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[ObjectStateMachine(typeof(SwimlanesDisableState), true, typeof(SwimlanesEnableState))]
	[ObjectStateMachine(typeof(SwimlanesEnableState), false, typeof(SwimlanesDisableState))]
	[ObjectStateTransfer("EmptyProjectState", "SwimlanesEnableState")]
	[ObjectStateTransfer("FileLoadingProjectState", "SwimlanesDisableState")]
	[ObjectStateTransfer("FileRemovingProjectState", "SwimlanesDisableState")]
	[ObjectStateTransfer("TraceLoadingProjectState", "SwimlanesDisableState")]
	[ObjectStateTransfer("FileReloadingProjectState", "SwimlanesDisableState")]
	[ObjectStateTransfer("IdleProjectState", "SwimlanesEnableState")]
	[ObjectStateTransfer("NoFileProjectState", "SwimlanesEnableState")]
	internal class SwimLanesControl : UserControl, IWindowlessControlContainerExt, IWindowlessControlContainer, IStateAwareObject
	{
		public delegate void ClickTraceRecordItem(TraceRecord trace, string activityID);

		private enum TraceRecordCellControlNextDirection
		{
			Up,
			Down,
			Left,
			Right
		}

		private TraceViewerForm parentContainer;

		private StateMachineController objectStateController;

		private const int MAX_LAYER_COUNT = 10;

		private GraphViewProvider currentGraphViewProvider;

		private TraceDataSource currentDataSource;

		private List<WindowlessControlBase> highlightedControls = new List<WindowlessControlBase>();

		private Dictionary<int, LinkedList<WindowlessControlBase>> internalWindowlessControls = new Dictionary<int, LinkedList<WindowlessControlBase>>();

		public ClickTraceRecordItem ClickTraceRecordItemCallback;

		private WindowlessControlExtentionEventCallback eventCallback;

		private Button scrollNavigatingControl;

		private object savedCurrentPersistObject;

		private int lastMouseDownYPos = -1;

		private WindowlessControlBase lastMouseOverControl;

		private Control internalFocusControl = new Control();

		private IUserInterfaceProvider userIP;

		private IErrorReport errorReport;

		private Stack<object> backwardPersistObjects = new Stack<object>();

		private Stack<object> forwardPersistObjects = new Stack<object>();

		private ToolStripButton backwardButton = new ToolStripButton();

		private ToolStripButton forwardButton = new ToolStripButton();

		private Color[] bufferColors = new Color[10]
		{
			Utilities.GetColor(ApplicationColors.RandomColor1),
			Utilities.GetColor(ApplicationColors.RandomColor2),
			Utilities.GetColor(ApplicationColors.RandomColor3),
			Utilities.GetColor(ApplicationColors.RandomColor4),
			Utilities.GetColor(ApplicationColors.RandomColor5),
			Utilities.GetColor(ApplicationColors.RandomColor6),
			Utilities.GetColor(ApplicationColors.RandomColor7),
			Utilities.GetColor(ApplicationColors.RandomColor8),
			Utilities.GetColor(ApplicationColors.RandomColor9),
			Utilities.GetColor(ApplicationColors.RandomColor10)
		};

		private IContainer components;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"SwimlanesEnableState"
		}, "Enabled")]
		private ToolStrip toolStripTop;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"SwimlanesEnableState"
		}, "Enabled")]
		private Panel titlePanel;

		private Panel upPanel;

		private Panel mainPanel;

		private TextBox focusTextControl;

		public Control FocusControl => internalFocusControl;

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

		public SwimLanesControl()
		{
			InitializeComponent();
		}

		public void Initialize(TraceViewerForm parent, IUserInterfaceProvider userIP, IErrorReport errorReport)
		{
			objectStateController = new StateMachineController(this);
			parent.ObjectStateController.RegisterStateSwitchListener(objectStateController);
			this.userIP = userIP;
			this.errorReport = errorReport;
			parent.DataSourceChangedHandler = (TraceViewerForm.DataSourceChanged)Delegate.Combine(parent.DataSourceChangedHandler, new TraceViewerForm.DataSourceChanged(DataSource_OnChanged));
			parent.SelectedTraceRecordChangedCallback = (TraceViewerForm.SelectedTraceRecordChanged)Delegate.Combine(parent.SelectedTraceRecordChangedCallback, new TraceViewerForm.SelectedTraceRecordChanged(TraceViewerForm_OnSelectedTraceRecordChanged));
			parentContainer = parent;
			ResetScrollingNavigator(isEnabling: false);
			SetInternalFocusControl(focusTextControl);
			InitializeDefaultToolbarItems();
		}

		public void ReloadGraph()
		{
			if (currentGraphViewProvider != null)
			{
				object persistObject = currentGraphViewProvider.GetPersistObject();
				if (persistObject != null)
				{
					GraphViewProvider.RestoreGraphView(persistObject, currentDataSource, this);
				}
			}
		}

		public Color GetRandColorByIndex(int index)
		{
			int result = 0;
			if (index >= 0)
			{
				Math.DivRem(index + bufferColors.Length - 1, bufferColors.Length, out result);
			}
			return bufferColors[result];
		}

		private void TraceViewerForm_OnSelectedTraceRecordChanged(TraceRecord trace, string activityId)
		{
			if (parentContainer.IsActivityGraphMode)
			{
				SelectTraceRecordItem(trace, activityId);
			}
		}

		public WindowlessControlScale GetCurrentScale()
		{
			if (currentGraphViewProvider != null && currentGraphViewProvider.CanSupportZoom)
			{
				return currentGraphViewProvider.CurrentScale;
			}
			return WindowlessControlScale.Normal;
		}

		public bool FindTraceRecord(FindCriteria findCriteria)
		{
			bool result = false;
			if (findCriteria != null && internalWindowlessControls != null && internalWindowlessControls.Count != 0)
			{
				findCriteria.Token = GetCurrentFindingToken((findCriteria.Token == null) ? true : false);
				if (findCriteria.Token.FindTraceRecordTRCSequenceList.Count != 0)
				{
					foreach (TraceRecordCellControl findTraceRecordTRCSequence in findCriteria.Token.FindTraceRecordTRCSequenceList)
					{
						if (findTraceRecordTRCSequence.CurrentTraceRecordItem != null && findTraceRecordTRCSequence.CurrentTraceRecordItem.CurrentTraceRecord.FindTraceRecord(findCriteria))
						{
							findTraceRecordTRCSequence.PerformClick();
							return true;
						}
					}
					return result;
				}
			}
			return result;
		}

		private FindingToken GetCurrentFindingToken(bool isCurrentContained)
		{
			FindingToken findingToken = new FindingToken();
			if (internalWindowlessControls != null && internalWindowlessControls.Count != 0)
			{
				Queue<TraceRecordCellControl> queue = new Queue<TraceRecordCellControl>();
				List<long> list = new List<long>();
				bool flag = false;
				TraceRecord traceRecord = null;
				foreach (WindowlessControlBase item in internalWindowlessControls[2])
				{
					if (item is TraceRecordCellControl && ((TraceRecordCellControl)item).CurrentTraceRecordItem != null && ((TraceRecordCellControl)item).CurrentTraceRecordItem.CurrentTraceRecord != null)
					{
						TraceRecordCellControl traceRecordCellControl = (TraceRecordCellControl)item;
						if (traceRecordCellControl.IsHighlighted)
						{
							flag = true;
							traceRecord = traceRecordCellControl.CurrentTraceRecordItem.CurrentTraceRecord;
						}
						if (!list.Contains(traceRecordCellControl.CurrentTraceRecordItem.CurrentTraceRecord.TraceID))
						{
							list.Add(traceRecordCellControl.CurrentTraceRecordItem.CurrentTraceRecord.TraceID);
							queue.Enqueue(traceRecordCellControl);
						}
					}
				}
				if (queue.Count != 0)
				{
					bool flag2 = false;
					while (queue.Count != 0)
					{
						TraceRecordCellControl traceRecordCellControl2 = queue.Dequeue();
						if (traceRecordCellControl2 != null && traceRecordCellControl2.CurrentTraceRecordItem != null)
						{
							TraceRecord currentTraceRecord = traceRecordCellControl2.CurrentTraceRecordItem.CurrentTraceRecord;
							if (flag && !flag2)
							{
								if (!(currentTraceRecord.TraceID == traceRecord.TraceID && isCurrentContained))
								{
									if (currentTraceRecord.TraceID == traceRecord.TraceID && !isCurrentContained)
									{
										flag2 = true;
									}
									continue;
								}
								flag2 = true;
							}
							findingToken.FindTraceRecordTRCSequenceList.Add(traceRecordCellControl2);
						}
					}
				}
			}
			return findingToken;
		}

		public void RegisterWindowlessControl(WindowlessControlBase control)
		{
			if (!internalWindowlessControls.ContainsKey(control.ZOrder))
			{
				internalWindowlessControls.Add(control.ZOrder, new LinkedList<WindowlessControlBase>());
			}
			internalWindowlessControls[control.ZOrder].AddLast(control);
		}

		public void InvalidateParent()
		{
			mainPanel.Invalidate();
		}

		public void InvalidateParent(Rectangle rect)
		{
			mainPanel.Invalidate(rect, invalidateChildren: false);
		}

		public void ScrollControlIntoView(WindowlessControlBase ctrl)
		{
			ScrollControlIntoView(ctrl, isCenter: false);
		}

		public void ScrollControlIntoView(WindowlessControlBase ctrl, bool isCenter)
		{
			if (ctrl != null)
			{
				if (!isCenter)
				{
					Point location = new Point(mainPanel.Location.X, mainPanel.Location.Y);
					location.Offset(ctrl.Location.X, ctrl.Location.Y);
					scrollNavigatingControl.Location = location;
					scrollNavigatingControl.Size = ctrl.Size;
					upPanel.ScrollControlIntoView(scrollNavigatingControl);
					SyncTitleControlXPos();
				}
				else
				{
					Point location2 = ctrl.Location;
					int width = upPanel.Width;
					int height = upPanel.Height;
					location2.Offset(-width / 2, -height / 2);
					int num = location2.X;
					if (num < upPanel.HorizontalScroll.Minimum)
					{
						num = upPanel.HorizontalScroll.Minimum;
					}
					else if (num > upPanel.HorizontalScroll.Maximum)
					{
						num = upPanel.HorizontalScroll.Maximum;
					}
					if (upPanel.HorizontalScroll.Visible)
					{
						upPanel.HorizontalScroll.Value = num;
					}
					int num2 = location2.Y;
					if (num2 < upPanel.VerticalScroll.Minimum)
					{
						num2 = upPanel.VerticalScroll.Minimum;
					}
					else if (num2 > upPanel.VerticalScroll.Maximum)
					{
						num2 = upPanel.VerticalScroll.Maximum;
					}
					if (upPanel.VerticalScroll.Visible)
					{
						upPanel.VerticalScroll.Value = num2;
					}
					ScrollControlIntoView(ctrl, isCenter: false);
				}
			}
		}

		public void HighlightSelectedTraceRecordRow(TraceRecordCellControl traceCell)
		{
			if (traceCell != null && traceCell.CurrentTraceRecordItem != null)
			{
				try
				{
					ClickTraceRecordItemCallback(traceCell.CurrentTraceRecordItem.CurrentTraceRecord, traceCell.CurrentTraceRecordItem.RelatedActivityItem.CurrentActivity.Id);
					SelectTraceRecordItem(traceCell.CurrentTraceRecordItem.CurrentTraceRecord, traceCell.CurrentTraceRecordItem.RelatedActivityItem.CurrentActivity.Id);
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
				}
			}
		}

		public void RegisterHighlightedControls(WindowlessControlBase control)
		{
			highlightedControls.Add(control);
		}

		public void RevertAllHighlightedControls()
		{
			foreach (WindowlessControlBase highlightedControl in highlightedControls)
			{
				try
				{
					highlightedControl.Highlight(isHighlight: false);
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
				}
			}
			highlightedControls.Clear();
		}

		public WindowlessControlBase FindWindowlessControl(object o)
		{
			WindowlessControlBase windowlessControlBase = null;
			if (o != null)
			{
				windowlessControlBase = InternalQuickFindWindowlessControl(o);
				if (windowlessControlBase == null)
				{
					foreach (LinkedList<WindowlessControlBase> value in internalWindowlessControls.Values)
					{
						foreach (WindowlessControlBase item in value)
						{
							if (item.IsFindingControl(o))
							{
								return item;
							}
						}
					}
					return windowlessControlBase;
				}
			}
			return windowlessControlBase;
		}

		private WindowlessControlBase InternalQuickFindWindowlessControl(object o)
		{
			if (o != null && o is TraceRecordCellItem && internalWindowlessControls.ContainsKey(2))
			{
				foreach (WindowlessControlBase item in internalWindowlessControls[2])
				{
					if (item.IsFindingControl(o))
					{
						return item;
					}
				}
			}
			return null;
		}

		public void AnalysisActivityInTraceMode(string activityId)
		{
			AnalysisActivityInTraceMode(GetActivityFromID(activityId));
		}

		public Activity GetActivityFromID(string activityId)
		{
			if (!string.IsNullOrEmpty(activityId) && currentDataSource != null && currentDataSource.Activities.ContainsKey(activityId))
			{
				return currentDataSource.Activities[activityId];
			}
			return null;
		}

		private void InitializeDefaultToolbarItems()
		{
			int index = -1;
			ImageList imageFromImageList = TraceViewerForm.GetImageFromImageList(Images.SL_Backward, out index);
			backwardButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
			if (imageFromImageList != null && index != -1 && index >= 0 && index < imageFromImageList.Images.Count)
			{
				backwardButton.Image = imageFromImageList.Images[index];
			}
			backwardButton.ImageAlign = ContentAlignment.MiddleCenter;
			backwardButton.ImageScaling = ToolStripItemImageScaling.SizeToFit;
			backwardButton.Click += backwardButton_Click;
			backwardButton.ToolTipText = SR.GetString("SL_BackwardTip");
			index = -1;
			imageFromImageList = TraceViewerForm.GetImageFromImageList(Images.SL_Forward, out index);
			forwardButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
			if (imageFromImageList != null && index != -1 && index >= 0 && index < imageFromImageList.Images.Count)
			{
				forwardButton.Image = imageFromImageList.Images[index];
			}
			forwardButton.ImageAlign = ContentAlignment.MiddleCenter;
			forwardButton.ImageScaling = ToolStripItemImageScaling.SizeToFit;
			forwardButton.Click += forwardButton_Click;
			forwardButton.ToolTipText = SR.GetString("SL_ForwardTip");
			SetupDefaultToolbar();
		}

		private void forwardButton_Click(object sender, EventArgs e)
		{
			Forward();
		}

		private void backwardButton_Click(object sender, EventArgs e)
		{
			Backward();
		}

		private void UpdateDefaultToolbarItems()
		{
			if (backwardPersistObjects.Count != 0)
			{
				backwardButton.Enabled = true;
			}
			else
			{
				backwardButton.Enabled = false;
			}
			if (forwardPersistObjects.Count != 0)
			{
				forwardButton.Enabled = true;
			}
			else
			{
				forwardButton.Enabled = false;
			}
		}

		private void SetupDefaultToolbar()
		{
			toolStripTop.Items.Clear();
			toolStripTop.Items.Add(backwardButton);
			toolStripTop.Items.Add(forwardButton);
			UpdateDefaultToolbarItems();
		}

		private void PushCurrentGraphViewIntoHistory()
		{
			if (currentGraphViewProvider != null)
			{
				object persistObject = currentGraphViewProvider.GetPersistObject();
				forwardPersistObjects.Clear();
				if (persistObject != null && (backwardPersistObjects.Count == 0 || backwardPersistObjects.Peek().GetHashCode() != persistObject.GetHashCode()))
				{
					backwardPersistObjects.Push(persistObject);
				}
			}
		}

		private void InternalAnalysisActivity(Activity activity, object initData, GraphViewMode mode, bool isUpdateBackward, object parameters)
		{
			InternalAnalysisActivity(activity, initData, mode, isUpdateBackward, parameters, isRestoring: false, reportError: true);
		}

		private void InternalAnalysisActivity(Activity activity, object initData, GraphViewMode mode, bool isUpdateBackward, object parameters, bool isRestoring, bool reportError)
		{
			if (activity != null && !string.IsNullOrEmpty(activity.Id) && currentDataSource != null && currentDataSource.Activities.ContainsKey(activity.Id))
			{
				GraphViewProvider graphViewProvider = GraphViewProvider.GetGraphViewProvider(activity, currentDataSource, this, errorReport, userIP, mode, initData);
				if (graphViewProvider == null)
				{
					userIP.ShowMessageBox(SR.GetString("SL_NOTANALYSIS"), null, MessageBoxIcon.Exclamation, MessageBoxButtons.OK);
				}
				else if (graphViewProvider.BeforePerformAnalysis(parameters))
				{
					if (isUpdateBackward)
					{
						PushCurrentGraphViewIntoHistory();
					}
					CleanUp();
					currentGraphViewProvider = graphViewProvider;
					if (currentGraphViewProvider.PerformAnalysis(isRestoring, reportError))
					{
						if (currentGraphViewProvider.IsEmpty && userIP.ShowMessageBox(SR.GetString("SL_GraphEmpty"), null, MessageBoxIcon.Exclamation, MessageBoxButtons.YesNo) == DialogResult.No)
						{
							Backward();
							forwardPersistObjects.Clear();
							UpdateDefaultToolbarItems();
						}
						else
						{
							currentGraphViewProvider.SetupToolbar(toolStripTop);
							if (parentContainer.CurrentSelectedTraceRecord != null && parentContainer.CurrentSelectedActivity != null)
							{
								SelectTraceRecordItem(parentContainer.CurrentSelectedTraceRecord, parentContainer.CurrentSelectedActivity.Id, isScrollToCenter: true);
							}
							ResetScrollingNavigator(isEnabling: true);
							mainPanel.Invalidate();
							SetFocus();
						}
					}
				}
			}
		}

		public void AnalysisActivityInTraceMode(Activity activity)
		{
			InternalAnalysisActivity(activity, null, GraphViewMode.TraceMode, true, null);
		}

		public void AnalysisActivityInTraceMode(Activity activity, TraceRecord trace, ActivityTraceModeAnalyzerParameters parameters)
		{
			InternalAnalysisActivity(activity, null, GraphViewMode.TraceMode, true, parameters, isRestoring: true, reportError: true);
			if (trace != null)
			{
				SelectTraceRecordItem(trace, trace.ActivityID, isScrollToCenter: true);
			}
		}

		public void AnalysisActivityInTraceMode(Activity activity, TraceRecord trace)
		{
			if (activity != null)
			{
				AnalysisActivityInTraceMode(activity);
			}
			if (trace != null)
			{
				SelectTraceRecordItem(trace, trace.ActivityID, isScrollToCenter: true);
			}
		}

		private void SetInternalFocusControl(Control ctrl)
		{
			if (ctrl != null)
			{
				internalFocusControl = ctrl;
			}
		}

		private void ResetScrollingNavigator(bool isEnabling)
		{
			if (scrollNavigatingControl == null)
			{
				scrollNavigatingControl = new Button();
				scrollNavigatingControl.BackColor = Color.Transparent;
				scrollNavigatingControl.FlatStyle = FlatStyle.Flat;
				scrollNavigatingControl.TabStop = false;
				upPanel.Controls.Add(scrollNavigatingControl);
			}
			scrollNavigatingControl.Location = new Point(0, 0);
			scrollNavigatingControl.Size = new Size(1, 1);
			scrollNavigatingControl.Visible = (isEnabling ? true : false);
		}

		public void ClearView()
		{
			InternalCleanUp(isCleanUpViewProvider: false);
		}

		private void InternalCleanUp(bool isCleanUpViewProvider)
		{
			ResetScrollingNavigator(isEnabling: false);
			if (titlePanel.Controls.Count != 0)
			{
				foreach (Control control in titlePanel.Controls)
				{
					control.Dispose();
				}
				titlePanel.Controls.Clear();
			}
			mainPanel.Size = new Size(1, 1);
			foreach (int key in internalWindowlessControls.Keys)
			{
				foreach (WindowlessControlBase item in internalWindowlessControls[key])
				{
					item.Dispose();
				}
			}
			internalWindowlessControls.Clear();
			mainPanel.Invalidate();
			if (isCleanUpViewProvider && currentGraphViewProvider != null)
			{
				currentGraphViewProvider.Dispose();
				currentGraphViewProvider = null;
				SetupDefaultToolbar();
			}
			eventCallback = null;
		}

		public void CleanUp()
		{
			InternalCleanUp(isCleanUpViewProvider: true);
		}

		public void SelectTraceRecordItem(TraceRecord trace, string activityId)
		{
			SelectTraceRecordItem(trace, activityId, isScrollToCenter: false);
		}

		private void SelectTraceRecordItem(TraceRecord trace, string activityId, bool isScrollToCenter)
		{
			try
			{
				if (trace == null)
				{
					RevertAllHighlightedControls();
				}
				else
				{
					WindowlessControlBase topMostHighlighedControl = GetTopMostHighlighedControl();
					if (topMostHighlighedControl == null || !(topMostHighlighedControl is TraceRecordCellControl))
					{
						goto IL_0069;
					}
					TraceRecordCellControl traceRecordCellControl = (TraceRecordCellControl)topMostHighlighedControl;
					if (traceRecordCellControl.CurrentTraceRecordItem == null || traceRecordCellControl.CurrentTraceRecordItem.CurrentTraceRecord.TraceID != trace.TraceID || !(traceRecordCellControl.CurrentTraceRecordItem.RelatedActivityItem.CurrentActivity.Id == activityId))
					{
						goto IL_0069;
					}
				}
				goto end_IL_0000;
				IL_0069:
				RevertAllHighlightedControls();
				if (internalWindowlessControls.ContainsKey(2) && internalWindowlessControls.Count != 0 && !string.IsNullOrEmpty(activityId))
				{
					foreach (WindowlessControlBase item in internalWindowlessControls[2])
					{
						if (item is TraceRecordCellControl)
						{
							TraceRecordCellControl traceRecordCellControl2 = (TraceRecordCellControl)item;
							if (traceRecordCellControl2.CurrentTraceRecordItem != null && traceRecordCellControl2.CurrentTraceRecordItem.CurrentTraceRecord.TraceID == trace.TraceID && activityId == traceRecordCellControl2.CurrentTraceRecordItem.RelatedActivityItem.CurrentActivity.Id)
							{
								item.Highlight(isHighlight: true);
								ScrollControlIntoView(item, isScrollToCenter);
								Point location = item.Location;
								location.Offset(2, 2);
								for (int i = 2; i <= 10; i++)
								{
									if (internalWindowlessControls.ContainsKey(i))
									{
										foreach (WindowlessControlBase item2 in internalWindowlessControls[i])
										{
											if (item2.IntersectsWith(location))
											{
												if (eventCallback != null)
												{
													eventCallback(item2, new WindowlessControlEventArgs(WindowlessControlEventType.ObjectClick));
												}
												if (!item2.OnClick(location))
												{
													return;
												}
											}
										}
									}
								}
								break;
							}
						}
					}
				}
				end_IL_0000:;
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
		}

		private void DataSource_OnChanged(TraceDataSource dataSource)
		{
			CleanUp();
			currentDataSource = dataSource;
			if (dataSource != null)
			{
				dataSource.AppendFileBeginCallback = (TraceDataSource.AppendFileBegin)Delegate.Combine(dataSource.AppendFileBeginCallback, new TraceDataSource.AppendFileBegin(DataSource_OnAppendFilesBegin));
				dataSource.AppendFileFinishedCallback = (TraceDataSource.AppendFileFinished)Delegate.Combine(dataSource.AppendFileFinishedCallback, new TraceDataSource.AppendFileFinished(DataSource_OnAppendFilesFinished));
				dataSource.RemoveFileBeginCallback = (TraceDataSource.RemoveFileBegin)Delegate.Combine(dataSource.RemoveFileBeginCallback, new TraceDataSource.RemoveFileBegin(DataSource_OnRemoveFileBegin));
				dataSource.RemoveFileFinishedCallback = (TraceDataSource.RemoveFileFinished)Delegate.Combine(dataSource.RemoveFileFinishedCallback, new TraceDataSource.RemoveFileFinished(DataSource_OnRemoveFileFinished));
				dataSource.RemoveAllFileFinishedCallback = (TraceDataSource.RemoveAllFileFinished)Delegate.Combine(dataSource.RemoveAllFileFinishedCallback, new TraceDataSource.RemoveAllFileFinished(DataSource_OnRemoveAllFilesFinished));
				dataSource.ReloadFilesBeginCallback = (TraceDataSource.ReloadFilesBegin)Delegate.Combine(dataSource.ReloadFilesBeginCallback, new TraceDataSource.ReloadFilesBegin(DataSource_OnReloadFilesBegin));
				dataSource.ReloadFilesFinishedCallback = (TraceDataSource.ReloadFilesFinished)Delegate.Combine(dataSource.ReloadFilesFinishedCallback, new TraceDataSource.ReloadFilesFinished(DataSource_OnReloadFilesFinished));
			}
		}

		private void Backward()
		{
			if (backwardPersistObjects.Count != 0)
			{
				object obj = backwardPersistObjects.Pop();
				if (obj != null)
				{
					if (currentGraphViewProvider != null)
					{
						object persistObject = currentGraphViewProvider.GetPersistObject();
						if (persistObject != null)
						{
							forwardPersistObjects.Push(persistObject);
						}
					}
					GraphViewProvider.RestoreGraphView(obj, currentDataSource, this);
				}
			}
			UpdateDefaultToolbarItems();
		}

		private void Forward()
		{
			if (forwardPersistObjects.Count != 0 && currentGraphViewProvider != null)
			{
				object persistObject = currentGraphViewProvider.GetPersistObject();
				object obj = forwardPersistObjects.Pop();
				if (persistObject != null && obj != null)
				{
					backwardPersistObjects.Push(persistObject);
					GraphViewProvider.RestoreGraphView(obj, currentDataSource, this);
				}
			}
			UpdateDefaultToolbarItems();
		}

		public void AnalysisActivityInHistory(Activity activity, GraphViewMode mode, object initData)
		{
			if (activity != null)
			{
				if (initData != null && initData is ActivityTraceModeAnalyzerParameters)
				{
					InternalAnalysisActivity(activity, initData, mode, false, (ActivityTraceModeAnalyzerParameters)initData, isRestoring: true, reportError: false);
				}
				else
				{
					InternalAnalysisActivity(activity, initData, mode, false, null, isRestoring: true, reportError: false);
				}
			}
		}

		private void PersistCurrentState()
		{
			if (currentGraphViewProvider != null)
			{
				savedCurrentPersistObject = currentGraphViewProvider.GetPersistObject(isInitData: false);
			}
			backwardPersistObjects.Clear();
			forwardPersistObjects.Clear();
		}

		private void RestoreSavedState()
		{
			if (savedCurrentPersistObject != null)
			{
				GraphViewProvider.RestoreGraphView(savedCurrentPersistObject, currentDataSource, this);
			}
			savedCurrentPersistObject = null;
		}

		private void DataSource_OnReloadFilesBegin()
		{
			PersistCurrentState();
		}

		private void DataSource_OnReloadFilesFinished()
		{
			RestoreSavedState();
		}

		private void DataSource_OnAppendFilesBegin(string[] filePaths)
		{
			PersistCurrentState();
			CleanUp();
		}

		private void DataSource_OnAppendFilesFinished(string[] fileNames, TaskInfoBase task)
		{
			RestoreSavedState();
		}

		private void DataSource_OnRemoveAllFilesFinished()
		{
			PersistCurrentState();
			CleanUp();
		}

		private void DataSource_OnRemoveFileBegin(string[] filesName)
		{
			PersistCurrentState();
			CleanUp();
		}

		private void DataSource_OnRemoveFileFinished(string[] filesName)
		{
			RestoreSavedState();
		}

		private void PaintOnMainPanel(Graphics graphics, Rectangle invalidatedRect)
		{
			if (graphics != null)
			{
				for (int num = 10; num >= 0; num--)
				{
					if (internalWindowlessControls.ContainsKey(num))
					{
						foreach (WindowlessControlBase item in internalWindowlessControls[num])
						{
							try
							{
								if (item.IntersectsWith(invalidatedRect))
								{
									item.OnPaint(graphics);
								}
							}
							catch (Exception e)
							{
								ExceptionManager.GeneralExceptionFilter(e);
							}
						}
					}
				}
			}
		}

		private void mainPanel_Paint(object sender, PaintEventArgs e)
		{
			if (e.Graphics != null)
			{
				PaintOnMainPanel(e.Graphics, e.ClipRectangle);
			}
		}

		private WindowlessControlBase GetWindowlessControlByPoint(Point point)
		{
			for (int i = 0; i <= 10; i++)
			{
				WindowlessControlBase windowlessControlByPoint = GetWindowlessControlByPoint(point, i);
				if (windowlessControlByPoint != null)
				{
					return windowlessControlByPoint;
				}
			}
			return null;
		}

		private WindowlessControlBase GetWindowlessControlByPoint(Point point, int zOrderIndex)
		{
			if (internalWindowlessControls.ContainsKey(zOrderIndex))
			{
				foreach (WindowlessControlBase item in internalWindowlessControls[zOrderIndex])
				{
					if (item.IntersectsWith(point))
					{
						return item;
					}
				}
			}
			return null;
		}

		private void mainPanel_MouseDown(object sender, MouseEventArgs e)
		{
			try
			{
				if (e.Button == MouseButtons.Left)
				{
					bool flag = true;
					RevertAllHighlightedControls();
					for (int i = 0; i <= 10; i++)
					{
						if (internalWindowlessControls.ContainsKey(i))
						{
							foreach (WindowlessControlBase item in internalWindowlessControls[i])
							{
								Point point = new Point(e.X, e.Y);
								if (item.IntersectsWith(point))
								{
									if (flag)
									{
										flag = false;
									}
									if (eventCallback != null)
									{
										eventCallback(item, new WindowlessControlEventArgs(WindowlessControlEventType.ObjectClick));
									}
									if (!item.OnClick(point))
									{
										return;
									}
								}
							}
						}
					}
				}
				else if (e.Button == MouseButtons.Right)
				{
					lastMouseDownYPos = e.Y;
				}
				SetFocus();
			}
			catch (Exception e2)
			{
				ExceptionManager.GeneralExceptionFilter(e2);
			}
		}

		private void mainPanel_MouseMove(object sender, MouseEventArgs e)
		{
			try
			{
				if (e.Button == MouseButtons.Right)
				{
					if (lastMouseDownYPos != -1 && upPanel.VerticalScroll.Visible)
					{
						int num = 0;
						if (e.Y > lastMouseDownYPos)
						{
							num = upPanel.VerticalScroll.Value + upPanel.VerticalScroll.SmallChange * 4;
							upPanel.VerticalScroll.Value = ((num > upPanel.VerticalScroll.Maximum) ? upPanel.VerticalScroll.Maximum : num);
						}
						else
						{
							num = upPanel.VerticalScroll.Value - upPanel.VerticalScroll.SmallChange * 4;
							upPanel.VerticalScroll.Value = ((num < upPanel.VerticalScroll.Minimum) ? upPanel.VerticalScroll.Minimum : num);
						}
					}
					lastMouseDownYPos = e.Y;
				}
				else if (e.Button == MouseButtons.None)
				{
					Point point = new Point(e.X, e.Y);
					WindowlessControlBase windowlessControlByPoint = GetWindowlessControlByPoint(point);
					if (windowlessControlByPoint != lastMouseOverControl)
					{
						if (lastMouseOverControl != null && lastMouseOverControl is WindowlessControlBaseExt)
						{
							PublishWindowlessControlExtMessage(((WindowlessControlBaseExt)lastMouseOverControl).OnMouseLeaveExt());
						}
						if (windowlessControlByPoint != null && windowlessControlByPoint is WindowlessControlBaseExt)
						{
							PublishWindowlessControlExtMessage(((WindowlessControlBaseExt)windowlessControlByPoint).OnMouseEnterExt(mainPanel, e.X, e.Y));
						}
						lastMouseOverControl = windowlessControlByPoint;
					}
				}
			}
			catch (Exception e2)
			{
				ExceptionManager.GeneralExceptionFilter(e2);
			}
		}

		private void PublishWindowlessControlExtMessage(WindowlessControlMessage message)
		{
			if (message != null)
			{
				for (int i = 0; i <= 10; i++)
				{
					if (internalWindowlessControls.ContainsKey(i))
					{
						foreach (WindowlessControlBase item in internalWindowlessControls[i])
						{
							if (item is WindowlessControlBaseExt && item != message.Sender)
							{
								((WindowlessControlBaseExt)item).OnWindowlessControlMessageReceived(message);
							}
						}
					}
				}
			}
		}

		private void mainPanel_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				lastMouseDownYPos = -1;
				Point point = new Point(e.X, e.Y);
				WindowlessControlBase windowlessControlByPoint = GetWindowlessControlByPoint(point);
				if (windowlessControlByPoint != null)
				{
					try
					{
						windowlessControlByPoint.GetContextMenu()?.Show(mainPanel, point);
					}
					catch (Exception e2)
					{
						ExceptionManager.GeneralExceptionFilter(e2);
					}
				}
			}
		}

		public WindowlessControlBase GetTopMostHighlighedControl()
		{
			int num = 10;
			WindowlessControlBase result = null;
			if (highlightedControls.Count != 0)
			{
				foreach (WindowlessControlBase highlightedControl in highlightedControls)
				{
					if (highlightedControl.ZOrder < num)
					{
						result = highlightedControl;
						num = highlightedControl.ZOrder;
					}
				}
				return result;
			}
			return result;
		}

		private TraceRecordCellControl GetNextTraceRecordCellControl(TraceRecordCellControlNextDirection direction)
		{
			if (internalWindowlessControls != null && internalWindowlessControls.Count != 0)
			{
				WindowlessControlBase topMostHighlighedControl = GetTopMostHighlighedControl();
				if (topMostHighlighedControl != null && topMostHighlighedControl is TraceRecordCellControl)
				{
					TraceRecordCellControl traceRecordCellControl = (TraceRecordCellControl)topMostHighlighedControl;
					if (internalWindowlessControls.ContainsKey(5) && internalWindowlessControls[5].Contains(traceRecordCellControl.ParentExecutionCellControl.ParentHorzBundRowCtrl))
					{
						LinkedListNode<WindowlessControlBase> linkedListNode = internalWindowlessControls[5].Find(traceRecordCellControl.ParentExecutionCellControl.ParentHorzBundRowCtrl);
						if (linkedListNode != null)
						{
							switch (direction)
							{
							case TraceRecordCellControlNextDirection.Down:
								while (linkedListNode.Next != null)
								{
									linkedListNode = linkedListNode.Next;
									if (linkedListNode.Value is HorzBundRowControl)
									{
										foreach (WindowlessControlBase childControl in ((HorzBundRowControl)linkedListNode.Value).ChildControls)
										{
											if (childControl is ExecutionCellControl && ((ExecutionCellControl)childControl).CurrentExecutionColumnItem == traceRecordCellControl.ParentExecutionCellControl.CurrentExecutionColumnItem)
											{
												foreach (WindowlessControlBase childControl2 in ((ExecutionCellControl)childControl).ChildControls)
												{
													if (childControl2 is TraceRecordCellControl && ((TraceRecordCellControl)childControl2).CurrentActivityColumnItem == traceRecordCellControl.CurrentActivityColumnItem && ((TraceRecordCellControl)childControl2).CurrentTraceRecordItem != null)
													{
														return (TraceRecordCellControl)childControl2;
													}
												}
											}
										}
									}
								}
								break;
							case TraceRecordCellControlNextDirection.Up:
								while (linkedListNode.Previous != null)
								{
									linkedListNode = linkedListNode.Previous;
									if (linkedListNode.Value is HorzBundRowControl)
									{
										foreach (WindowlessControlBase childControl3 in ((HorzBundRowControl)linkedListNode.Value).ChildControls)
										{
											if (childControl3 is ExecutionCellControl && ((ExecutionCellControl)childControl3).CurrentExecutionColumnItem == traceRecordCellControl.ParentExecutionCellControl.CurrentExecutionColumnItem)
											{
												foreach (WindowlessControlBase childControl4 in ((ExecutionCellControl)childControl3).ChildControls)
												{
													if (childControl4 is TraceRecordCellControl && ((TraceRecordCellControl)childControl4).CurrentActivityColumnItem == traceRecordCellControl.CurrentActivityColumnItem && ((TraceRecordCellControl)childControl4).CurrentTraceRecordItem != null)
													{
														return (TraceRecordCellControl)childControl4;
													}
												}
											}
										}
									}
								}
								break;
							case TraceRecordCellControlNextDirection.Right:
								if (traceRecordCellControl.CurrentTraceRecordItem != null && traceRecordCellControl.CurrentTraceRecordItem.CurrentTraceRecord.IsTransfer && IsTransferLeftToRight(traceRecordCellControl.CurrentTraceRecordItem))
								{
									foreach (WindowlessControlBase childControl5 in traceRecordCellControl.ParentExecutionCellControl.ChildControls)
									{
										if (childControl5 is TraceRecordCellControl && ((TraceRecordCellControl)childControl5).CurrentTraceRecordItem != null && ((TraceRecordCellControl)childControl5).CurrentTraceRecordItem.CurrentTraceRecord.IsTransfer && ((TraceRecordCellControl)childControl5).CurrentActivityColumnItem != traceRecordCellControl.CurrentActivityColumnItem)
										{
											return (TraceRecordCellControl)childControl5;
										}
									}
								}
								break;
							case TraceRecordCellControlNextDirection.Left:
								if (traceRecordCellControl.CurrentTraceRecordItem != null && traceRecordCellControl.CurrentTraceRecordItem.CurrentTraceRecord.IsTransfer && !IsTransferLeftToRight(traceRecordCellControl.CurrentTraceRecordItem))
								{
									foreach (WindowlessControlBase childControl6 in traceRecordCellControl.ParentExecutionCellControl.ChildControls)
									{
										if (childControl6 is TraceRecordCellControl && ((TraceRecordCellControl)childControl6).CurrentTraceRecordItem != null && ((TraceRecordCellControl)childControl6).CurrentTraceRecordItem.CurrentTraceRecord.IsTransfer && ((TraceRecordCellControl)childControl6).CurrentActivityColumnItem != traceRecordCellControl.CurrentActivityColumnItem)
										{
											return (TraceRecordCellControl)childControl6;
										}
									}
								}
								break;
							}
						}
					}
				}
			}
			return null;
		}

		private static bool IsTransferLeftToRight(TraceRecordCellItem trCellItem)
		{
			if (trCellItem != null && trCellItem.RelatedTraceRecordCellItem != null && trCellItem.RelatedActivityItem != null && trCellItem.RelatedTraceRecordCellItem.RelatedActivityItem != null)
			{
				if (trCellItem.RelatedActivityItem.ItemIndex >= trCellItem.RelatedTraceRecordCellItem.RelatedActivityItem.ItemIndex)
				{
					return false;
				}
				return true;
			}
			return true;
		}

		private void upPanel_Click(object sender, EventArgs e)
		{
			SetFocus();
		}

		public void SetFocus()
		{
			if (FocusControl != null)
			{
				FocusControl.Focus();
				FocusControl.Select();
			}
		}

		private void focusTextControl_MouseWheel(object sender, MouseEventArgs e)
		{
			ScrollViewOnMouseWheel(e.Delta);
			ScrollViewOnMouseWheel(e.Delta);
		}

		private void ScrollViewOnMouseWheel(int delta)
		{
			int value = upPanel.VerticalScroll.Value;
			if (delta < 0)
			{
				value += -delta;
				upPanel.VerticalScroll.Value = ((value > upPanel.VerticalScroll.Maximum) ? upPanel.VerticalScroll.Maximum : value);
			}
			else
			{
				value -= delta;
				upPanel.VerticalScroll.Value = ((value < upPanel.VerticalScroll.Minimum) ? upPanel.VerticalScroll.Minimum : value);
			}
		}

		private void SyncTitleControlXPos(int newXPos)
		{
			if (titlePanel.Controls.Count != 0)
			{
				Control control = titlePanel.Controls[0];
				Point location = new Point(0, 0);
				if (upPanel.HorizontalScroll.Visible)
				{
					location.Offset(-newXPos, 0);
				}
				control.Location = location;
			}
		}

		private void SyncTitleControlXPos()
		{
			SyncTitleControlXPos(upPanel.HorizontalScroll.Value);
		}

		private void upPanel_Resize(object sender, EventArgs e)
		{
			SyncTitleControlXPos();
		}

		private void upPanel_Scroll(object sender, ScrollEventArgs e)
		{
			if (upPanel.HorizontalScroll.Enabled && e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
			{
				SyncTitleControlXPos();
			}
		}

		private void focusTextControl_KeyUp(object sender, KeyEventArgs e)
		{
			TraceRecordCellControl traceRecordCellControl = null;
			switch (e.KeyCode)
			{
			case Keys.Up:
				traceRecordCellControl = GetNextTraceRecordCellControl(TraceRecordCellControlNextDirection.Up);
				break;
			case Keys.Down:
				traceRecordCellControl = GetNextTraceRecordCellControl(TraceRecordCellControlNextDirection.Down);
				break;
			case Keys.Left:
				traceRecordCellControl = GetNextTraceRecordCellControl(TraceRecordCellControlNextDirection.Left);
				break;
			case Keys.Right:
				traceRecordCellControl = GetNextTraceRecordCellControl(TraceRecordCellControlNextDirection.Right);
				break;
			case Keys.Add:
			{
				WindowlessControlBase topMostHighlighedControl2 = GetTopMostHighlighedControl();
				if (topMostHighlighedControl2 != null && topMostHighlighedControl2 is TraceRecordCellControl)
				{
					TraceRecordCellControl traceRecordCellControl3 = (TraceRecordCellControl)topMostHighlighedControl2;
					if (e.Control && titlePanel.Controls.Count == 1 && titlePanel.Controls[0] is HorzBundTitleControl)
					{
						((HorzBundTitleControl)titlePanel.Controls[0]).PerformExpandForActivity(traceRecordCellControl3.CurrentActivityColumnItem.CurrentActivity.Id);
					}
					else if (traceRecordCellControl3.ExpandingState == ExpandingState.Collapsed)
					{
						traceRecordCellControl3.PerformExpanding();
					}
				}
				return;
			}
			case Keys.Subtract:
			{
				WindowlessControlBase topMostHighlighedControl = GetTopMostHighlighedControl();
				if (topMostHighlighedControl != null && topMostHighlighedControl is TraceRecordCellControl)
				{
					TraceRecordCellControl traceRecordCellControl2 = (TraceRecordCellControl)topMostHighlighedControl;
					if (e.Control && titlePanel.Controls.Count == 1 && titlePanel.Controls[0] is HorzBundTitleControl)
					{
						((HorzBundTitleControl)titlePanel.Controls[0]).PerformCollapseForActivity(traceRecordCellControl2.CurrentActivityColumnItem.CurrentActivity.Id);
					}
					else if (traceRecordCellControl2.ExpandingState == ExpandingState.Expanded)
					{
						traceRecordCellControl2.PerformCollapse();
					}
				}
				return;
			}
			}
			traceRecordCellControl?.PerformClick();
		}

		public void RegisterExtentionEventListener(WindowlessControlExtentionEventCallback callback)
		{
			eventCallback = (WindowlessControlExtentionEventCallback)Delegate.Combine(eventCallback, callback);
		}

		public ToolStrip GetToolStripExtension()
		{
			return toolStripTop;
		}

		public void SetupTitleControl(Control titleControl)
		{
			titlePanel.Controls.Clear();
			if (titleControl != null)
			{
				titlePanel.Controls.Add(titleControl);
			}
		}

		public void SetupTitleSize(Size size)
		{
			titlePanel.Size = size;
		}

		public void SetupBodySize(Size size)
		{
			mainPanel.Size = size;
		}

		public Dictionary<int, LinkedList<WindowlessControlBase>> GetWindowlessControls()
		{
			return internalWindowlessControls;
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
			toolStripTop = new System.Windows.Forms.ToolStrip();
			titlePanel = new System.Windows.Forms.Panel();
			upPanel = new System.Windows.Forms.Panel();
			mainPanel = new System.Windows.Forms.Panel();
			focusTextControl = new System.Windows.Forms.TextBox();
			toolStripTop.SuspendLayout();
			upPanel.SuspendLayout();
			SuspendLayout();
			focusTextControl.Text = string.Empty;
			focusTextControl.Name = "focusTextControl";
			focusTextControl.TabIndex = 0;
			focusTextControl.Dock = System.Windows.Forms.DockStyle.Top;
			focusTextControl.Size = new System.Drawing.Size(600, 1);
			focusTextControl.Location = new System.Drawing.Point(0, 0);
			focusTextControl.Multiline = true;
			focusTextControl.ContextMenu = null;
			focusTextControl.ContextMenuStrip = null;
			focusTextControl.ShortcutsEnabled = false;
			focusTextControl.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			focusTextControl.KeyUp += new System.Windows.Forms.KeyEventHandler(focusTextControl_KeyUp);
			focusTextControl.MouseWheel += new System.Windows.Forms.MouseEventHandler(focusTextControl_MouseWheel);
			toolStripTop.Location = new System.Drawing.Point(0, 0);
			toolStripTop.Name = "toolStripTop";
			toolStripTop.Size = new System.Drawing.Size(455, 25);
			titlePanel.BackColor = System.Drawing.SystemColors.Window;
			titlePanel.Dock = System.Windows.Forms.DockStyle.Top;
			titlePanel.Name = "titlePanel";
			titlePanel.TabStop = false;
			upPanel.AutoScroll = true;
			upPanel.Location = new System.Drawing.Point(0, 0);
			upPanel.BackColor = System.Drawing.SystemColors.Window;
			upPanel.Controls.Add(mainPanel);
			upPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			upPanel.Name = "upPanel";
			upPanel.Height = ((base.Height - 100 > 0) ? (base.Height - 100) : 100);
			upPanel.Size = new System.Drawing.Size(455, 408);
			upPanel.TabStop = false;
			upPanel.Click += new System.EventHandler(upPanel_Click);
			upPanel.Scroll += new System.Windows.Forms.ScrollEventHandler(upPanel_Scroll);
			upPanel.Resize += new System.EventHandler(upPanel_Resize);
			mainPanel.BackColor = System.Drawing.SystemColors.Window;
			mainPanel.Location = new System.Drawing.Point(0, 0);
			mainPanel.Name = "mainPanel";
			mainPanel.Paint += new System.Windows.Forms.PaintEventHandler(mainPanel_Paint);
			mainPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(mainPanel_MouseMove);
			mainPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(mainPanel_MouseUp);
			mainPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(mainPanel_MouseDown);
			mainPanel.TabStop = false;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(upPanel);
			base.Controls.Add(titlePanel);
			base.Controls.Add(focusTextControl);
			base.Controls.Add(toolStripTop);
			DoubleBuffered = true;
			base.Name = "SwimLanesControl";
			base.Size = new System.Drawing.Size(455, 557);
			toolStripTop.ResumeLayout(performLayout: false);
			upPanel.ResumeLayout(performLayout: false);
			ResumeLayout(performLayout: false);
			PerformLayout();
		}
	}
}
