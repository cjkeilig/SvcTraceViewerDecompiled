using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class MessageExchangeCellControl : WindowlessControlBaseExt
	{
		public const int MessageExchangeCellZOrder = 3;

		private const int EDGE_EXTENSION_SIZE = 2;

		private bool isMouseOver;

		private bool isInSameExecution;

		private static Color messageTransferColor = Utilities.GetColor(ApplicationColors.MessageTransfer);

		private static Color messageTransferMouseOverColor = Utilities.GetColor(ApplicationColors.MouseOver);

		private bool isToArrow = true;

		private List<TraceRecordCellItem> loggedMessageItems;

		private ExecutionColumnItem sendExecutionColumnItem;

		private ExecutionColumnItem receiveExecutionColumnItem;

		private int lineWidth;

		private int boundEmpty;

		private Point startPoint;

		private Point secondPoint;

		private Point thirdPoint;

		private Point endPoint;

		private Rectangle leftRect;

		private Rectangle rightRect;

		private List<WindowlessControlBase> relatedControls = new List<WindowlessControlBase>();

		private static int GetLineWidth(WindowlessControlScale scale)
		{
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return 4;
			case WindowlessControlScale.Small:
				return 3;
			case WindowlessControlScale.XSmall:
				return 2;
			default:
				return 0;
			}
		}

		private static int GetBoundEmpty(WindowlessControlScale scale)
		{
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return 5;
			case WindowlessControlScale.Small:
				return 4;
			case WindowlessControlScale.XSmall:
				return 3;
			default:
				return 0;
			}
		}

		private void InitializeRedrawData(TraceRecordCellControl sendCellCtrl, TraceRecordCellControl receiveCellCtrl)
		{
			boundEmpty = GetBoundEmpty(base.Container.GetCurrentScale());
			if (!isInSameExecution)
			{
				startPoint = ExecutionCellControl.GetCrossMessageExchangeConnectionPoint(sendCellCtrl.ParentExecutionCellControl, base.Container.GetCurrentScale(), !isToArrow);
				secondPoint = startPoint;
				secondPoint.Offset(isToArrow ? boundEmpty : (-boundEmpty), 0);
				endPoint = ExecutionCellControl.GetCrossMessageExchangeConnectionPoint(receiveCellCtrl.ParentExecutionCellControl, base.Container.GetCurrentScale(), isToArrow);
				thirdPoint = endPoint;
				thirdPoint.Offset(isToArrow ? (-boundEmpty) : boundEmpty, 0);
			}
			else
			{
				startPoint = ExecutionCellControl.GetCrossMessageExchangeConnectionPoint(sendCellCtrl.ParentExecutionCellControl, base.Container.GetCurrentScale(), isToArrow);
				secondPoint = startPoint;
				secondPoint.Offset(-HorzBundRowControl.GetDefaultBlock(base.Container.GetCurrentScale()) / 2, 0);
				endPoint = ExecutionCellControl.GetCrossMessageExchangeConnectionPoint(receiveCellCtrl.ParentExecutionCellControl, base.Container.GetCurrentScale(), isToArrow);
				thirdPoint = endPoint;
				thirdPoint.Offset(-HorzBundRowControl.GetDefaultBlock(base.Container.GetCurrentScale()) / 2, 0);
			}
			if (isToArrow)
			{
				startPoint.Offset(-(startPoint.X - sendCellCtrl.Location.X - sendCellCtrl.Size.Width), 0);
				endPoint.Offset(receiveCellCtrl.Location.X - endPoint.X, 0);
			}
			else
			{
				startPoint.Offset(sendCellCtrl.Location.X - startPoint.X, 0);
				endPoint.Offset(-(endPoint.X - receiveCellCtrl.Location.X - receiveCellCtrl.Size.Width), 0);
			}
			lineWidth = GetLineWidth(base.Container.GetCurrentScale());
			Point point = startPoint;
			Point point2 = secondPoint;
			Point point3 = thirdPoint;
			Point point4 = endPoint;
			point.Offset(0, -2);
			point2.Offset(0, -2);
			point3.Offset(0, -2);
			point4.Offset(0, -2);
			if (!isInSameExecution)
			{
				int num = Math.Abs(point2.X - point.X) + 2;
				int num2 = Math.Abs(point3.X - point4.X) + 2;
				leftRect = new Rectangle(isToArrow ? point : point4, new Size(isToArrow ? num : num2, lineWidth));
				rightRect = new Rectangle(isToArrow ? point3 : point2, new Size(isToArrow ? num2 : num, lineWidth));
			}
			else
			{
				leftRect = new Rectangle(point, new Size(Math.Abs(point2.X - point.X) + 2, lineWidth + 1));
				rightRect = new Rectangle(point4, new Size(Math.Abs(point4.X - point3.X) + 2, lineWidth + 1));
			}
			base.Location = new Point(Math.Min(startPoint.X, endPoint.X) - 8, Math.Min(startPoint.Y, endPoint.Y) - 8);
			Point point5 = new Point(Math.Max(startPoint.X, endPoint.X), Math.Max(startPoint.Y, endPoint.Y) + lineWidth);
			base.Size = new Size(point5.X - base.Location.X + 16, point5.Y - base.Location.Y + 16);
		}

		public override bool IntersectsWith(Rectangle rect)
		{
			if (rect.IntersectsWith(leftRect) || rect.IntersectsWith(rightRect))
			{
				return true;
			}
			Rectangle rect2 = new Rectangle(new Point(Math.Min(secondPoint.X, thirdPoint.X) - 2, Math.Min(secondPoint.Y, thirdPoint.Y) - 2), new Size(Math.Abs(secondPoint.X - thirdPoint.X) + 4, Math.Abs(secondPoint.Y - thirdPoint.Y) + 4));
			if (rect.IntersectsWith(rect2))
			{
				return true;
			}
			return false;
		}

		public MessageExchangeCellControl(IWindowlessControlContainer parentContainer, MessageExchangeCellItem item, IErrorReport errorReport)
			: base(3, parentContainer.GetCurrentScale(), parentContainer, new Point(0, 0), errorReport)
		{
			if (item != null && item.SentTraceRecordCellItem != null && item.ReceiveTraceRecordCellItem != null)
			{
				WindowlessControlBase windowlessControlBase = base.Container.FindWindowlessControl(item.SentTraceRecordCellItem);
				WindowlessControlBase windowlessControlBase2 = base.Container.FindWindowlessControl(item.ReceiveTraceRecordCellItem);
				if (windowlessControlBase != null && windowlessControlBase2 != null && windowlessControlBase is TraceRecordCellControl && windowlessControlBase2 is TraceRecordCellControl)
				{
					sendExecutionColumnItem = item.SentExecutionColumnItem;
					receiveExecutionColumnItem = item.ReceiveExecutionColumnItem;
					loggedMessageItems = item.RelatedMessageTraceCellItems;
					isToArrow = (sendExecutionColumnItem.ItemIndex < receiveExecutionColumnItem.ItemIndex);
					isInSameExecution = (sendExecutionColumnItem.ItemIndex == receiveExecutionColumnItem.ItemIndex);
					InitializeRedrawData((TraceRecordCellControl)windowlessControlBase, (TraceRecordCellControl)windowlessControlBase2);
					relatedControls.Add(windowlessControlBase);
					relatedControls.Add(windowlessControlBase2);
					if (loggedMessageItems != null && loggedMessageItems.Count != 0)
					{
						foreach (TraceRecordCellItem loggedMessageItem in loggedMessageItems)
						{
							WindowlessControlBase windowlessControlBase3 = base.Container.FindWindowlessControl(loggedMessageItem);
							if (windowlessControlBase3 != null && windowlessControlBase3 is TraceRecordCellControl)
							{
								relatedControls.Add(windowlessControlBase3);
							}
						}
					}
					relatedControls.Add(this);
					foreach (WindowlessControlBase relatedControl in relatedControls)
					{
						if (relatedControl is TraceRecordCellControl)
						{
							AddRelatedControlsForTraceRecord((TraceRecordCellControl)relatedControl, relatedControls);
						}
					}
				}
			}
		}

		private void AddRelatedControlsForTraceRecord(TraceRecordCellControl ctrl, List<WindowlessControlBase> relatedCtrls)
		{
			if (ctrl != null && relatedCtrls != null)
			{
				foreach (WindowlessControlBase relatedCtrl in relatedCtrls)
				{
					if (!ctrl.RelatedMessageControls.Contains(relatedCtrl))
					{
						ctrl.RelatedMessageControls.Add(relatedCtrl);
					}
				}
			}
		}

		public override bool IntersectsWith(Point point)
		{
			if (leftRect.Contains(point))
			{
				return true;
			}
			if (rightRect.Contains(point))
			{
				return true;
			}
			if (!isInSameExecution && point.X <= (isToArrow ? thirdPoint.X : secondPoint.X) && point.X >= (isToArrow ? secondPoint.X : thirdPoint.X))
			{
				Point point2 = new Point(isToArrow ? secondPoint.X : thirdPoint.X, isToArrow ? secondPoint.Y : (thirdPoint.Y + lineWidth));
				Point point3 = new Point(isToArrow ? thirdPoint.X : secondPoint.X, isToArrow ? thirdPoint.Y : (secondPoint.Y + lineWidth));
				if (point3.X - point2.X > 0)
				{
					double num = Math.Abs((double)(point3.Y - point2.Y) / (double)(point3.X - point2.X));
					double num2 = (double)((point3.Y > point2.Y) ? (point3.X - point.X) : (point.X - point2.X)) * num;
					double num3 = ((point3.Y > point2.Y) ? ((double)point3.Y - num2) : ((double)point2.Y - num2)) + 2.0;
					double num4 = num3 - (double)lineWidth - 4.0;
					if ((double)point.Y >= num4 && (double)point.Y <= num3)
					{
						return true;
					}
				}
			}
			else if (isInSameExecution)
			{
				Point location = (secondPoint.Y > thirdPoint.Y) ? thirdPoint : secondPoint;
				location.Offset(-2, 0);
				int height = Math.Abs(secondPoint.Y - thirdPoint.Y);
				if (new Rectangle(location, new Size(lineWidth, height)).Contains(point))
				{
					return true;
				}
			}
			return false;
		}

		public override bool OnClick(Point point)
		{
			return true;
		}

		public override void OnPaint(Graphics graphics)
		{
			if (graphics != null)
			{
				Pen pen;
				Pen pen2;
				if (isMouseOver)
				{
					pen = WindowlessControlBase.CreatePen(messageTransferMouseOverColor, (float)lineWidth);
					pen2 = WindowlessControlBase.CreatePen(messageTransferMouseOverColor);
				}
				else
				{
					pen = WindowlessControlBase.CreatePen(messageTransferColor, (float)lineWidth);
					pen2 = WindowlessControlBase.CreatePen(messageTransferColor);
				}
				Point point = endPoint;
				point.Offset(isToArrow ? (-lineWidth) : lineWidth, 0);
				graphics.DrawLines(pen, new Point[4]
				{
					startPoint,
					secondPoint,
					thirdPoint,
					point
				});
				Point point2 = point;
				int num = lineWidth;
				point2.Offset(0, -lineWidth);
				while (num > 0)
				{
					Point pt = point2;
					pt.Offset(0, num * 2);
					graphics.DrawLine(pen2, point2, pt);
					point2.Offset(isToArrow ? 1 : (-1), 1);
					num--;
				}
			}
		}

		public override void Invalidate()
		{
			Rectangle rect = leftRect;
			Rectangle rect2 = rightRect;
			if (isToArrow)
			{
				rect.Inflate(2, 2);
				rect2.Inflate(8, 8);
			}
			else
			{
				rect2.Inflate(2, 2);
				rect.Inflate(8, 8);
			}
			base.Container.InvalidateParent(rect);
			base.Container.InvalidateParent(rect2);
			Rectangle rect3 = new Rectangle(new Point(Math.Min(secondPoint.X, thirdPoint.X) - 2, Math.Min(secondPoint.Y, thirdPoint.Y) - 2), new Size(Math.Abs(secondPoint.X - thirdPoint.X) + 4, Math.Abs(secondPoint.Y - thirdPoint.Y) + 4));
			rect3.Inflate(2, 0);
			base.Container.InvalidateParent(rect3);
		}

		public override WindowlessControlMessage OnMouseEnterExt(Control parentControl, int x, int y)
		{
			isMouseOver = true;
			Invalidate();
			return new MouseOverMessageExchangeMessage(this, relatedControls);
		}

		public override WindowlessControlMessage OnMouseLeaveExt()
		{
			isMouseOver = false;
			Invalidate();
			return new MouseOverMessageExchangeMessage(this);
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
				else if (!mouseOverMessageExchangeMessage.IsReverting && mouseOverMessageExchangeMessage.RelatedControls.Contains(this) && !isMouseOver)
				{
					isMouseOver = true;
					Invalidate();
				}
			}
		}
	}
}
