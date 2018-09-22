using System.Drawing;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class ActivityTransferCellControl : WindowlessControlBase
	{
		public const int ActivityTransferCellZOrder = 1;

		private static Color defaultBackColor = Color.White;

		private static Color defaultForeColor = Color.Black;

		private TraceRecordCellItem parentTraceItem;

		private bool isToArrow = true;

		private bool isLeft = true;

		public static Size GetControlSize(WindowlessControlScale scale, TraceRecordCellItem parentTraceItem)
		{
			if (parentTraceItem.RelatedTraceRecordCellItem != null)
			{
				int itemIndex = parentTraceItem.RelatedActivityItem.ItemIndex;
				int itemIndex2 = parentTraceItem.RelatedTraceRecordCellItem.RelatedActivityItem.ItemIndex;
				int num = (itemIndex > itemIndex2) ? (itemIndex - itemIndex2) : (itemIndex2 - itemIndex);
				int activityColumnBlockWidth = TraceRecordCellControl.GetActivityColumnBlockWidth(scale, num);
				if (num > 0)
				{
					switch (scale)
					{
					case WindowlessControlScale.Normal:
						return new Size(activityColumnBlockWidth, 5);
					case WindowlessControlScale.Small:
						return new Size(activityColumnBlockWidth, 3);
					case WindowlessControlScale.XSmall:
						return new Size(activityColumnBlockWidth, 3);
					}
				}
			}
			return new Size(0, 0);
		}

		public override bool IntersectsWith(Point point)
		{
			if (isToArrow)
			{
				return base.IntersectsWith(point);
			}
			Point location = new Point(base.Location.X - base.Size.Width, base.Location.Y);
			return new Rectangle(location, base.Size).Contains(point);
		}

		public override bool IntersectsWith(Rectangle rect)
		{
			if (base.Location.Y >= rect.Location.Y && base.Location.Y <= rect.Location.Y + rect.Height)
			{
				return true;
			}
			return base.IntersectsWith(rect);
		}

		public ActivityTransferCellControl(TraceRecordCellItem parentTraceItem, IWindowlessControlContainer parentContainer, Point location, IErrorReport errorReport)
			: base(1, parentContainer.GetCurrentScale(), parentContainer, location, errorReport)
		{
			this.parentTraceItem = parentTraceItem;
			base.BackColor = defaultBackColor;
			base.ForeColor = defaultForeColor;
			isToArrow = ((parentTraceItem.RelatedActivityItem.ItemIndex <= parentTraceItem.RelatedTraceRecordCellItem.RelatedActivityItem.ItemIndex) ? true : false);
			isLeft = ((parentTraceItem.RelatedActivityItem.ItemIndex >= parentTraceItem.RelatedTraceRecordCellItem.RelatedActivityItem.ItemIndex) ? true : false);
			base.Size = GetControlSize(base.Scale, parentTraceItem);
		}

		public override bool OnClick(Point point)
		{
			return true;
		}

		public override void OnPaint(Graphics graphics)
		{
			if (graphics != null)
			{
				Pen pen = WindowlessControlBase.CreatePen(base.ForeColor);
				Point location = base.Location;
				if (isLeft)
				{
					location.Offset(-base.Size.Width, 0);
				}
				graphics.FillRectangle(WindowlessControlBase.CreateSolidBrush(base.BackColor), new Rectangle(location, base.Size));
				Point pt = location;
				Point point = location;
				Point point2 = location;
				Point pt2 = location;
				Point point3 = location;
				Point pt3 = location;
				if (base.Container.GetCurrentScale() == WindowlessControlScale.Normal)
				{
					pt.Offset(new Point(0, 2));
					point.Offset(new Point(base.Size.Width, 2));
					point2.Offset(new Point(base.Size.Width - 3, 0));
					pt2.Offset(new Point(base.Size.Width - 3, base.Size.Height - 1));
					point3.Offset(new Point(3, 0));
					pt3.Offset(new Point(3, base.Size.Height - 1));
					graphics.DrawLine(pen, pt, point);
					if (isToArrow)
					{
						graphics.DrawLine(pen, point, point2);
						graphics.DrawLine(pen, point, pt2);
					}
					else
					{
						graphics.DrawLine(pen, pt, point3);
						graphics.DrawLine(pen, pt, pt3);
					}
				}
				else
				{
					pt.Offset(new Point(0, 1));
					point.Offset(new Point(base.Size.Width, 1));
					point2.Offset(new Point(base.Size.Width - 1, 0));
					pt2.Offset(new Point(base.Size.Width - 1, 2));
					point3.Offset(new Point(1, 0));
					pt3.Offset(new Point(1, 2));
					graphics.DrawLine(pen, pt, point);
					if (isToArrow)
					{
						graphics.DrawLine(pen, point2, pt2);
					}
					else
					{
						graphics.DrawLine(pen, point3, pt3);
					}
				}
			}
		}
	}
}
