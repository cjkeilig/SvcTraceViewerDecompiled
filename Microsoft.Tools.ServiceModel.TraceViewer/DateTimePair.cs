using System;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class DateTimePair
	{
		private DateTime startTime;

		private DateTime endTime;

		public DateTime StartTime => startTime;

		public DateTime EndTime => endTime;

		public DateTimePair(DateTime startTime, DateTime endTime)
		{
			this.startTime = startTime;
			this.endTime = endTime;
		}
	}
}
