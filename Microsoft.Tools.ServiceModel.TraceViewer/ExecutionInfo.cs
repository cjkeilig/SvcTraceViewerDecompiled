namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class ExecutionInfo
	{
		private string processName;

		private string threadID;

		private int processID = -1;

		private string computerName;

		public string ProcessName => processName;

		public string ComputerName => computerName;

		public string ThreadID => threadID;

		public int ExecutionID => InternalGetHashCode(TraceViewerForm.IsThreadExecutionMode);

		public ExecutionInfo(string computer, string process, string thread, int processID)
		{
			computerName = ((!string.IsNullOrEmpty(computer)) ? computer : string.Empty);
			processName = ((!string.IsNullOrEmpty(process)) ? process : string.Empty);
			threadID = ((!string.IsNullOrEmpty(thread)) ? thread : "0");
			this.processID = processID;
		}

		public override string ToString()
		{
			return SR.GetString("EI_ToString1") + ((!string.IsNullOrEmpty(ComputerName)) ? ComputerName : string.Empty) + SR.GetString("EI_ToString2") + ((!string.IsNullOrEmpty(ProcessName)) ? ProcessName : string.Empty) + (TraceViewerForm.IsThreadExecutionMode ? (SR.GetString("EI_ToString3") + ThreadID) : string.Empty);
		}

		private int InternalGetHashCode(bool threadMode)
		{
			int num = 0;
			num = ((processID == -1) ? (ComputerName.GetHashCode() * ProcessName.GetHashCode()) : (ComputerName.GetHashCode() * processID));
			if (threadMode)
			{
				int result = 0;
				if (int.TryParse(threadID, out result) && result.GetHashCode() > 0)
				{
					num *= result.GetHashCode();
				}
			}
			return num;
		}

		public override int GetHashCode()
		{
			return InternalGetHashCode(threadMode: false);
		}
	}
}
