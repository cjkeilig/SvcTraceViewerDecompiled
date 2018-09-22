using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceDetailBasicInfoPart : ExpandablePart
	{
		private BasicTraceInfoControl basicInfoControl;

		protected override string ExpandablePartName => SR.GetString("FV_Basic_Title");

		public TraceDetailBasicInfoPart(ExpandablePartStateChanged callback)
		{
			basicInfoControl = new BasicTraceInfoControl();
			basicInfoControl.Dock = DockStyle.Fill;
			SetupRightPart(basicInfoControl, callback, basicInfoControl.Height + 30);
		}

		public override void ReloadTracePart(TraceDetailedProcessParameter parameter)
		{
			basicInfoControl.CleanUp();
			basicInfoControl.ReloadTrace(parameter.RelatedTraceRecord);
			UpdateUIElements();
		}
	}
}
