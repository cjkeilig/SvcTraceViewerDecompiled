using System;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class LogFileException : TraceViewerException
	{
		private string filePath;

		private string outputMessage;

		public string FilePath => filePath;

		public override string GetOutputMessage()
		{
			if (outputMessage == null)
			{
				return base.GetOutputMessage();
			}
			return outputMessage;
		}

		public LogFileException(string message, string filePath, Exception e)
			: base(message, e)
		{
			this.filePath = filePath;
			debugMessage = ((e == null) ? string.Empty : (e.GetType().ToString() + SR.GetString("MsgReturnBack") + e.ToString()));
		}
	}
}
