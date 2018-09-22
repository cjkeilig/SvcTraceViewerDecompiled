namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal interface IErrorReport
	{
		void ReportErrorToUser(string message);

		void ReportErrorToUser(string message, string debugMessage);

		void ReportErrorToUser(TraceViewerException exception);

		void LogError(string message);

		void LogError(string message, string debugMessage);

		void LogError(TraceViewerException exception);
	}
}
