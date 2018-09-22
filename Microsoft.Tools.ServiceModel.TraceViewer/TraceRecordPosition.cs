using System;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceRecordPosition
	{
		private FileDescriptor fileDesp;

		private long fileOffset;

		private DateTime dateTime;

		private int typePositionPriority;

		public int TypePositionPriority => typePositionPriority;

		public DateTime TraceRecordDateTime => dateTime;

		public FileDescriptor RelatedFileDescriptor => fileDesp;

		public long FileOffset => fileOffset;

		public TraceRecordPosition(FileDescriptor fileDesp, long fileOffset, DateTime dateTime, int typePositionPriority)
		{
			if (fileDesp == null)
			{
				throw new ArgumentNullException();
			}
			this.fileDesp = fileDesp;
			this.fileOffset = fileOffset;
			this.dateTime = dateTime;
			this.typePositionPriority = typePositionPriority;
		}
	}
}
