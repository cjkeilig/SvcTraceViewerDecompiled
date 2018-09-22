using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceRecordCellControl : WindowlessControlBaseExt
	{
		public const int TraceRecordItemCellZOrder = 2;

		private bool isMouseOver;

		private bool isToChildActivity;

		private static Color errorTransferBackColor = Color.Red;

		private static Color warningTransferBackColor = Color.Yellow;

		private static Color defaultActivityTraceColor = Utilities.GetColor(ApplicationColors.DefaultActivityInner);

		private static Color activeActivityTraceColor = Utilities.GetColor(ApplicationColors.ActiveActivityInner);

		private static Color defaultBackColor = Utilities.GetColor(ApplicationColors.DefaultActivityBack);

		private static Color activeBackColor = Utilities.GetColor(ApplicationColors.ActiveActivityBack);

		private static Color defaultTraceBorderColor = Utilities.GetColor(ApplicationColors.DefaultActivityBorder);

		private static Color activeTraceBorderColor = Utilities.GetColor(ApplicationColors.ActiveActivityBorder);

		private static Color highlightedTraceColor = Utilities.GetColor(ApplicationColors.HighlightingBack2);

		private static Color highlightedBorderColor = Utilities.GetColor(ApplicationColors.HighlightingBorder);

		private static Color mouseOverTraceColor = Utilities.GetColor(ApplicationColors.MouseOver);

		private static Image errorTraceImage = TempFileManager.GetImageFromEmbededResources(Images.ErrorTrace, Color.Black, isMakeTransparent: true);

		private static Image wariningTraceImage = TempFileManager.GetImageFromEmbededResources(Images.WarningTrace, Color.Black, isMakeTransparent: true);

		private static Image transferOutTraceImage = TempFileManager.GetImageFromEmbededResources(Images.TransferOut, Color.Black, isMakeTransparent: true);

		private static Image transferInTraceImage = TempFileManager.GetImageFromEmbededResources(Images.TransferIn, Color.Black, isMakeTransparent: true);

		private static Image messageTraceImage = TempFileManager.GetImageFromEmbededResources(Images.Message, Color.Black, isMakeTransparent: true);

		private static Image messageSentTraceImage = TempFileManager.GetImageFromEmbededResources(Images.MessageSentTrace, Color.Black, isMakeTransparent: true);

		private static Image messageReceivedTraceImage = TempFileManager.GetImageFromEmbededResources(Images.MessageReceiveTrace, Color.Black, isMakeTransparent: true);

		private TraceRecordCellItem currentTraceRecordItem;

		private ActivityColumnItem currentActivityColumnItem;

		private ExecutionCellControl executionCellCtrl;

		private List<WindowlessControlBase> relatedMessageCtrls = new List<WindowlessControlBase>();

		internal List<WindowlessControlBase> RelatedMessageControls => relatedMessageCtrls;

		internal ExecutionCellControl ParentExecutionCellControl => executionCellCtrl;

		internal TraceRecordCellItem CurrentTraceRecordItem => currentTraceRecordItem;

		internal ActivityColumnItem CurrentActivityColumnItem => currentActivityColumnItem;

		public ExpandingState ExpandingState
		{
			get
			{
				if (CurrentTraceRecordItem != null && CurrentTraceRecordItem.CurrentTraceRecord != null && CurrentTraceRecordItem.CurrentTraceRecord.IsTransfer && isToChildActivity)
				{
					if (CurrentTraceRecordItem.IsParentTransferTrace)
					{
						return ExpandingState.Expanded;
					}
					return ExpandingState.Collapsed;
				}
				return ExpandingState.Unexpandable;
			}
		}

		internal static int GetActivityColumnBlockWidth(WindowlessControlScale scale, int delta)
		{
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return delta * ExecutionCellControl.GetDefaultBlock(scale) + (delta - 1) * GetControlSize(scale).Width + 10;
			case WindowlessControlScale.Small:
				return delta * ExecutionCellControl.GetDefaultBlock(scale) + (delta - 1) * GetControlSize(scale).Width + 6;
			case WindowlessControlScale.XSmall:
				return delta * ExecutionCellControl.GetDefaultBlock(scale) + (delta - 1) * GetControlSize(scale).Width + 4;
			default:
				return 0;
			}
		}

		internal static Size GetControlSize(WindowlessControlScale scale)
		{
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return new Size(26, 18);
			case WindowlessControlScale.Small:
				return new Size(20, 14);
			case WindowlessControlScale.XSmall:
				return new Size(14, 10);
			default:
				return new Size(0, 0);
			}
		}

		public TraceRecordCellControl(TraceRecordCellItem traceItem, IWindowlessControlContainer parentContainer, Point location, ActivityColumnItem activityColumnItem, ExecutionCellControl executionCellCtrl, IErrorReport errorReport)
			: base(2, parentContainer.GetCurrentScale(), parentContainer, location, errorReport)
		{
			currentTraceRecordItem = traceItem;
			this.executionCellCtrl = executionCellCtrl;
			currentActivityColumnItem = activityColumnItem;
			isToChildActivity = (CurrentTraceRecordItem != null && CurrentTraceRecordItem.IsToChildTransferTrace);
			if (activityColumnItem.IsActiveActivity)
			{
				base.BackColor = activeBackColor;
			}
			else if (activityColumnItem.PairedActivityIndex == 0)
			{
				base.BackColor = defaultBackColor;
			}
			else
			{
				base.BackColor = base.Container.GetRandColorByIndex(activityColumnItem.PairedActivityIndex);
			}
			base.Size = GetControlSize(base.Scale);
			switch (base.Scale)
			{
			case WindowlessControlScale.Normal:
				base.FontSize = 8f;
				break;
			case WindowlessControlScale.Small:
				base.FontSize = 6f;
				break;
			case WindowlessControlScale.XSmall:
				base.FontSize = 3f;
				break;
			}
			if (currentTraceRecordItem != null)
			{
				currentTraceRecordItem.RelatedActivityItem.IncrementDrawnTraceRecordItemCount();
			}
			if (isToChildActivity && CurrentTraceRecordItem.CurrentTraceRecord != null && CurrentTraceRecordItem.CurrentTraceRecord.IsTransfer && CurrentTraceRecordItem.CurrentTraceRecord.ActivityID != CurrentTraceRecordItem.CurrentTraceRecord.RelatedActivityID)
			{
				TraceTransferExpandableIconControl item = new TraceTransferExpandableIconControl(base.Container, this, base.ErrorReport);
				base.ChildControls.Add(item);
			}
		}

		public void PerformClick()
		{
			Highlight(isHighlight: true);
			base.Container.HighlightSelectedTraceRecordRow(this);
		}

		internal void PerformExpanding()
		{
			if (isToChildActivity && CurrentTraceRecordItem.CurrentTraceRecord != null && CurrentTraceRecordItem.CurrentTraceRecord.IsTransfer && CurrentTraceRecordItem.CurrentTraceRecord.DataSource != null && CurrentTraceRecordItem.CurrentTraceRecord.DataSource.Activities.ContainsKey(CurrentTraceRecordItem.CurrentTraceRecord.RelatedActivityID))
			{
				ActivityTraceModeAnalyzerParameters parameters = CurrentTraceRecordItem.Analyzer.Parameters;
				parameters = ((parameters != null) ? new ActivityTraceModeAnalyzerParameters(parameters) : new ActivityTraceModeAnalyzerParameters());
				try
				{
					parameters.AppendExpandingTransfer(CurrentTraceRecordItem.CurrentTraceRecord, ExpandingLevel.ExpandAll);
				}
				catch (TraceViewerException ex)
				{
					base.ErrorReport.ReportErrorToUser(SR.GetString("SL_FailExpandTransfer") + ex.Message);
				}
				base.Container.AnalysisActivityInTraceMode(CurrentActivityColumnItem.Analyzer.ActiveActivity, CurrentTraceRecordItem.CurrentTraceRecord, parameters);
			}
		}

		internal void PerformCollapse()
		{
			if (isToChildActivity && CurrentTraceRecordItem.CurrentTraceRecord != null && CurrentTraceRecordItem.CurrentTraceRecord.IsTransfer && CurrentTraceRecordItem.CurrentTraceRecord.DataSource != null && CurrentTraceRecordItem.CurrentTraceRecord.DataSource.Activities.ContainsKey(CurrentTraceRecordItem.CurrentTraceRecord.RelatedActivityID))
			{
				ActivityTraceModeAnalyzerParameters parameters = CurrentTraceRecordItem.Analyzer.Parameters;
				parameters = ((parameters != null) ? new ActivityTraceModeAnalyzerParameters(parameters) : new ActivityTraceModeAnalyzerParameters());
				try
				{
					parameters.AppendCollapsingTransfer(CurrentTraceRecordItem.CurrentTraceRecord);
				}
				catch (TraceViewerException ex)
				{
					base.ErrorReport.ReportErrorToUser(SR.GetString("SL_FailCollapseTransfer") + ex.Message);
				}
				base.Container.AnalysisActivityInTraceMode(CurrentActivityColumnItem.Analyzer.ActiveActivity, CurrentTraceRecordItem.CurrentTraceRecord, parameters);
			}
		}

		public override bool OnClick(Point point)
		{
			PerformClick();
			return true;
		}

		private static int GetInnerBoxEdge(WindowlessControlScale scale, int width)
		{
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return width - 3;
			case WindowlessControlScale.Small:
				return width - 2;
			case WindowlessControlScale.XSmall:
				return width - 2;
			default:
				return width - 1;
			}
		}

		public override void OnPaint(Graphics graphics)
		{
			base.OnPaint(graphics);
			if (currentTraceRecordItem != null)
			{
				int innerBoxEdge = GetInnerBoxEdge(base.Container.GetCurrentScale(), base.Size.Height);
				int num = (base.Size.Width - base.Size.Height) / 2 + 1;
				Point point = new Point(base.Location.X + num, base.Location.Y + 1);
				bool flag = true;
				if (isToChildActivity && currentTraceRecordItem.SeverityLevel != 0)
				{
					Color color = defaultBackColor;
					switch (currentTraceRecordItem.SeverityLevel)
					{
					case TraceRecordSetSeverityLevel.Error:
						color = errorTransferBackColor;
						break;
					case TraceRecordSetSeverityLevel.Warning:
						color = warningTransferBackColor;
						break;
					}
					Rectangle rect = new Rectangle(base.Location, base.Size);
					rect.Inflate(-1, -1);
					graphics.FillRectangle(WindowlessControlBase.CreateSolidBrush(color), rect);
				}
				if (base.IsHighlighted && currentTraceRecordItem.SeverityLevel == TraceRecordSetSeverityLevel.Normal)
				{
					Rectangle rect2 = new Rectangle(base.Location, base.Size);
					rect2.Inflate(0, -1);
					graphics.FillRectangle(WindowlessControlBase.CreateSolidBrush(highlightedTraceColor), rect2);
				}
				if (isMouseOver)
				{
					graphics.FillRectangle(WindowlessControlBase.CreateSolidBrush(mouseOverTraceColor), new Rectangle(base.Location, base.Size));
					flag = false;
				}
				if (currentTraceRecordItem.CurrentTraceRecord.Level == TraceEventType.Error || currentTraceRecordItem.CurrentTraceRecord.Level == TraceEventType.Critical)
				{
					Image image = errorTraceImage;
					Point location = point;
					int num2 = innerBoxEdge;
					graphics.DrawImage(image, new Rectangle(location, new Size(num2, num2)));
					flag = false;
				}
				else if (currentTraceRecordItem.CurrentTraceRecord.Level == TraceEventType.Warning || (isToChildActivity && currentTraceRecordItem.SeverityLevel == TraceRecordSetSeverityLevel.Warning))
				{
					Image image2 = wariningTraceImage;
					Point location2 = point;
					int num3 = innerBoxEdge;
					graphics.DrawImage(image2, new Rectangle(location2, new Size(num3, num3)));
					flag = false;
				}
				else if (currentTraceRecordItem.CurrentTraceRecord.IsTransfer)
				{
					if (currentTraceRecordItem.CurrentTraceRecord.ActivityID == CurrentActivityColumnItem.CurrentActivity.Id)
					{
						Image image3 = transferOutTraceImage;
						Point location3 = point;
						int num4 = innerBoxEdge;
						graphics.DrawImage(image3, new Rectangle(location3, new Size(num4, num4)));
					}
					else
					{
						Image image4 = transferInTraceImage;
						Point location4 = point;
						int num5 = innerBoxEdge;
						graphics.DrawImage(image4, new Rectangle(location4, new Size(num5, num5)));
					}
					flag = false;
				}
				else if (currentTraceRecordItem.CurrentTraceRecord.IsMessageLogged)
				{
					Image image5 = messageTraceImage;
					Point location5 = point;
					int num6 = innerBoxEdge;
					graphics.DrawImage(image5, new Rectangle(location5, new Size(num6, num6)));
					flag = false;
				}
				else if (currentTraceRecordItem.CurrentTraceRecord.IsMessageSentRecord)
				{
					Image image6 = messageSentTraceImage;
					Point location6 = point;
					int num7 = innerBoxEdge;
					graphics.DrawImage(image6, new Rectangle(location6, new Size(num7, num7)));
					flag = false;
				}
				else if (currentTraceRecordItem.CurrentTraceRecord.IsMessageReceivedRecord)
				{
					Image image7 = messageReceivedTraceImage;
					Point location7 = point;
					int num8 = innerBoxEdge;
					graphics.DrawImage(image7, new Rectangle(location7, new Size(num8, num8)));
					flag = false;
				}
				if (flag)
				{
					if (currentTraceRecordItem.RelatedActivityItem.IsActiveActivity)
					{
						Brush brush = WindowlessControlBase.CreateSolidBrush(activeActivityTraceColor);
						Point location8 = point;
						int num9 = innerBoxEdge;
						graphics.FillRectangle(brush, new Rectangle(location8, new Size(num9, num9)));
						Pen pen = WindowlessControlBase.CreatePen(activeTraceBorderColor);
						Point location9 = point;
						int num10 = innerBoxEdge;
						graphics.DrawRectangle(pen, new Rectangle(location9, new Size(num10, num10)));
					}
					else
					{
						Brush brush2 = WindowlessControlBase.CreateSolidBrush(defaultActivityTraceColor);
						Point location10 = point;
						int num11 = innerBoxEdge;
						graphics.FillRectangle(brush2, new Rectangle(location10, new Size(num11, num11)));
						Pen pen2 = WindowlessControlBase.CreatePen(defaultTraceBorderColor);
						Point location11 = point;
						int num12 = innerBoxEdge;
						graphics.DrawRectangle(pen2, new Rectangle(location11, new Size(num12, num12)));
					}
				}
				if (base.IsHighlighted && currentTraceRecordItem.SeverityLevel == TraceRecordSetSeverityLevel.Normal)
				{
					Rectangle rectangle = new Rectangle(base.Location, base.Size);
					rectangle.Inflate(0, -1);
					Point location12 = rectangle.Location;
					Point location13 = rectangle.Location;
					location13.Offset(rectangle.Width, 0);
					Point location14 = rectangle.Location;
					location14.Offset(0, rectangle.Height);
					Point location15 = rectangle.Location;
					location15.Offset(rectangle.Width, rectangle.Height);
					graphics.DrawLine(WindowlessControlBase.CreatePen(highlightedBorderColor), location12, location13);
					graphics.DrawLine(WindowlessControlBase.CreatePen(highlightedBorderColor), location14, location15);
				}
			}
		}

		public override bool IsFindingControl(object o)
		{
			if (o != null && o is TraceRecordCellItem && CurrentTraceRecordItem != null)
			{
				if (o == CurrentTraceRecordItem)
				{
					return true;
				}
				if (((TraceRecordCellItem)o).CurrentTraceRecord.TraceID == CurrentTraceRecordItem.CurrentTraceRecord.TraceID)
				{
					return true;
				}
			}
			return false;
		}

		public override WindowlessControlMessage OnMouseEnterExt(Control parentControl, int x, int y)
		{
			if (RelatedMessageControls.Count != 0)
			{
				MouseOverMessageExchangeMessage result = new MouseOverMessageExchangeMessage(this, RelatedMessageControls);
				isMouseOver = true;
				Invalidate();
				return result;
			}
			return null;
		}

		public override WindowlessControlMessage OnMouseLeaveExt()
		{
			if (RelatedMessageControls.Count != 0)
			{
				MouseOverMessageExchangeMessage result = new MouseOverMessageExchangeMessage(this);
				isMouseOver = false;
				Invalidate();
				return result;
			}
			return null;
		}

		public override void OnWindowlessControlMessageReceived(WindowlessControlMessage message)
		{
			if (message != null && message is MouseOverMessageExchangeMessage)
			{
				MouseOverMessageExchangeMessage mouseOverMessageExchangeMessage = (MouseOverMessageExchangeMessage)message;
				if (mouseOverMessageExchangeMessage.IsReverting && isMouseOver)
				{
					isMouseOver = false;
					Invalidate();
				}
				else if (!mouseOverMessageExchangeMessage.IsReverting && mouseOverMessageExchangeMessage.RelatedControls.Contains(this))
				{
					isMouseOver = true;
					Invalidate();
				}
			}
		}
	}
}
