using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class FileLoadingTaskInfo : TaskInfoBase
	{
		private List<FileDescriptor> fileDescriptors;

		private bool isEventTrigger = true;

		public bool IsEventTrigger
		{
			get
			{
				return isEventTrigger;
			}
			set
			{
				isEventTrigger = value;
			}
		}

		public int FileCount => fileDescriptors.Count;

		public long LoadingFileSize
		{
			get
			{
				long num = 0L;
				foreach (FileDescriptor fileDescriptor in fileDescriptors)
				{
					if (fileDescriptor != null)
					{
						num += fileDescriptor.SelectedBlockFileSize;
					}
				}
				return num;
			}
		}

		public List<FileDescriptor> LoadingFileDescriptors => fileDescriptors;

		public FileLoadingTaskInfo(List<FileDescriptor> fileDesps, bool persistErrors)
			: this(fileDesps, persistErrors, null)
		{
		}

		public FileLoadingTaskInfo(List<FileDescriptor> fileDesps, bool persistErrors, TaskFinished taskFinishedCallback)
			: base(persistErrors)
		{
			if (taskFinishedCallback != null)
			{
				SetTaskFinishedCallback(taskFinishedCallback);
			}
			fileDescriptors = fileDesps;
		}

		public FileLoadingTaskInfo(List<FileDescriptor> fileDesps)
			: this(fileDesps, persistErrors: true)
		{
		}
	}
}
