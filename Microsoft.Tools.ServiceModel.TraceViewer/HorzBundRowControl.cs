using System.Drawing;
using System.Globalization;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class HorzBundRowControl : WindowlessControlBase
	{
		public const int HorzBundRowControlZOrder = 5;

		private static Color defaultBackColor = Color.White;

		private static Color highlightBackColor = Utilities.GetColor(ApplicationColors.HighlightingBack);

		private static Color highlightBorderColor = Utilities.GetColor(ApplicationColors.HighlightingBorder);

		private HorzBundRowItem currentRowItem;

		internal HorzBundRowItem CurrentRowItem => currentRowItem;

		public static Size GetTimeBoxSize(WindowlessControlScale scale)
		{
			int height = TraceRecordCellControl.GetControlSize(scale).Height;
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return new Size(100, height);
			case WindowlessControlScale.Small:
				return new Size(70, height);
			case WindowlessControlScale.XSmall:
				return new Size(40, height);
			default:
				return new Size(0, 0);
			}
		}

		public static int GetDefaultBlank(WindowlessControlScale scale)
		{
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return 10;
			case WindowlessControlScale.Small:
				return 8;
			case WindowlessControlScale.XSmall:
				return 6;
			default:
				return 0;
			}
		}

		public static int GetDefaultBlock(WindowlessControlScale scale)
		{
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return 30;
			case WindowlessControlScale.Small:
				return 25;
			case WindowlessControlScale.XSmall:
				return 20;
			default:
				return 0;
			}
		}

		public static float GetFontSize(WindowlessControlScale scale)
		{
			switch (scale)
			{
			case WindowlessControlScale.Normal:
				return 8f;
			case WindowlessControlScale.Small:
				return 5.5f;
			case WindowlessControlScale.XSmall:
				return 3f;
			default:
				return 8f;
			}
		}

		public HorzBundRowControl(ActivityTraceModeAnalyzer analyzer, HorzBundRowItem rowItem, IWindowlessControlContainer parentContainer, Point location, IErrorReport errorReport)
			: base(5, parentContainer.GetCurrentScale(), parentContainer, location, errorReport)
		{
			base.BackColor = defaultBackColor;
			currentRowItem = rowItem;
			int width = GetTimeBoxSize(base.Scale).Width;
			width += GetDefaultBlank(base.Scale);
			int num = 0;
			foreach (ExecutionColumnItem executionColumnItem in analyzer.ExecutionColumnItems)
			{
				ExecutionCellControl item = new ExecutionCellControl(analyzer, executionColumnItem, rowItem, base.Container, new Point(width, base.Location.Y), this, base.ErrorReport);
				base.ChildControls.Add(item);
				width += ExecutionCellControl.GetControlSize(base.Scale, executionColumnItem.ActivityColumnCount).Width + GetDefaultBlock(base.Scale);
				num++;
			}
			base.FontSize = GetFontSize(base.Scale);
			base.Size = new Size(width - GetDefaultBlock(base.Scale), GetTimeBoxSize(base.Scale).Height);
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

		public override bool OnClick(Point point)
		{
			Highlight(isHighlight: true);
			foreach (WindowlessControlBase childControl in base.ChildControls)
			{
				if (childControl is ExecutionCellControl)
				{
					((ExecutionCellControl)childControl).Highlight(isHighlight: true);
				}
			}
			return true;
		}

		public override void OnPaint(Graphics graphics)
		{
			if (graphics != null)
			{
				Size timeBoxSize = GetTimeBoxSize(base.Container.GetCurrentScale());
				if (base.IsHighlighted)
				{
					Rectangle rect = new Rectangle(base.Location, base.Size);
					rect.Inflate(-1, -1);
					graphics.FillRectangle(WindowlessControlBase.CreateSolidBrush(base.BackColor), rect);
					graphics.DrawRectangle(WindowlessControlBase.CreatePen(highlightBorderColor), rect);
				}
				else
				{
					base.OnPaint(graphics);
					graphics.FillRectangle(WindowlessControlBase.CreateSolidBrush(defaultBackColor), new Rectangle(base.Location, timeBoxSize));
				}
				string s = currentRowItem.Date.ToLongTimeString() + SR.GetString("SL_TimeMillSecondSep") + currentRowItem.Date.Millisecond.ToString(CultureInfo.CurrentUICulture);
				graphics.DrawString(s, WindowlessControlBase.CreateFont(base.FontSize), WindowlessControlBase.CreateSolidBrush(base.ForeColor), new PointF((float)(base.Location.X + 1), (float)(base.Location.Y + 1)));
			}
		}
	}
}
