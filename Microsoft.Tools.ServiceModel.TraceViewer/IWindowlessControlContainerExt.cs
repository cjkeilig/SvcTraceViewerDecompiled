using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal interface IWindowlessControlContainerExt : IWindowlessControlContainer
	{
		void AnalysisActivityInHistory(Activity activity, GraphViewMode mode, object initData);

		ToolStrip GetToolStripExtension();

		void SetupTitleControl(Control titleControl);

		void SetupTitleSize(Size size);

		void SetupBodySize(Size size);

		void ClearView();

		WindowlessControlBase GetTopMostHighlighedControl();

		void SelectTraceRecordItem(TraceRecord trace, string activityId);

		Dictionary<int, LinkedList<WindowlessControlBase>> GetWindowlessControls();
	}
}
