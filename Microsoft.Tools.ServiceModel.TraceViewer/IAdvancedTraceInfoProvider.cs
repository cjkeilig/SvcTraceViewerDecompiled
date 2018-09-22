using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal interface IAdvancedTraceInfoProvider
	{
		bool CanSupport(TraceRecord trace);

		Control GetAdvancedTraceInfoControl();

		void ReloadTrace(TraceRecord trace, TraceDetailInfoControlParam param);
	}
}
