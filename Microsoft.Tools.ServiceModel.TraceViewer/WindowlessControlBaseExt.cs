using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class WindowlessControlBaseExt : WindowlessControlBase
	{
		protected WindowlessControlBaseExt(int zOrder, WindowlessControlScale scale, IWindowlessControlContainer container, Point p, IErrorReport errorReport)
			: base(zOrder, scale, container, p, errorReport)
		{
		}

		public virtual WindowlessControlMessage OnMouseEnterExt(Control parentControl, int x, int y)
		{
			return null;
		}

		public virtual WindowlessControlMessage OnMouseLeaveExt()
		{
			return null;
		}

		public virtual void OnWindowlessControlMessageReceived(WindowlessControlMessage message)
		{
		}
	}
}
