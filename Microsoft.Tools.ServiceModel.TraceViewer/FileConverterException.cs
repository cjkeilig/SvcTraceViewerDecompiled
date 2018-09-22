using System;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class FileConverterException : TraceViewerException
	{
		public FileConverterException(string sourceFilePath, string destFilePath, string message, Exception e)
			: base(message, e)
		{
		}
	}
}
