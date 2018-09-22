using System;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class AppSettingsException : TraceViewerException
	{
		public AppSettingsException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
