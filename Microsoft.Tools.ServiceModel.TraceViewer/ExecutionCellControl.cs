using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class ExecutionCellControl : WindowlessControlBase
	{
		public const int ExecutionItemCellZOrder = 4;

		private List<IDisposable> disposedControls = new List<IDisposable>();

		private ContextMenuStrip contextMenuStrip;

		private static Color defaultBackColor = Utilities.GetColor(ApplicationColors.Info);

		private static Color highlightBackColor = Utilities.GetColor(ApplicationColors.HighlightingBack);

		private static Color highlightBorderColor = Utilities.GetColor(ApplicationColors.HighlightingBorder);

		private ExecutionColumnItem currentExecutionColumnItem;

		private HorzBundRowControl horzBundRowCtrl;

		internal HorzBundRowControl ParentHorzBundRowCtrl => horzBundRowCtrl;

		internal ExecutionColumnItem CurrentExecutionColumnItem => currentExecutionColumnItem;

		internal static int GetDefaultBlank(WindowlessControlScale scale)
		{
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return 30;
			case WindowlessControlScale.Small:
				return 24;
			case WindowlessControlScale.XSmall:
				return 18;
			default:
				return 0;
			}
		}

		internal static int GetDefaultBlock(WindowlessControlScale scale)
		{
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return 15;
			case WindowlessControlScale.Small:
				return 10;
			case WindowlessControlScale.XSmall:
				return 6;
			default:
				return 0;
			}
		}

		private static Point GetInterActivityConnectionPoint(WindowlessControlScale scale, bool isToLeft, Point basePoint)
		{
			Point p = new Point(0, 0);
			Point result = basePoint;
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				p = (isToLeft ? new Point(4, 6) : new Point(20, 6));
				break;
			case WindowlessControlScale.Small:
				p = (isToLeft ? new Point(3, 5) : new Point(18, 5));
				break;
			case WindowlessControlScale.XSmall:
				p = (isToLeft ? new Point(2, 3) : new Point(12, 4));
				break;
			}
			result.Offset(p);
			return result;
		}

		internal static Point GetCrossMessageExchangeConnectionPoint(ExecutionCellControl ctrl, WindowlessControlScale scale, bool isLeft)
		{
			if (ctrl != null)
			{
				Point location = ctrl.Location;
				switch (scale)
				{
				case WindowlessControlScale.Normal:
					if (isLeft)
					{
						location.Offset(0, 7);
					}
					else
					{
						location.Offset(ctrl.Size.Width, 7);
					}
					break;
				case WindowlessControlScale.Small:
					if (isLeft)
					{
						location.Offset(0, 4);
					}
					else
					{
						location.Offset(ctrl.Size.Width, 4);
					}
					break;
				case WindowlessControlScale.XSmall:
					if (isLeft)
					{
						location.Offset(0, 3);
					}
					else
					{
						location.Offset(ctrl.Size.Width, 3);
					}
					break;
				}
				return location;
			}
			return new Point(0, 0);
		}

		internal static Size GetControlSize(WindowlessControlScale scale, int activityCount)
		{
			int height = TraceRecordCellControl.GetControlSize(scale).Height;
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return new Size(GetDefaultBlank(scale) * 2 + (activityCount - 1) * GetDefaultBlock(scale) + activityCount * TraceRecordCellControl.GetControlSize(scale).Width, height);
			case WindowlessControlScale.Small:
				return new Size(GetDefaultBlank(scale) * 2 + (activityCount - 1) * GetDefaultBlock(scale) + activityCount * TraceRecordCellControl.GetControlSize(scale).Width, height);
			case WindowlessControlScale.XSmall:
				return new Size(GetDefaultBlank(scale) * 2 + (activityCount - 1) * GetDefaultBlock(scale) + activityCount * TraceRecordCellControl.GetControlSize(scale).Width, height);
			default:
				return new Size(0, 0);
			}
		}

		public override void Highlight(bool isHighlight)
		{
			if (isHighlight)
			{
				base.BackColor = highlightBackColor;
			}
			else
			{
				base.BackColor = defaultBackColor;
			}
			base.Highlight(isHighlight);
		}

		public override void OnPaint(Graphics graphics)
		{
			if (base.IsHighlighted)
			{
				Rectangle rect = new Rectangle(base.Location, base.Size);
				rect.Inflate(-1, -1);
				graphics.FillRectangle(WindowlessControlBase.CreateSolidBrush(base.BackColor), rect);
				Point location = rect.Location;
				Point location2 = rect.Location;
				location2.Offset(rect.Width, 0);
				Point location3 = rect.Location;
				location3.Offset(0, rect.Height);
				Point location4 = rect.Location;
				location4.Offset(rect.Width, rect.Height);
				graphics.DrawLine(WindowlessControlBase.CreatePen(highlightBorderColor), location, location2);
				graphics.DrawLine(WindowlessControlBase.CreatePen(highlightBorderColor), location3, location4);
			}
			else
			{
				base.OnPaint(graphics);
			}
		}

		public override bool OnClick(Point point)
		{
			return true;
		}

		public ExecutionCellControl(ActivityTraceModeAnalyzer analyzer, ExecutionColumnItem currentExecutionColumn, HorzBundRowItem rowItem, IWindowlessControlContainer parentContainer, Point location, HorzBundRowControl horzBundRowCtrl, IErrorReport errorReport)
			: base(4, parentContainer.GetCurrentScale(), parentContainer, location, errorReport)
		{
			int i = 0;
			int num = base.Location.X + GetDefaultBlank(base.Scale);
			Dictionary<int, TraceRecordCellItem> dictionary = new Dictionary<int, TraceRecordCellItem>();
			currentExecutionColumnItem = currentExecutionColumn;
			this.horzBundRowCtrl = horzBundRowCtrl;
			base.Size = GetControlSize(base.Scale, currentExecutionColumn.ActivityColumnCount);
			base.BackColor = defaultBackColor;
			foreach (TraceRecordCellItem traceRecordCellItem in rowItem.TraceRecordCellItems)
			{
				if (traceRecordCellItem.RelatedExecutionItem == currentExecutionColumn && !dictionary.ContainsKey(traceRecordCellItem.RelatedActivityItem.ItemIndex))
				{
					dictionary.Add(traceRecordCellItem.RelatedActivityItem.ItemIndex, traceRecordCellItem);
				}
			}
			for (; i < currentExecutionColumn.ActivityColumnCount; i++)
			{
				if (dictionary.ContainsKey(i))
				{
					TraceRecordCellControl traceRecordCellControl = new TraceRecordCellControl(dictionary[i], base.Container, new Point(num, base.Location.Y), currentExecutionColumn[i], this, base.ErrorReport);
					base.ChildControls.Add(traceRecordCellControl);
					if (dictionary[i].IsParentTransferTrace)
					{
						bool isToLeft = (dictionary[i].RelatedActivityItem.ItemIndex >= dictionary[i].RelatedTraceRecordCellItem.RelatedActivityItem.ItemIndex) ? true : false;
						Point interActivityConnectionPoint = GetInterActivityConnectionPoint(base.Container.GetCurrentScale(), isToLeft, traceRecordCellControl.Location);
						ActivityTransferCellControl item = new ActivityTransferCellControl(dictionary[i], base.Container, interActivityConnectionPoint, base.ErrorReport);
						base.ChildControls.Add(item);
					}
				}
				else if (currentExecutionColumn[i].WithinActivityBoundary)
				{
					TraceRecordCellControl item2 = new TraceRecordCellControl(null, base.Container, new Point(num, base.Location.Y), currentExecutionColumn[i], this, base.ErrorReport);
					base.ChildControls.Add(item2);
				}
				num += TraceRecordCellControl.GetControlSize(base.Scale).Width + GetDefaultBlock(base.Scale);
			}
			if (CurrentExecutionColumnItem.Analyzer.AllInvolvedExecutionItems.Count > 1)
			{
				contextMenuStrip = new ContextMenuStrip();
				ToolStripMenuItem value = new ToolStripMenuItem(SR.GetString("SL_HideProcess"));
				contextMenuStrip.Click += menuItem_Click;
				contextMenuStrip.Items.Add(value);
				disposedControls.Add(contextMenuStrip);
			}
		}

		public override ContextMenuStrip GetContextMenu()
		{
			if (CurrentExecutionColumnItem.Analyzer.AllInvolvedExecutionItems.Count > 1 && (CurrentExecutionColumnItem.Analyzer.Parameters == null || CurrentExecutionColumnItem.Analyzer.AllInvolvedExecutionItems.Count - CurrentExecutionColumnItem.Analyzer.Parameters.SuppressedExecutions.Count > 1))
			{
				return contextMenuStrip;
			}
			return null;
		}

		protected override void DisposeObject()
		{
			foreach (IDisposable disposedControl in disposedControls)
			{
				disposedControl.Dispose();
			}
		}

		private void menuItem_Click(object sender, EventArgs e)
		{
			ActivityTraceModeAnalyzerParameters parameters = CurrentExecutionColumnItem.Analyzer.Parameters;
			parameters = ((parameters != null) ? new ActivityTraceModeAnalyzerParameters(parameters) : new ActivityTraceModeAnalyzerParameters());
			parameters.AppendSuppressedExecution(CurrentExecutionColumnItem.CurrentExecutionInfo);
			base.Container.AnalysisActivityInTraceMode(CurrentExecutionColumnItem.Analyzer.ActiveActivity, null, parameters);
		}
	}
}
