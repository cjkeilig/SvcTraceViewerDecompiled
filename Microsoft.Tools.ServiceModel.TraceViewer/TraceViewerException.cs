using System;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceViewerException : ApplicationException
	{
		protected string debugMessage = string.Empty;

		public virtual string GetOutputMessage()
		{
			return Message;
		}

		public TraceViewerException()
		{
		}

		public TraceViewerException(string message)
			: base(message)
		{
		}

		public TraceViewerException(string message, string debugMessage)
			: base(message)
		{
			this.debugMessage = debugMessage;
		}

		public TraceViewerException(string message, Exception e)
			: base(message, e)
		{
		}
	}
}
