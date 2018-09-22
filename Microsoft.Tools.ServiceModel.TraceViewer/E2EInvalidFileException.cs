using System;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class E2EInvalidFileException : TraceViewerException
	{
		private string filePath;

		private long fileOffset;

		public string FilePath => filePath;

		public long FileOffset => fileOffset;

		public E2EInvalidFileException(string message, string filePath, Exception e, long fileOffset)
			: base(message, e)
		{
			this.filePath = filePath;
			this.fileOffset = fileOffset;
		}
	}
}
