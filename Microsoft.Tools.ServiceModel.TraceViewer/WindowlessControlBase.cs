using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class WindowlessControlBase : IDisposable
	{
		private static Dictionary<Color, Brush> solidBrushCache = new Dictionary<Color, Brush>();

		private static Dictionary<float, Pen> penCache = new Dictionary<float, Pen>();

		private static Dictionary<float, Font> fontCache = new Dictionary<float, Font>();

		private const float DEFAULT_FONT_SIZE = 1f;

		private bool isDisposed;

		private Point currentLocation;

		private Size currentSize;

		private WindowlessControlScale currentScale;

		private int zOrder;

		private Color backColor = Color.White;

		private Color foreColor = Color.Black;

		private float fontSize = 8f;

		private List<WindowlessControlBase> childControls = new List<WindowlessControlBase>();

		private IWindowlessControlContainer container;

		private bool isHighlighted;

		private IErrorReport errorReport;

		public bool IsDisposed
		{
			get
			{
				return isDisposed;
			}
			set
			{
				isDisposed = value;
			}
		}

		internal IErrorReport ErrorReport => errorReport;

		public bool IsHighlighted => isHighlighted;

		internal List<WindowlessControlBase> ChildControls => childControls;

		protected IWindowlessControlContainer Container => container;

		internal WindowlessControlScale Scale
		{
			get
			{
				return currentScale;
			}
			set
			{
				currentScale = value;
			}
		}

		public int ZOrder
		{
			get
			{
				return zOrder;
			}
			set
			{
				zOrder = value;
			}
		}

		public Color BackColor
		{
			get
			{
				return backColor;
			}
			set
			{
				backColor = value;
			}
		}

		public Color ForeColor
		{
			get
			{
				return foreColor;
			}
			set
			{
				foreColor = value;
			}
		}

		public float FontSize
		{
			get
			{
				return fontSize;
			}
			set
			{
				fontSize = value;
			}
		}

		public Point Location
		{
			get
			{
				return currentLocation;
			}
			set
			{
				currentLocation = value;
			}
		}

		public Size Size
		{
			get
			{
				return currentSize;
			}
			set
			{
				currentSize = value;
			}
		}

		internal static Brush CreateSolidBrush(Color color)
		{
			if (!solidBrushCache.ContainsKey(color))
			{
				solidBrushCache.Add(color, new SolidBrush(color));
			}
			return solidBrushCache[color];
		}

		internal static Pen CreatePen(Color color)
		{
			return CreatePen(color, 1f);
		}

		internal static Pen CreatePen(Color color, float width)
		{
			float key = (float)color.GetHashCode() * width;
			if (!penCache.ContainsKey(key))
			{
				penCache.Add(key, new Pen(CreateSolidBrush(color), width));
			}
			return penCache[key];
		}

		internal static Font CreateFont(float fontSize)
		{
			if (!fontCache.ContainsKey(fontSize))
			{
				fontCache.Add(fontSize, new Font(FontFamily.GenericSansSerif, fontSize));
			}
			return fontCache[fontSize];
		}

		protected WindowlessControlBase(int zOrder, WindowlessControlScale scale, IWindowlessControlContainer container, Point location, IErrorReport errorReport)
		{
			ZOrder = zOrder;
			this.container = container;
			Scale = scale;
			Location = location;
			this.errorReport = errorReport;
			Container.RegisterWindowlessControl(this);
		}

		public virtual bool IntersectsWith(Point point)
		{
			return new Rectangle(Location, Size).Contains(point);
		}

		public virtual void Highlight(bool isHighlight)
		{
			if (isHighlight)
			{
				isHighlighted = true;
				Container.RegisterHighlightedControls(this);
			}
			else
			{
				isHighlighted = false;
			}
			Invalidate();
		}

		public virtual void OnPaint(Graphics graphics)
		{
			if (graphics != null)
			{
				Size size = Size;
				size.Height++;
				graphics.FillRectangle(CreateSolidBrush(BackColor), new Rectangle(Location, size));
			}
		}

		public virtual void Invalidate()
		{
			Container.InvalidateParent(new Rectangle(Location, Size));
		}

		public virtual bool IntersectsWith(Rectangle rect)
		{
			if (rect.IntersectsWith(new Rectangle(Location, Size)))
			{
				return true;
			}
			return false;
		}

		public virtual ContextMenuStrip GetContextMenu()
		{
			return null;
		}

		public virtual bool OnClick(Point point)
		{
			Invalidate();
			return false;
		}

		public virtual bool IsFindingControl(object o)
		{
			return false;
		}

		protected virtual void DisposeObject()
		{
		}

		public void Dispose()
		{
			if (!IsDisposed)
			{
				DisposeObject();
				IsDisposed = true;
				GC.SuppressFinalize(this);
			}
		}

		~WindowlessControlBase()
		{
			if (!IsDisposed)
			{
				DisposeObject();
			}
		}
	}
}
