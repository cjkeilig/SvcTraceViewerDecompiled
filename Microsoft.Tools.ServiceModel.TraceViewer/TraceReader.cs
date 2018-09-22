using System;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal abstract class TraceReader
	{
		protected TraceCallback processor;

		protected DateTime startTimeFilter = DateTime.MinValue;

		protected DateTime endTimeFilter = DateTime.MaxValue;

		protected bool filterTimeRange;

		protected bool cancelTraceProcessing;

		protected bool isTraceOpen;

		private string fileName;

		public string FileName
		{
			get
			{
				return fileName;
			}
			set
			{
				fileName = value;
			}
		}

		public TraceReader(TraceCallback callback, DateTime start, DateTime end)
		{
			processor = callback;
			startTimeFilter = start;
			endTimeFilter = end;
		}

		public abstract void GetTraces();

		public abstract void Close();
	}
}
