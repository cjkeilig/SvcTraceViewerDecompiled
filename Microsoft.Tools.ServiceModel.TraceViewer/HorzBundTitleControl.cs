using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class HorzBundTitleControl : UserControl
	{
		private class ActivityLabelTag
		{
			internal ActivityColumnItem activityColumnItem;

			internal ActivityTraceModeAnalyzer analyzer;

			internal ExpandingLevel expectedExpandingLevel;
		}

		private static Color defaultBackColor = Color.Blue;

		private static Color activeBackColor = Color.BlueViolet;

		private IWindowlessControlContainer container;

		private static Image processTitleBackImage = TempFileManager.GetImageFromEmbededResources(Images.ProcessTitleBack, Color.Transparent, isMakeTransparent: false);

		private static Image plusImage = TempFileManager.GetImageFromEmbededResources(Images.PlusIcon, Color.Black, isMakeTransparent: true);

		private static Image minusImage = TempFileManager.GetImageFromEmbededResources(Images.MinusIcon, Color.Black, isMakeTransparent: true);

		private Dictionary<string, PictureBox> activityExpandableControls = new Dictionary<string, PictureBox>();

		private IContainer components;

		private Label lblDate;

		private Panel mainPanel;

		private ToolTip toolTip;

		private PictureBox CreatePictureBox(ActivityColumnItem activityColumnItem, ActivityTraceModeAnalyzer analyzer, WindowlessControlScale scale)
		{
			if (activityColumnItem.CurrentActivity.ActivityType == ActivityType.RootActivity)
			{
				return null;
			}
			List<TraceRecord> list = null;
			try
			{
				list = activityColumnItem.CurrentActivity.LoadTraceRecords( true, null);
			}
			catch (LogFileException)
			{
				return null;
			}
			Dictionary<long, TraceRecord> dictionary = new Dictionary<long, TraceRecord>();
			Dictionary<long, TraceRecord> dictionary2 = new Dictionary<long, TraceRecord>();
			ActivityLabelTag activityLabelTag = new ActivityLabelTag();
			activityLabelTag.activityColumnItem = activityColumnItem;
			activityLabelTag.analyzer = analyzer;
			foreach (TraceRecord item in list)
			{
				if (ActivityTraceModeAnalyzer.IsValidForGraphFilter(item, analyzer.ContainsActivityBoundary, analyzer.ContainsVerboseTraces) && (analyzer.Parameters == null || !analyzer.Parameters.SuppressedExecutions.ContainsKey(item.Execution.ExecutionID)))
				{
					if (item.IsTransfer)
					{
						dictionary.Add(item.TraceID, item);
					}
					else if (item.Level == TraceEventType.Error || item.Level == TraceEventType.Warning || item.Level == TraceEventType.Critical)
					{
						dictionary.Add(item.TraceID, item);
					}
					else
					{
						dictionary2.Add(item.TraceID, item);
					}
				}
			}
			List<TraceRecordCellItem> resultTraceRecordItemsForActivity = analyzer.GetResultTraceRecordItemsForActivity(activityColumnItem.CurrentActivity);
			if (resultTraceRecordItemsForActivity != null && resultTraceRecordItemsForActivity.Count != 0 && dictionary.Count != 0 && dictionary2.Count != 0)
			{
				PictureBox pictureBox = new PictureBox();
				pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
				pictureBox.Size = GetExpandableIconSize(scale);
				pictureBox.Tag = activityLabelTag;
				pictureBox.Click += activityPlusBox_Click;
				if (dictionary.Count + dictionary2.Count != resultTraceRecordItemsForActivity.Count)
				{
					pictureBox.Image = plusImage;
					activityLabelTag.expectedExpandingLevel = ExpandingLevel.ExpandAll;
					return pictureBox;
				}
				foreach (TraceRecordCellItem item2 in resultTraceRecordItemsForActivity)
				{
					if (item2.CurrentTraceRecord != null && dictionary2.ContainsKey(item2.CurrentTraceRecord.TraceID))
					{
						pictureBox.Image = minusImage;
						activityLabelTag.expectedExpandingLevel = ExpandingLevel.ExpandTransferOut;
						return pictureBox;
					}
				}
			}
			return null;
		}

		public void PerformExpandForActivity(string activityId)
		{
			if (!string.IsNullOrEmpty(activityId) && activityExpandableControls.ContainsKey(activityId) && activityExpandableControls[activityId].Tag != null && ((ActivityLabelTag)activityExpandableControls[activityId].Tag).expectedExpandingLevel == ExpandingLevel.ExpandAll)
			{
				activityPlusBox_Click(activityExpandableControls[activityId], null);
			}
		}

		public void PerformCollapseForActivity(string activityId)
		{
			if (!string.IsNullOrEmpty(activityId) && activityExpandableControls.ContainsKey(activityId) && activityExpandableControls[activityId].Tag != null && ((ActivityLabelTag)activityExpandableControls[activityId].Tag).expectedExpandingLevel == ExpandingLevel.ExpandTransferOut)
			{
				activityPlusBox_Click(activityExpandableControls[activityId], null);
			}
		}

		private void activityPlusBox_Click(object sender, EventArgs e)
		{
			if (sender != null && sender is PictureBox && ((PictureBox)sender).Tag != null)
			{
				PictureBox pictureBox = (PictureBox)sender;
				if (pictureBox.Tag != null && pictureBox.Tag is ActivityLabelTag)
				{
					ActivityLabelTag activityLabelTag = (ActivityLabelTag)pictureBox.Tag;
					switch (activityLabelTag.expectedExpandingLevel)
					{
					case ExpandingLevel.ExpandAll:
					{
						ActivityTraceModeAnalyzerParameters parameters2 = activityLabelTag.analyzer.Parameters;
						parameters2 = ((parameters2 != null) ? new ActivityTraceModeAnalyzerParameters(parameters2) : new ActivityTraceModeAnalyzerParameters());
						parameters2.AppendExpandingActivity(activityLabelTag.activityColumnItem.CurrentActivity.Id, ExpandingLevel.ExpandAll);
						container.AnalysisActivityInTraceMode(activityLabelTag.analyzer.ActiveActivity, null, parameters2);
						break;
					}
					case ExpandingLevel.ExpandTransferOut:
					{
						ActivityTraceModeAnalyzerParameters parameters = activityLabelTag.analyzer.Parameters;
						parameters = ((parameters != null) ? new ActivityTraceModeAnalyzerParameters(parameters) : new ActivityTraceModeAnalyzerParameters());
						parameters.AppendExpandingActivity(activityLabelTag.activityColumnItem.CurrentActivity.Id, ExpandingLevel.ExpandTransferOut);
						container.AnalysisActivityInTraceMode(activityLabelTag.analyzer.ActiveActivity, null, parameters);
						break;
					}
					}
				}
			}
		}

		public static int GetDefaultVSize(WindowlessControlScale scale)
		{
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return 30;
			case WindowlessControlScale.Small:
				return 24;
			case WindowlessControlScale.XSmall:
				return 16;
			default:
				return 0;
			}
		}

		public static int GetDefaultHSize(WindowlessControlScale scale)
		{
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return 60;
			case WindowlessControlScale.Small:
				return 48;
			case WindowlessControlScale.XSmall:
				return 32;
			default:
				return 0;
			}
		}

		public static Size GetExpandableIconSize(WindowlessControlScale scale)
		{
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return new Size(9, 9);
			case WindowlessControlScale.Small:
				return new Size(7, 7);
			case WindowlessControlScale.XSmall:
				return new Size(5, 5);
			default:
				return new Size(9, 9);
			}
		}

		private static Font GetDefaultFont(WindowlessControlScale scale)
		{
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold);
			case WindowlessControlScale.Small:
				return new Font(FontFamily.GenericSansSerif, 7f, FontStyle.Bold);
			case WindowlessControlScale.XSmall:
				return new Font(FontFamily.GenericSansSerif, 5f);
			default:
				return new Font(FontFamily.GenericSansSerif, 8f);
			}
		}

		public HorzBundTitleControl(ActivityTraceModeAnalyzer analyzer, IWindowlessControlContainer container)
		{
			if (analyzer != null)
			{
				InitializeComponent();
				this.container = container;
				Label value = new Label
				{
					AutoSize = false,
					Dock = DockStyle.Bottom,
					Height = 1,
					BackColor = Utilities.GetColor(ApplicationColors.TitleBorder)
				};
				base.Controls.Add(value);
				WindowlessControlScale currentScale = container.GetCurrentScale();
				int num = HorzBundRowControl.GetTimeBoxSize(currentScale).Width + HorzBundRowControl.GetDefaultBlank(currentScale);
				int num2 = num + ExecutionCellControl.GetDefaultBlank(currentScale);
				int num3 = num;
				foreach (ExecutionColumnItem executionColumnItem in analyzer.ExecutionColumnItems)
				{
					int num4 = TraceRecordCellControl.GetControlSize(currentScale).Width * executionColumnItem.ActivityColumnCount + ExecutionCellControl.GetDefaultBlock(currentScale) * (executionColumnItem.ActivityColumnCount - 1) + 2 * ExecutionCellControl.GetDefaultBlank(currentScale);
					PictureBox pictureBox = new PictureBox();
					toolTip.SetToolTip(pictureBox, (string)pictureBox.Tag);
					pictureBox.Location = new Point(num, 0);
					pictureBox.BorderStyle = BorderStyle.None;
					pictureBox.Image = processTitleBackImage;
					pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
					pictureBox.Size = new Size(num4, GetDefaultVSize(currentScale));
					mainPanel.Controls.Add(pictureBox);
					num += num4 + HorzBundRowControl.GetDefaultBlock(currentScale);
					num3 += num4;
					Label label = new Label();
					if (TraceViewerForm.IsThreadExecutionMode)
					{
						label.Text = executionColumnItem.CurrentExecutionInfo.ProcessName + SR.GetString("CF_LeftB") + executionColumnItem.CurrentExecutionInfo.ThreadID + SR.GetString("CF_RightB");
					}
					else
					{
						label.Text = executionColumnItem.CurrentExecutionInfo.ProcessName;
					}
					label.Font = GetDefaultFont(currentScale);
					label.TextAlign = ContentAlignment.MiddleCenter;
					label.Dock = DockStyle.Fill;
					label.BackColor = Color.Transparent;
					toolTip.SetToolTip(label, executionColumnItem.CurrentExecutionInfo.ToString());
					pictureBox.Controls.Add(label);
					for (int i = 0; i < executionColumnItem.ActivityColumnCount; i++)
					{
						Activity currentActivity = executionColumnItem[i].CurrentActivity;
						Label label2 = new Label();
						string activityDisplayName = TraceViewerForm.GetActivityDisplayName(currentActivity);
						toolTip.SetToolTip(label2, activityDisplayName);
						int index = -1;
						ImageList imageList = null;
						if (container.GetCurrentScale() != WindowlessControlScale.XSmall)
						{
							if (executionColumnItem[i].CurrentActivity != null && executionColumnItem[i].CurrentActivity.ActivityType == ActivityType.RootActivity)
							{
								imageList = TraceViewerForm.GetImageFromImageList(Images.RootActivity, out index);
							}
							else if (executionColumnItem[i].CurrentActivity != null && ActivityAnalyzerHelper.IsHostRelatedActivity(executionColumnItem[i].CurrentActivity))
							{
								imageList = ((executionColumnItem[i].CurrentActivity.ActivityType != ActivityType.ListenActivity) ? TraceViewerForm.GetImageFromImageList(Images.HostActivityIcon, out index) : TraceViewerForm.GetImageFromImageList(Images.ListenActivity, out index));
							}
							else if (executionColumnItem[i].CurrentActivity != null && ActivityAnalyzerHelper.IsMessageRelatedActivity(executionColumnItem[i].CurrentActivity))
							{
								imageList = TraceViewerForm.GetImageFromImageList(Images.MessageActivityIcon, out index);
								if (executionColumnItem[i].CurrentActivity.ActivityType == ActivityType.UserCodeExecutionActivity)
								{
									imageList = TraceViewerForm.GetImageFromImageList(Images.ExecutionActivityIcon, out index);
								}
								else if (executionColumnItem[i].CurrentActivity.ActivityType == ActivityType.ConnectionActivity)
								{
									imageList = TraceViewerForm.GetImageFromImageList(Images.ConnectionActivityIcon, out index);
								}
							}
							else
							{
								imageList = TraceViewerForm.GetImageFromImageList(Images.DefaultActivityIcon, out index);
							}
						}
						if (index != -1 && imageList != null)
						{
							label2.ImageList = imageList;
							label2.ImageIndex = index;
							label2.ImageAlign = ContentAlignment.MiddleCenter;
						}
						else
						{
							label2.Text = SR.GetString("SL_ATitle");
						}
						label2.BackColor = Color.Transparent;
						label2.DoubleClick += lblActivity_DoubleClick;
						label2.Tag = currentActivity;
						label2.Font = GetDefaultFont(currentScale);
						label2.BorderStyle = BorderStyle.None;
						label2.TextAlign = ContentAlignment.TopCenter;
						label2.Location = new Point(num2, GetDefaultVSize(currentScale));
						label2.Size = new Size(TraceRecordCellControl.GetControlSize(currentScale).Width, GetDefaultVSize(currentScale));
						SetupContextMenuForActivityTitle(label2, executionColumnItem[i], analyzer, currentScale);
						mainPanel.Controls.Add(label2);
						num2 += TraceRecordCellControl.GetControlSize(currentScale).Width + ExecutionCellControl.GetDefaultBlock(currentScale);
					}
					num2 -= ExecutionCellControl.GetDefaultBlock(currentScale) - ExecutionCellControl.GetDefaultBlank(currentScale);
					num2 += HorzBundRowControl.GetDefaultBlock(currentScale) + ExecutionCellControl.GetDefaultBlank(currentScale);
				}
				if (analyzer.ExecutionColumnItems.Count > 1)
				{
					num3 += HorzBundRowControl.GetDefaultBlock(currentScale) * (analyzer.ExecutionColumnItems.Count - 1);
				}
				lblDate.Font = new Font(WindowlessControlBase.CreateFont(HorzBundRowControl.GetFontSize(currentScale)), FontStyle.Bold);
				lblDate.Width = HorzBundRowControl.GetTimeBoxSize(currentScale).Width;
				lblDate.Height = GetDefaultHSize(currentScale);
				lblDate.Location = new Point(0, 10);
				container.RegisterExtentionEventListener(OnWindowlessControlExtentionEvent);
				base.Size = new Size(num3, GetDefaultHSize(currentScale));
			}
		}

		private void OnWindowlessControlExtentionEvent(object o, WindowlessControlEventArgs e)
		{
			if (o != null && e != null && e.EventType == WindowlessControlEventType.ObjectClick && o is HorzBundRowControl)
			{
				HorzBundRowControl horzBundRowControl = (HorzBundRowControl)o;
				lblDate.Text = horzBundRowControl.CurrentRowItem.Date.ToLongDateString();
			}
		}

		private void SetupContextMenuForActivityTitle(Label activityLabel, ActivityColumnItem activityColumnItem, ActivityTraceModeAnalyzer analyzer, WindowlessControlScale scale)
		{
			if (activityLabel != null && activityColumnItem != null)
			{
				PictureBox pictureBox = CreatePictureBox(activityColumnItem, analyzer, scale);
				if (pictureBox != null)
				{
					Point location = activityLabel.Location;
					location.Offset(-pictureBox.Width - 1, (activityLabel.Height - pictureBox.Height) / 2);
					pictureBox.Location = location;
					mainPanel.Controls.Add(pictureBox);
					if (!activityExpandableControls.ContainsKey(activityColumnItem.CurrentActivity.Id))
					{
						activityExpandableControls.Add(activityColumnItem.CurrentActivity.Id, pictureBox);
					}
				}
			}
		}

		private void lblActivity_DoubleClick(object sender, EventArgs e)
		{
			if (((Label)sender).Tag != null && ((Label)sender).Tag is Activity)
			{
				container.AnalysisActivityInTraceMode((Activity)((Label)sender).Tag);
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
			mainPanel = new System.Windows.Forms.Panel();
			lblDate = new System.Windows.Forms.Label();
			toolTip = new System.Windows.Forms.ToolTip(components);
			SuspendLayout();
			lblDate.BackColor = System.Drawing.Color.White;
			lblDate.Location = new System.Drawing.Point(0, 0);
			lblDate.AutoSize = false;
			mainPanel.Controls.Add(lblDate);
			mainPanel.BackColor = System.Drawing.Color.White;
			mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			mainPanel.Location = new System.Drawing.Point(0, 0);
			mainPanel.Name = "mainPanel";
			mainPanel.Size = new System.Drawing.Size(355, 29);
			toolTip.AutomaticDelay = 10;
			toolTip.AutoPopDelay = 10000;
			toolTip.InitialDelay = 10;
			toolTip.ReshowDelay = 2;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(mainPanel);
			base.Name = "HorzBundTitleControl";
			base.Size = new System.Drawing.Size(355, 29);
			ResumeLayout(performLayout: false);
		}
	}
}
