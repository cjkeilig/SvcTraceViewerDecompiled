using System.Drawing;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceTransferExpandableIconControl : WindowlessControlBaseExt
	{
		public const int TraceTransferExpandableIconZOrder = 0;

		private static Image plusImage = TempFileManager.GetImageFromEmbededResources(Images.PlusIcon, Color.Black, isMakeTransparent: true);

		private static Image minusImage = TempFileManager.GetImageFromEmbededResources(Images.MinusIcon, Color.Black, isMakeTransparent: true);

		private TraceRecordCellControl expandableCell;

		private static Size GetControlSize(WindowlessControlScale scale)
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

		public TraceTransferExpandableIconControl(IWindowlessControlContainer parentContainer, TraceRecordCellControl expandableCell, IErrorReport errorReport)
			: base(0, parentContainer.GetCurrentScale(), parentContainer, new Point(0, 0), errorReport)
		{
			base.Size = GetControlSize(parentContainer.GetCurrentScale());
			Point location = expandableCell.Location;
			location.Offset(-base.Size.Width - 1, 2);
			base.Location = location;
			this.expandableCell = expandableCell;
		}

		public override void OnPaint(Graphics graphics)
		{
			if (graphics != null && expandableCell != null)
			{
				if (expandableCell.ExpandingState == ExpandingState.Expanded)
				{
					graphics.DrawImage(minusImage, new Rectangle(base.Location, base.Size));
				}
				else if (expandableCell.ExpandingState == ExpandingState.Collapsed)
				{
					graphics.DrawImage(plusImage, new Rectangle(base.Location, base.Size));
				}
			}
		}

		public override bool OnClick(Point point)
		{
			if (expandableCell != null)
			{
				if (expandableCell.ExpandingState == ExpandingState.Expanded)
				{
					expandableCell.PerformCollapse();
				}
				else if (expandableCell.ExpandingState == ExpandingState.Collapsed)
				{
					expandableCell.PerformExpanding();
				}
			}
			return false;
		}
	}
}
