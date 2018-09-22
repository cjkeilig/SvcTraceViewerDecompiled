using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class ActivityTraceModeGraphProvider : GraphViewProvider
	{
		private class InternalPersistObject : IPersistStatus
		{
			public void OutputToStream(XmlTextWriter writer)
			{
				if (writer != null)
				{
					try
					{
						writer.WriteStartElement("traceModeViewSettings");
						writer.WriteStartElement("showActivityBoundary");
						writer.WriteString(showActivityBoundaryTracesMenuItem.Checked ? "1" : "0");
						writer.WriteEndElement();
						writer.WriteStartElement("showVerbose");
						writer.WriteString(showVerboseMenuItem.Checked ? "1" : "0");
						writer.WriteEndElement();
						writer.WriteStartElement("zoom");
						if (normalToolStripMenuItem.Checked)
						{
							writer.WriteString("0");
						}
						else if (smallToolStripMenuItem.Checked)
						{
							writer.WriteString("1");
						}
						else if (xSmallToolStripMenuItem.Checked)
						{
							writer.WriteString("2");
						}
						writer.WriteEndElement();
						writer.WriteEndElement();
					}
					catch (XmlException innerException)
					{
						throw new AppSettingsException(SR.GetString("MsgAppSettingSaveError"), innerException);
					}
				}
			}

			public void RestoreFromXMLNode(XmlNode node)
			{
				if (node != null)
				{
					try
					{
						XmlElement xmlElement = node["zoom"];
						if (xmlElement != null && !string.IsNullOrEmpty(xmlElement.InnerText))
						{
							normalToolStripMenuItem.Checked = false;
							smallToolStripMenuItem.Checked = false;
							xSmallToolStripMenuItem.Checked = false;
							switch (xmlElement.InnerText)
							{
							case "0":
								normalToolStripMenuItem.Checked = true;
								currentScale = WindowlessControlScale.Normal;
								break;
							case "1":
								smallToolStripMenuItem.Checked = true;
								currentScale = WindowlessControlScale.Small;
								break;
							case "2":
								xSmallToolStripMenuItem.Checked = true;
								currentScale = WindowlessControlScale.XSmall;
								break;
							default:
								normalToolStripMenuItem.Checked = true;
								currentScale = WindowlessControlScale.Normal;
								break;
							}
						}
						XmlElement xmlElement2 = node["showActivityBoundary"];
						if (xmlElement2 != null && !string.IsNullOrEmpty(xmlElement2.InnerText))
						{
							string innerText = xmlElement2.InnerText;
							if (!(innerText == "0"))
							{
								if (innerText == "1")
								{
									showActivityBoundaryTracesMenuItem.Checked = true;
								}
								else
								{
									showActivityBoundaryTracesMenuItem.Checked = true;
								}
							}
							else
							{
								showActivityBoundaryTracesMenuItem.Checked = false;
							}
						}
						XmlElement xmlElement3 = node["showVerbose"];
						if (xmlElement3 != null && !string.IsNullOrEmpty(xmlElement3.InnerText))
						{
							string innerText = xmlElement3.InnerText;
							if (!(innerText == "0"))
							{
								if (innerText == "1")
								{
									showVerboseMenuItem.Checked = true;
								}
								else
								{
									showVerboseMenuItem.Checked = true;
								}
							}
							else
							{
								showVerboseMenuItem.Checked = false;
							}
						}
					}
					catch (XmlException)
					{
					}
				}
			}

			public bool IsCurrentPersistNode(XmlNode node)
			{
				if (node != null && node.Name == "traceModeViewSettings")
				{
					return true;
				}
				return false;
			}
		}

		private class GraphViewTraceModePersistObject : GraphViewPersistObject
		{
			private int renderingHashCode;

			internal GraphViewTraceModePersistObject(GraphViewMode mode, string currentActivityId, object initData, int renderingHashCode)
				: base(mode, currentActivityId, initData)
			{
				this.renderingHashCode = renderingHashCode;
			}

			public override int GetHashCode()
			{
				return renderingHashCode;
			}
		}

		private const int VIEW_EDGE_SIZE = 10;

		private const int MAX_VIEW_SIZE = 32767;

		private EventHandler normalToolStripMenuItemClickHandler;

		private EventHandler smallToolStripMenuItemClickHandler;

		private EventHandler xSmallToolStripMenuItemClickHandler;

		private EventHandler showActivityBoundaryTracesMenuItemClickHandler;

		private EventHandler showVerboseMenuItemClickHandler;

		private EventHandler toolStripExecutionModeByProcessMenuItemClickHandler;

		private EventHandler toolStripExecutionModeByThreadMenuItemClickHandler;

		private ActivityTraceModeAnalyzerParameters parameters;

		private List<TraceRecordCellControl> unexpandedTransferOutControls = new List<TraceRecordCellControl>();

		private int renderingHashCode;

		private ActivityTraceModeAnalyzer currentAnalyzer;

		private bool isEmpty;

		private static WindowlessControlScale currentScale;

		private static ToolStripDropDownButton zoomStripDropDown;

		private static ToolStripMenuItem normalToolStripMenuItem;

		private static ToolStripMenuItem smallToolStripMenuItem;

		private static ToolStripMenuItem xSmallToolStripMenuItem;

		private static ToolStripDropDownButton optionsStripDropDown;

		private static ToolStripMenuItem showActivityBoundaryTracesMenuItem;

		private static ToolStripMenuItem showVerboseMenuItem;

		private static ToolStripDropDownButton executionRegionsDropDown;

		private static ToolStripDropDownButton toolStripExecutionModeMenu;

		private static ToolStripMenuItem toolStripExecutionModeByProcessMenuItem;

		private static ToolStripMenuItem toolStripExecutionModeByThreadMenuItem;

		protected override object InitializeData
		{
			get
			{
				if (parameters != null)
				{
					return new ActivityTraceModeAnalyzerParameters(parameters);
				}
				return null;
			}
		}

		internal override bool IsEmpty => isEmpty;

		internal override bool CanSupportZoom => true;

		internal override WindowlessControlScale CurrentScale => currentScale;

		internal static IPersistStatus GetViewSettingStatusObject()
		{
			return new InternalPersistObject();
		}

		static ActivityTraceModeGraphProvider()
		{
			zoomStripDropDown = new ToolStripDropDownButton();
			normalToolStripMenuItem = new ToolStripMenuItem();
			smallToolStripMenuItem = new ToolStripMenuItem();
			xSmallToolStripMenuItem = new ToolStripMenuItem();
			zoomStripDropDown.DisplayStyle = ToolStripItemDisplayStyle.Text;
			zoomStripDropDown.DropDownItems.AddRange(new ToolStripItem[3]
			{
				normalToolStripMenuItem,
				smallToolStripMenuItem,
				xSmallToolStripMenuItem
			});
			zoomStripDropDown.Name = "zoomStripDropDown";
			zoomStripDropDown.Text = SR.GetString("SL_Zoom");
			zoomStripDropDown.ToolTipText = SR.GetString("SL_ZoomTip");
			normalToolStripMenuItem.Checked = true;
			normalToolStripMenuItem.CheckState = CheckState.Checked;
			normalToolStripMenuItem.Name = "normalToolStripMenuItem";
			normalToolStripMenuItem.Text = SR.GetString("SL_Normal");
			normalToolStripMenuItem.ToolTipText = SR.GetString("SL_NormalTip");
			smallToolStripMenuItem.Name = "smallToolStripMenuItem";
			smallToolStripMenuItem.Text = SR.GetString("SL_Small");
			smallToolStripMenuItem.ToolTipText = SR.GetString("SL_SmallTip");
			xSmallToolStripMenuItem.Name = "xSmallToolStripMenuItem";
			xSmallToolStripMenuItem.Text = SR.GetString("SL_XSmall");
			xSmallToolStripMenuItem.ToolTipText = SR.GetString("SL_XSmallTip");
			optionsStripDropDown = new ToolStripDropDownButton();
			showActivityBoundaryTracesMenuItem = new ToolStripMenuItem();
			showVerboseMenuItem = new ToolStripMenuItem();
			optionsStripDropDown.DisplayStyle = ToolStripItemDisplayStyle.Text;
			optionsStripDropDown.DropDownItems.AddRange(new ToolStripItem[2]
			{
				showActivityBoundaryTracesMenuItem,
				showVerboseMenuItem
			});
			optionsStripDropDown.ImageTransparentColor = Color.Magenta;
			optionsStripDropDown.Name = "optionsStripDropDown";
			optionsStripDropDown.Text = SR.GetString("SL_Options");
			optionsStripDropDown.ToolTipText = SR.GetString("SL_OptionTip");
			showActivityBoundaryTracesMenuItem.Checked = true;
			showActivityBoundaryTracesMenuItem.CheckOnClick = true;
			showActivityBoundaryTracesMenuItem.CheckState = CheckState.Checked;
			showActivityBoundaryTracesMenuItem.Name = "showActivityBoundaryTracesMenuItem";
			showActivityBoundaryTracesMenuItem.Text = SR.GetString("SL_ShowA");
			showActivityBoundaryTracesMenuItem.ToolTipText = SR.GetString("SL_ShowATip");
			showVerboseMenuItem.Checked = true;
			showVerboseMenuItem.CheckOnClick = true;
			showVerboseMenuItem.CheckState = CheckState.Checked;
			showVerboseMenuItem.Name = "showVerboseMenuItem";
			showVerboseMenuItem.Text = SR.GetString("SL_ShowVerbose");
			showVerboseMenuItem.ToolTipText = SR.GetString("SL_ShowVerboseTip");
			executionRegionsDropDown = new ToolStripDropDownButton();
			executionRegionsDropDown.Name = "executionRegionsDropDown";
			executionRegionsDropDown.Text = SR.GetString("SL_ProcessList");
			executionRegionsDropDown.ToolTipText = SR.GetString("SL_ProcessListTip");
			executionRegionsDropDown.ShowDropDownArrow = true;
			toolStripExecutionModeMenu = new ToolStripDropDownButton();
			toolStripExecutionModeByThreadMenuItem = new ToolStripMenuItem();
			toolStripExecutionModeByProcessMenuItem = new ToolStripMenuItem();
			toolStripExecutionModeMenu.DisplayStyle = ToolStripItemDisplayStyle.Text;
			toolStripExecutionModeMenu.DropDownItems.AddRange(new ToolStripItem[2]
			{
				toolStripExecutionModeByProcessMenuItem,
				toolStripExecutionModeByThreadMenuItem
			});
			toolStripExecutionModeMenu.ImageTransparentColor = Color.Magenta;
			toolStripExecutionModeMenu.Name = "toolStripExecutionModeMenu";
			toolStripExecutionModeMenu.Text = SR.GetString("MainFrm_ExecProcessMode");
			toolStripExecutionModeMenu.ToolTipText = SR.GetString("MainFrm_ExecProcessModeTip0");
			toolStripExecutionModeByProcessMenuItem.Name = "toolStripExecutionModeByProcessMenuItem";
			toolStripExecutionModeByProcessMenuItem.Text = SR.GetString("MainFrm_ExecProcessMode2");
			toolStripExecutionModeByProcessMenuItem.Checked = true;
			toolStripExecutionModeByProcessMenuItem.Font = new Font(toolStripExecutionModeByProcessMenuItem.Font, FontStyle.Bold);
			toolStripExecutionModeByProcessMenuItem.ToolTipText = SR.GetString("MainFrm_ExecProcessModeTip");
			toolStripExecutionModeByThreadMenuItem.Name = "toolStripExecutionModeByThreadMenuItem";
			toolStripExecutionModeByThreadMenuItem.Text = SR.GetString("MainFrm_ExecThreadMode2");
			toolStripExecutionModeByThreadMenuItem.Checked = false;
			toolStripExecutionModeByThreadMenuItem.ToolTipText = SR.GetString("MainFrm_ExecThreadModeTip");
		}

		internal ActivityTraceModeGraphProvider(Activity activity, TraceDataSource dataSource, IWindowlessControlContainerExt container, IErrorReport errorReport, IUserInterfaceProvider userIP, GraphViewMode mode)
			: base(activity, dataSource, container, errorReport, userIP, mode)
		{
		}

		internal override bool BeforePerformAnalysis(object parameters)
		{
			if (parameters != null && parameters is ActivityTraceModeAnalyzerParameters)
			{
				this.parameters = (ActivityTraceModeAnalyzerParameters)parameters;
			}
			return true;
		}

		private void RefreshOptionMenuHighlightStatus()
		{
			if (!showActivityBoundaryTracesMenuItem.Checked || !showVerboseMenuItem.Checked)
			{
				optionsStripDropDown.ForeColor = Utilities.GetColor(ApplicationColors.HightlightedMenuColor);
			}
			else
			{
				optionsStripDropDown.ForeColor = SystemColors.ControlText;
			}
		}

		internal override void SetupToolbar(ToolStrip toolStrip)
		{
			if (toolStrip != null)
			{
				normalToolStripMenuItemClickHandler = normalToolStripMenuItem_Click;
				smallToolStripMenuItemClickHandler = smallToolStripMenuItem_Click;
				xSmallToolStripMenuItemClickHandler = xSmallToolStripMenuItem_Click;
				showActivityBoundaryTracesMenuItemClickHandler = showActivityBoundaryTracesMenuItem_Click;
				showVerboseMenuItemClickHandler = showVerboseMenuItem_Click;
				toolStripExecutionModeByProcessMenuItemClickHandler = toolStripExecutionModeByProcessMenuItem_Click;
				toolStripExecutionModeByThreadMenuItemClickHandler = toolStripExecutionModeByThreadMenuItem_Click;
				normalToolStripMenuItem.Click += normalToolStripMenuItemClickHandler;
				smallToolStripMenuItem.Click += smallToolStripMenuItemClickHandler;
				xSmallToolStripMenuItem.Click += xSmallToolStripMenuItemClickHandler;
				showActivityBoundaryTracesMenuItem.Click += showActivityBoundaryTracesMenuItemClickHandler;
				showVerboseMenuItem.Click += showVerboseMenuItemClickHandler;
				toolStripExecutionModeByProcessMenuItem.Click += toolStripExecutionModeByProcessMenuItemClickHandler;
				toolStripExecutionModeByThreadMenuItem.Click += toolStripExecutionModeByThreadMenuItemClickHandler;
				toolStrip.Items.AddRange(new ToolStripItem[3]
				{
					zoomStripDropDown,
					optionsStripDropDown,
					toolStripExecutionModeMenu
				});
				RefreshOptionMenuHighlightStatus();
				if (currentAnalyzer != null && currentAnalyzer.AllInvolvedExecutionItems != null && currentAnalyzer.AllInvolvedExecutionItems.Count > 1)
				{
					executionRegionsDropDown.DropDownItems.Clear();
					executionRegionsDropDown.Text = (TraceViewerForm.IsThreadExecutionMode ? SR.GetString("SL_ProcessList2") : SR.GetString("SL_ProcessList"));
					executionRegionsDropDown.ToolTipText = (TraceViewerForm.IsThreadExecutionMode ? SR.GetString("SL_ProcessListTip2") : SR.GetString("SL_ProcessListTip"));
					List<int> list = new List<int>();
					foreach (ExecutionColumnItem executionColumnItem in currentAnalyzer.ExecutionColumnItems)
					{
						list.Add(executionColumnItem.CurrentExecutionInfo.ExecutionID);
					}
					bool flag = false;
					foreach (ExecutionColumnItem allInvolvedExecutionItem in currentAnalyzer.AllInvolvedExecutionItems)
					{
						ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem();
						toolStripMenuItem.Text = allInvolvedExecutionItem.CurrentExecutionInfo.ProcessName + (TraceViewerForm.IsThreadExecutionMode ? (SR.GetString("CF_LeftB") + allInvolvedExecutionItem.CurrentExecutionInfo.ThreadID + SR.GetString("CF_RightB")) : string.Empty);
						toolStripMenuItem.ToolTipText = allInvolvedExecutionItem.CurrentExecutionInfo.ToString();
						toolStripMenuItem.Checked = (list.Contains(allInvolvedExecutionItem.CurrentExecutionInfo.ExecutionID) ? true : false);
						if (!toolStripMenuItem.Checked)
						{
							flag = true;
						}
						toolStripMenuItem.Tag = allInvolvedExecutionItem;
						toolStripMenuItem.Click += ProcessListItem_Click;
						executionRegionsDropDown.DropDownItems.Add(toolStripMenuItem);
					}
					if (flag)
					{
						executionRegionsDropDown.ForeColor = Utilities.GetColor(ApplicationColors.HightlightedMenuColor);
					}
					else
					{
						executionRegionsDropDown.ForeColor = SystemColors.ControlText;
					}
					toolStrip.Items.Add(executionRegionsDropDown);
				}
			}
		}

		private void ProcessListItem_Click(object sender, EventArgs e)
		{
			if (sender != null && sender is ToolStripMenuItem && ((ToolStripMenuItem)sender).Tag != null && ((ToolStripMenuItem)sender).Tag is ExecutionColumnItem)
			{
				ExecutionColumnItem executionColumnItem = (ExecutionColumnItem)((ToolStripMenuItem)sender).Tag;
				if (currentAnalyzer != null)
				{
					ActivityTraceModeAnalyzerParameters activityTraceModeAnalyzerParameters = currentAnalyzer.Parameters;
					activityTraceModeAnalyzerParameters = ((activityTraceModeAnalyzerParameters != null) ? new ActivityTraceModeAnalyzerParameters(activityTraceModeAnalyzerParameters) : new ActivityTraceModeAnalyzerParameters());
					((ToolStripMenuItem)sender).Checked = !((ToolStripMenuItem)sender).Checked;
					if (((ToolStripMenuItem)sender).Checked)
					{
						activityTraceModeAnalyzerParameters.RemoveSuppressedExecution(executionColumnItem.CurrentExecutionInfo);
					}
					else
					{
						activityTraceModeAnalyzerParameters.AppendSuppressedExecution(executionColumnItem.CurrentExecutionInfo);
					}
					base.Container.AnalysisActivityInTraceMode(currentAnalyzer.ActiveActivity, null, activityTraceModeAnalyzerParameters);
				}
			}
		}

		private void ProbingErrorsInView()
		{
			CollectUnexpandedTransfers();
			if (unexpandedTransferOutControls.Count != 0)
			{
				TraceRecordCellControl traceRecordCellControl = null;
				foreach (TraceRecordCellControl unexpandedTransferOutControl in unexpandedTransferOutControls)
				{
					if (unexpandedTransferOutControl.ExpandingState == ExpandingState.Collapsed)
					{
						TraceRecordSetSeverityLevel severityLevel = TraceRecordSetSeverityLevel.Normal;
						TraceRecord firstErrorTrace = null;
						ActivityAnalyzerHelper.DetectErrorOrWarningOnActivity(base.CurrentDataSource.Activities[unexpandedTransferOutControl.CurrentTraceRecordItem.CurrentTraceRecord.RelatedActivityID], base.CurrentDataSource.Activities, null, null, true, true, ref severityLevel, ref firstErrorTrace, ActivityAnalyzerHelper.INIT_ACTIVITY_TREE_DEPTH2);
						if (severityLevel != 0)
						{
							unexpandedTransferOutControl.CurrentTraceRecordItem.SeverityLevel = severityLevel;
							if (traceRecordCellControl == null)
							{
								traceRecordCellControl = unexpandedTransferOutControl;
								base.Container.ScrollControlIntoView(traceRecordCellControl, isCenter: true);
							}
						}
					}
				}
			}
		}

		private void showActivityBoundaryTracesMenuItem_Click(object sender, EventArgs e)
		{
			ReloadView();
		}

		private void showVerboseMenuItem_Click(object sender, EventArgs e)
		{
			ReloadView();
		}

		private void normalToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!normalToolStripMenuItem.Checked)
			{
				ResizeUI(WindowlessControlScale.Normal);
			}
			normalToolStripMenuItem.Checked = true;
			smallToolStripMenuItem.Checked = false;
			xSmallToolStripMenuItem.Checked = false;
		}

		private void smallToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!smallToolStripMenuItem.Checked)
			{
				ResizeUI(WindowlessControlScale.Small);
			}
			normalToolStripMenuItem.Checked = false;
			smallToolStripMenuItem.Checked = true;
			xSmallToolStripMenuItem.Checked = false;
		}

		private void xSmallToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!xSmallToolStripMenuItem.Checked)
			{
				ResizeUI(WindowlessControlScale.XSmall);
			}
			normalToolStripMenuItem.Checked = false;
			smallToolStripMenuItem.Checked = false;
			xSmallToolStripMenuItem.Checked = true;
		}

		internal override bool PerformAnalysis(bool isRestoring, bool reportError)
		{
			if (base.CurrentActivity != null && base.CurrentDataSource != null)
			{
				int num = 0;
				int width = 0;
				bool flag = false;
				ActivityTraceModeAnalyzer activityTraceModeAnalyzer = null;
				try
				{
					activityTraceModeAnalyzer = new ActivityTraceModeAnalyzer(base.CurrentActivity, base.CurrentDataSource, showActivityBoundaryTracesMenuItem.Checked, showVerboseMenuItem.Checked, parameters, base.ErrorReport);
				}
				catch (TraceViewerException exception)
				{
					if (reportError)
					{
						base.ErrorReport.ReportErrorToUser(exception);
					}
					return false;
				}
				renderingHashCode = activityTraceModeAnalyzer.RenderingHashCode;
				currentAnalyzer = activityTraceModeAnalyzer;
				if (currentAnalyzer.ContainsProcess)
				{
					isEmpty = false;
					foreach (HorzBundRowItem item in activityTraceModeAnalyzer)
					{
						HorzBundRowControl horzBundRowControl = new HorzBundRowControl(activityTraceModeAnalyzer, item, base.Container, new Point(0, num), base.ErrorReport);
						num += horzBundRowControl.Size.Height;
						width = horzBundRowControl.Size.Width;
					}
					if (activityTraceModeAnalyzer.MessageExchanges != null && activityTraceModeAnalyzer.MessageExchanges.Count != 0)
					{
						foreach (MessageExchangeCellItem value in activityTraceModeAnalyzer.MessageExchanges.Values)
						{
							if (!flag && value.SentExecutionColumnItem.CurrentExecutionInfo.GetHashCode() == value.ReceiveExecutionColumnItem.CurrentExecutionInfo.GetHashCode())
							{
								flag = true;
							}
							new MessageExchangeCellControl(base.Container, value, base.ErrorReport);
						}
					}
					ProbingErrorsInView();
					HorzBundTitleControl horzBundTitleControl = new HorzBundTitleControl(activityTraceModeAnalyzer, base.Container);
					horzBundTitleControl.Location = new Point(0, 0);
					base.Container.SetupTitleControl(horzBundTitleControl);
					Size size = new Size(width, num + 10);
					base.Container.SetupBodySize(size);
					base.Container.SetupTitleSize(new Size(size.Width, HorzBundTitleControl.GetDefaultHSize(base.Container.GetCurrentScale())));
					if (size.Width > 32767 || size.Height > 32767)
					{
						base.UserInterfaceProvider.ShowMessageBox(SR.GetString("SL_OutOfSize"), null, MessageBoxIcon.Exclamation, MessageBoxButtons.OK);
					}
				}
				else
				{
					isEmpty = true;
				}
				return true;
			}
			return false;
		}

		internal override object GetPersistObject()
		{
			if (base.CurrentActivity != null)
			{
				return new GraphViewTraceModePersistObject(base.CurrentViewMode, base.CurrentActivity.Id, InitializeData, renderingHashCode);
			}
			return null;
		}

		private void CollectUnexpandedTransfers()
		{
			Dictionary<int, LinkedList<WindowlessControlBase>> windowlessControls = base.Container.GetWindowlessControls();
			if (windowlessControls != null && windowlessControls.ContainsKey(2))
			{
				foreach (WindowlessControlBase item in windowlessControls[2])
				{
					if (item is TraceRecordCellControl)
					{
						TraceRecordCellControl traceRecordCellControl = (TraceRecordCellControl)item;
						if (traceRecordCellControl.CurrentTraceRecordItem != null && !unexpandedTransferOutControls.Contains(traceRecordCellControl) && traceRecordCellControl.ExpandingState == ExpandingState.Collapsed)
						{
							unexpandedTransferOutControls.Add(traceRecordCellControl);
						}
					}
				}
			}
		}

		protected override void DisposeObject()
		{
			if (normalToolStripMenuItemClickHandler != null)
			{
				normalToolStripMenuItem.Click -= normalToolStripMenuItemClickHandler;
			}
			if (smallToolStripMenuItemClickHandler != null)
			{
				smallToolStripMenuItem.Click -= smallToolStripMenuItemClickHandler;
			}
			if (xSmallToolStripMenuItemClickHandler != null)
			{
				xSmallToolStripMenuItem.Click -= xSmallToolStripMenuItemClickHandler;
			}
			if (showActivityBoundaryTracesMenuItemClickHandler != null)
			{
				showActivityBoundaryTracesMenuItem.Click -= showActivityBoundaryTracesMenuItemClickHandler;
			}
			if (showVerboseMenuItemClickHandler != null)
			{
				showVerboseMenuItem.Click -= showVerboseMenuItemClickHandler;
			}
			if (toolStripExecutionModeByProcessMenuItemClickHandler != null)
			{
				toolStripExecutionModeByProcessMenuItem.Click -= toolStripExecutionModeByProcessMenuItemClickHandler;
			}
			if (toolStripExecutionModeByThreadMenuItemClickHandler != null)
			{
				toolStripExecutionModeByThreadMenuItem.Click -= toolStripExecutionModeByThreadMenuItemClickHandler;
			}
		}

		private void ReloadView()
		{
			if (base.CurrentActivity != null && base.CurrentDataSource != null)
			{
				WindowlessControlBase topMostHighlighedControl = base.Container.GetTopMostHighlighedControl();
				base.Container.ClearView();
				if (PerformAnalysis(isRestoring: true, reportError: true) && topMostHighlighedControl != null && topMostHighlighedControl is TraceRecordCellControl)
				{
					TraceRecordCellControl traceRecordCellControl = (TraceRecordCellControl)topMostHighlighedControl;
					if (traceRecordCellControl.CurrentTraceRecordItem != null)
					{
						base.Container.SelectTraceRecordItem(traceRecordCellControl.CurrentTraceRecordItem.CurrentTraceRecord, traceRecordCellControl.CurrentActivityColumnItem.CurrentActivity.Id);
					}
				}
			}
			RefreshOptionMenuHighlightStatus();
		}

		private void toolStripExecutionModeByThreadMenuItem_Click(object sender, EventArgs e)
		{
			if (!toolStripExecutionModeByThreadMenuItem.Checked)
			{
				UncheckExecutionModeMenuItems();
				toolStripExecutionModeByThreadMenuItem.Checked = true;
				toolStripExecutionModeMenu.Text = SR.GetString("MainFrm_ExecThreadMode");
				TraceViewerForm.SwitchExecutionMode(isThreadMode: true);
			}
		}

		private void toolStripExecutionModeByProcessMenuItem_Click(object sender, EventArgs e)
		{
			if (!toolStripExecutionModeByProcessMenuItem.Checked)
			{
				UncheckExecutionModeMenuItems();
				toolStripExecutionModeByProcessMenuItem.Checked = true;
				toolStripExecutionModeMenu.Text = SR.GetString("MainFrm_ExecProcessMode");
				TraceViewerForm.SwitchExecutionMode(isThreadMode: false);
			}
		}

		private static void UncheckExecutionModeMenuItems()
		{
			toolStripExecutionModeByThreadMenuItem.Checked = false;
			toolStripExecutionModeByProcessMenuItem.Checked = false;
		}

		public void ResizeUI(WindowlessControlScale scale)
		{
			currentScale = scale;
			ReloadView();
		}
	}
}
