using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class ExecutionColumnItem
	{
		private int itemIndex;

		private ExecutionInfo currentExecutionInfo;

		private Activity activeActivity;

		private Dictionary<string, ActivityColumnItem> activityItems = new Dictionary<string, ActivityColumnItem>();

		private Dictionary<int, ActivityColumnItem> activityItemIndics = new Dictionary<int, ActivityColumnItem>();

		private Queue<TraceRecordCellItem> traceRecordCellItems = new Queue<TraceRecordCellItem>();

		private Dictionary<string, Activity> allActivities;

		private List<string> suppressedActivityIds;

		private ActivityTraceModeAnalyzer analyzer;

		internal ActivityTraceModeAnalyzer Analyzer => analyzer;

		public int ItemIndex
		{
			get
			{
				return itemIndex;
			}
			set
			{
				itemIndex = value;
			}
		}

		internal ExecutionInfo CurrentExecutionInfo => currentExecutionInfo;

		internal ActivityColumnItem this[string activityId]
		{
			get
			{
				if (!string.IsNullOrEmpty(activityId) && activityItems.ContainsKey(activityId))
				{
					return activityItems[activityId];
				}
				return null;
			}
		}

		public int ActivityColumnCount => activityItemIndics.Count;

		internal ActivityColumnItem this[int index]
		{
			get
			{
				if (activityItemIndics.Count != 0 && index >= 0 && index < activityItemIndics.Count)
				{
					return activityItemIndics[index];
				}
				return null;
			}
		}

		public ExecutionColumnItem(ExecutionInfo executionInfo, Activity activeActivity, Dictionary<string, Activity> allActivities, int index, List<string> suppressedActivityIds, ActivityTraceModeAnalyzer analyzer)
		{
			currentExecutionInfo = executionInfo;
			this.activeActivity = activeActivity;
			this.allActivities = allActivities;
			itemIndex = index;
			this.analyzer = analyzer;
			if (suppressedActivityIds != null && suppressedActivityIds.Count != 0)
			{
				this.suppressedActivityIds = suppressedActivityIds;
			}
		}

		public TraceRecordCellItem AppendTraceRecord(TraceRecord trace)
		{
			TraceRecordCellItem result = null;
			if (trace.Execution.ExecutionID == CurrentExecutionInfo.ExecutionID)
			{
				if (!trace.IsTransfer && allActivities.ContainsKey(trace.ActivityID) && (suppressedActivityIds == null || !suppressedActivityIds.Contains(trace.ActivityID)))
				{
					if (this[trace.ActivityID] == null)
					{
						activityItems.Add(trace.ActivityID, new ActivityColumnItem(allActivities[trace.ActivityID], this, activityItemIndics.Count, Analyzer));
						activityItemIndics.Add(this[trace.ActivityID].ItemIndex, this[trace.ActivityID]);
					}
					this[trace.ActivityID].AppendTraceRecord(trace);
					traceRecordCellItems.Enqueue(this[trace.ActivityID][trace.TraceID]);
					result = this[trace.ActivityID][trace.TraceID];
				}
				else if (trace.IsTransfer && allActivities.ContainsKey(trace.ActivityID) && allActivities.ContainsKey(trace.RelatedActivityID))
				{
					if (suppressedActivityIds == null || (!suppressedActivityIds.Contains(trace.ActivityID) && !suppressedActivityIds.Contains(trace.RelatedActivityID)))
					{
						if (this[trace.ActivityID] == null)
						{
							activityItems.Add(trace.ActivityID, new ActivityColumnItem(allActivities[trace.ActivityID], this, activityItemIndics.Count, Analyzer));
							activityItemIndics.Add(this[trace.ActivityID].ItemIndex, this[trace.ActivityID]);
						}
						this[trace.ActivityID].AppendTraceRecord(trace);
						traceRecordCellItems.Enqueue(this[trace.ActivityID][trace.TraceID]);
						result = this[trace.ActivityID][trace.TraceID];
						if (this[trace.RelatedActivityID] == null)
						{
							activityItems.Add(trace.RelatedActivityID, new ActivityColumnItem(allActivities[trace.RelatedActivityID], this, activityItemIndics.Count, Analyzer));
							activityItemIndics.Add(this[trace.RelatedActivityID].ItemIndex, this[trace.RelatedActivityID]);
						}
						this[trace.RelatedActivityID].AppendTraceRecord(trace);
						traceRecordCellItems.Enqueue(this[trace.RelatedActivityID][trace.TraceID]);
						this[trace.RelatedActivityID][trace.TraceID].RelatedTraceRecordCellItem = this[trace.ActivityID][trace.TraceID];
						this[trace.ActivityID][trace.TraceID].RelatedTraceRecordCellItem = this[trace.RelatedActivityID][trace.TraceID];
						this[trace.ActivityID][trace.TraceID].IsParentTransferTrace = true;
					}
					else if (suppressedActivityIds != null && suppressedActivityIds.Contains(trace.ActivityID) && !suppressedActivityIds.Contains(trace.RelatedActivityID))
					{
						if (this[trace.RelatedActivityID] == null)
						{
							activityItems.Add(trace.RelatedActivityID, new ActivityColumnItem(allActivities[trace.RelatedActivityID], this, activityItemIndics.Count, Analyzer));
							activityItemIndics.Add(this[trace.RelatedActivityID].ItemIndex, this[trace.RelatedActivityID]);
						}
						this[trace.RelatedActivityID].AppendTraceRecord(trace);
						traceRecordCellItems.Enqueue(this[trace.RelatedActivityID][trace.TraceID]);
						result = this[trace.RelatedActivityID][trace.TraceID];
					}
					else if (suppressedActivityIds != null && !suppressedActivityIds.Contains(trace.ActivityID) && suppressedActivityIds.Contains(trace.RelatedActivityID))
					{
						if (this[trace.ActivityID] == null)
						{
							activityItems.Add(trace.ActivityID, new ActivityColumnItem(allActivities[trace.ActivityID], this, activityItemIndics.Count, Analyzer));
							activityItemIndics.Add(this[trace.ActivityID].ItemIndex, this[trace.ActivityID]);
						}
						this[trace.ActivityID].AppendTraceRecord(trace);
						traceRecordCellItems.Enqueue(this[trace.ActivityID][trace.TraceID]);
						result = this[trace.ActivityID][trace.TraceID];
					}
				}
			}
			return result;
		}

		public TraceRecordCellItem GetTopTraceRecordCellItem()
		{
			if (traceRecordCellItems.Count != 0)
			{
				return traceRecordCellItems.Peek();
			}
			return null;
		}

		public void RemoveTopTraceRecordCellItem()
		{
			if (traceRecordCellItems.Count != 0)
			{
				traceRecordCellItems.Dequeue();
			}
		}
	}
}
