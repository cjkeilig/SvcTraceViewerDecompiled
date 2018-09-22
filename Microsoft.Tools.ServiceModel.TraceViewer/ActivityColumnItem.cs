using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class ActivityColumnItem
	{
		private int itemIndex;

		private int drawnTraceRecordItemCount;

		private int pairedActivityIndex;

		private Activity currentActivity;

		private ActivityTraceModeAnalyzer analyzer;

		private ExecutionColumnItem executionItem;

		private Dictionary<long, TraceRecordCellItem> traceRecordItems = new Dictionary<long, TraceRecordCellItem>();

		internal ActivityTraceModeAnalyzer Analyzer => analyzer;

		public int PairedActivityIndex
		{
			get
			{
				return pairedActivityIndex;
			}
			set
			{
				pairedActivityIndex = value;
			}
		}

		public bool WithinActivityBoundary
		{
			get
			{
				if (drawnTraceRecordItemCount > 0)
				{
					return drawnTraceRecordItemCount < traceRecordItems.Count;
				}
				return false;
			}
		}

		public bool IsActiveActivity => Analyzer.ActiveActivity.Id == currentActivity.Id;

		public int ItemIndex => itemIndex;

		internal Activity CurrentActivity => currentActivity;

		internal ExecutionColumnItem RelatedExecutionItem => executionItem;

		internal TraceRecordCellItem this[long traceID]
		{
			get
			{
				if (traceRecordItems.ContainsKey(traceID))
				{
					return traceRecordItems[traceID];
				}
				return null;
			}
		}

		public void IncrementDrawnTraceRecordItemCount()
		{
			drawnTraceRecordItemCount++;
		}

		public ActivityColumnItem(Activity activity, ExecutionColumnItem item, int index, ActivityTraceModeAnalyzer analyzer)
		{
			currentActivity = activity;
			executionItem = item;
			itemIndex = index;
			this.analyzer = analyzer;
		}

		public void AppendTraceRecord(TraceRecord trace)
		{
			if (this[trace.TraceID] == null)
			{
				traceRecordItems.Add(trace.TraceID, new TraceRecordCellItem(trace, this, Analyzer));
			}
		}
	}
}
