namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceDetailInfoControlParam
	{
		private bool showBasicsInfo;

		private bool showDiagInfo;

		public bool ShowDiagnosticsInfo => showDiagInfo;

		public bool ShowBasicInfo => showBasicsInfo;

		public TraceDetailInfoControlParam(bool showBasicsInfo, bool showDiagnosticsInfo)
		{
			this.showBasicsInfo = showBasicsInfo;
			showDiagInfo = showDiagnosticsInfo;
		}
	}
}
