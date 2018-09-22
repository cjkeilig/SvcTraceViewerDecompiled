using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class ActivityTraceModeAnalyzerParameters
	{
		private Dictionary<long, TraceRecord> expandingTransfers = new Dictionary<long, TraceRecord>();

		private Dictionary<long, ExpandingLevel> expandingTransferTraceLevel = new Dictionary<long, ExpandingLevel>();

		private Dictionary<long, TraceRecord> collapsingTransfers = new Dictionary<long, TraceRecord>();

		private Dictionary<string, ExpandingLevel> expandingActivities = new Dictionary<string, ExpandingLevel>();

		private Dictionary<int, ExecutionInfo> suppressedExecutions = new Dictionary<int, ExecutionInfo>();

		private LinkedList<int> executionItemOrder = new LinkedList<int>();

		internal Dictionary<int, ExecutionInfo> SuppressedExecutions => suppressedExecutions;

		internal Dictionary<long, ExpandingLevel> ExpandingTransferTraceLevels => expandingTransferTraceLevel;

		internal Dictionary<string, ExpandingLevel> ExpandingActivities => expandingActivities;

		internal Dictionary<long, TraceRecord> CollapsingTransfers => collapsingTransfers;

		internal Dictionary<long, TraceRecord> ExpandingTransfers => expandingTransfers;

		internal void PushExecutionColumnItem(ExecutionColumnItem item)
		{
			if (!executionItemOrder.Contains(item.CurrentExecutionInfo.ExecutionID))
			{
				executionItemOrder.AddLast(item.CurrentExecutionInfo.ExecutionID);
			}
		}

		internal void ReorderExecutionColumnItems(List<ExecutionColumnItem> items)
		{
			if (items != null)
			{
				Dictionary<int, ExecutionColumnItem> dictionary = new Dictionary<int, ExecutionColumnItem>();
				Queue<ExecutionColumnItem> queue = new Queue<ExecutionColumnItem>();
				foreach (ExecutionColumnItem item in items)
				{
					if (!dictionary.ContainsKey(item.CurrentExecutionInfo.ExecutionID))
					{
						dictionary.Add(item.CurrentExecutionInfo.ExecutionID, item);
					}
				}
				int num = 0;
				foreach (int item2 in executionItemOrder)
				{
					if (dictionary.ContainsKey(item2))
					{
						dictionary[item2].ItemIndex = num++;
						queue.Enqueue(dictionary[item2]);
						dictionary.Remove(item2);
					}
				}
				foreach (int key in dictionary.Keys)
				{
					PushExecutionColumnItem(dictionary[key]);
					dictionary[key].ItemIndex = num++;
					queue.Enqueue(dictionary[key]);
				}
				items.Clear();
				while (queue.Count != 0)
				{
					items.Add(queue.Dequeue());
				}
			}
		}

		internal void AppendSuppressedExecution(ExecutionInfo exec)
		{
			if (!suppressedExecutions.ContainsKey(exec.ExecutionID))
			{
				suppressedExecutions.Add(exec.ExecutionID, exec);
			}
		}

		internal void RemoveSuppressedExecution(ExecutionInfo exec)
		{
			if (suppressedExecutions.ContainsKey(exec.ExecutionID))
			{
				suppressedExecutions.Remove(exec.ExecutionID);
			}
		}

		internal ActivityTraceModeAnalyzerParameters()
		{
		}

		internal ActivityTraceModeAnalyzerParameters(ActivityTraceModeAnalyzerParameters param)
		{
			if (param != null)
			{
				foreach (long key in param.ExpandingTransfers.Keys)
				{
					expandingTransfers.Add(key, param.ExpandingTransfers[key]);
				}
				foreach (long key2 in param.ExpandingTransferTraceLevels.Keys)
				{
					expandingTransferTraceLevel.Add(key2, param.ExpandingTransferTraceLevels[key2]);
				}
				foreach (long key3 in param.CollapsingTransfers.Keys)
				{
					collapsingTransfers.Add(key3, param.CollapsingTransfers[key3]);
				}
				foreach (string key4 in param.ExpandingActivities.Keys)
				{
					expandingActivities.Add(key4, param.ExpandingActivities[key4]);
				}
				foreach (int key5 in param.SuppressedExecutions.Keys)
				{
					suppressedExecutions.Add(key5, param.SuppressedExecutions[key5]);
				}
				foreach (int item in param.executionItemOrder)
				{
					if (!executionItemOrder.Contains(item))
					{
						executionItemOrder.AddLast(item);
					}
				}
			}
		}

		internal void AppendExpandingActivity(string activityId, ExpandingLevel level)
		{
			if (!string.IsNullOrEmpty(activityId))
			{
				if (expandingActivities.ContainsKey(activityId))
				{
					expandingActivities[activityId] = level;
				}
				else
				{
					expandingActivities.Add(activityId, level);
				}
			}
		}

		internal void AppendExpandingTransfer(TraceRecord trace, ExpandingLevel level)
		{
			if (trace != null && trace.IsTransfer && trace.DataSource.Activities.ContainsKey(trace.RelatedActivityID))
			{
				Dictionary<string, Activity> dictionary = new Dictionary<string, Activity>();
				ActivityAnalyzerHelper.DetectPossibleParentActivities(trace.DataSource.Activities[trace.RelatedActivityID], trace.DataSource.Activities, dictionary, ActivityAnalyzerHelper.INIT_ACTIVITY_TREE_DEPTH2, null);
				List<long> list = new List<long>();
				foreach (TraceRecord value in collapsingTransfers.Values)
				{
					if (value.RelatedActivityID == trace.RelatedActivityID && collapsingTransfers.ContainsKey(value.TraceID))
					{
						list.Add(value.TraceID);
					}
					else if (dictionary.ContainsKey(value.RelatedActivityID))
					{
						list.Add(value.TraceID);
					}
				}
				foreach (long item in list)
				{
					collapsingTransfers.Remove(item);
				}
				if (!expandingTransfers.ContainsKey(trace.TraceID))
				{
					expandingTransfers.Add(trace.TraceID, trace);
				}
				if (!expandingTransferTraceLevel.ContainsKey(trace.TraceID))
				{
					expandingTransferTraceLevel.Add(trace.TraceID, level);
				}
			}
		}

		internal void AppendCollapsingTransfer(TraceRecord trace)
		{
			if (trace != null && trace.IsTransfer && trace.DataSource.Activities.ContainsKey(trace.RelatedActivityID))
			{
				Dictionary<string, Activity> dictionary = new Dictionary<string, Activity>();
				dictionary.Add(trace.RelatedActivityID, trace.DataSource.Activities[trace.RelatedActivityID]);
				ActivityAnalyzerHelper.DetectAllChildActivities(trace.RelatedActivityID, trace.DataSource.Activities, dictionary, null, ActivityAnalyzerHelper.INIT_ACTIVITY_TREE_DEPTH);
				List<long> list = new List<long>();
				foreach (TraceRecord value in expandingTransfers.Values)
				{
					if (value.RelatedActivityID == trace.RelatedActivityID && expandingTransfers.ContainsKey(value.TraceID))
					{
						list.Add(value.TraceID);
					}
					else if (dictionary.ContainsKey(value.RelatedActivityID))
					{
						list.Add(value.TraceID);
					}
				}
				foreach (string key in dictionary.Keys)
				{
					if (expandingActivities.ContainsKey(key))
					{
						expandingActivities.Remove(key);
					}
				}
				foreach (long item in list)
				{
					expandingTransfers.Remove(item);
					expandingTransferTraceLevel.Remove(item);
				}
				if (!collapsingTransfers.ContainsKey(trace.TraceID))
				{
					collapsingTransfers.Add(trace.TraceID, trace);
				}
			}
		}
	}
}
