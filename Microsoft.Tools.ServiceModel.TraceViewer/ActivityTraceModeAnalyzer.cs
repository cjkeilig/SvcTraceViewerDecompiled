using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class ActivityTraceModeAnalyzer : IEnumerator
	{
		private bool containsActivityBoundary = true;

		private bool containsVerboseTraces = true;

		private Activity activeActivity;

		private TraceDataSource dataSource;

		private ActivityTraceModeAnalyzerParameters parameters;

		private IErrorReport errorReport;

		private List<string> parsedActivities = new List<string>();

		private Dictionary<long, TraceRecord> results = new Dictionary<long, TraceRecord>();

		private List<ExecutionColumnItem> executionItems = new List<ExecutionColumnItem>();

		private List<ExecutionColumnItem> allInvolvedExecutionItems = new List<ExecutionColumnItem>();

		private List<TraceRecordCellItem> messageRelatedItems = new List<TraceRecordCellItem>();

		private Dictionary<long, MessageExchangeCellItem> messageExchangeMap = new Dictionary<long, MessageExchangeCellItem>();

		private List<string> suppressedActivityIds = new List<string>();

		private HorzBundRowItem currentHorzBundRowItem;

		private int renderingHashCode;

		private Dictionary<string, List<TraceRecordCellItem>> resultTraceRecordCellItems = new Dictionary<string, List<TraceRecordCellItem>>();

		internal TraceDataSource DataSource => dataSource;

		internal List<ExecutionColumnItem> AllInvolvedExecutionItems => allInvolvedExecutionItems;

		internal int RenderingHashCode => renderingHashCode;

		internal ActivityTraceModeAnalyzerParameters Parameters => parameters;

		public bool ContainsActivityBoundary => containsActivityBoundary;

		public bool ContainsVerboseTraces => containsVerboseTraces;

		internal List<ExecutionColumnItem> ExecutionColumnItems => executionItems;

		private Dictionary<string, Activity> AllActivities => DataSource.Activities;

		public Dictionary<long, MessageExchangeCellItem> MessageExchanges => messageExchangeMap;

		public object Current => currentHorzBundRowItem;

		internal bool ContainsProcess => executionItems.Count != 0;

		public Activity ActiveActivity => activeActivity;

		public ActivityTraceModeAnalyzer(Activity activeActivity, TraceDataSource dataSource, bool containsActivityBoundary, bool containsVerboseTraces, ActivityTraceModeAnalyzerParameters parameters, IErrorReport errorReport)
		{
			if (activeActivity != null && dataSource != null)
			{
				this.containsActivityBoundary = containsActivityBoundary;
				this.containsVerboseTraces = containsVerboseTraces;
				this.activeActivity = activeActivity;
				this.dataSource = dataSource;
				this.parameters = parameters;
				this.errorReport = errorReport;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					AnalysisRootActivity(ActiveActivity);
					ExpandTransfers();
					CollapseTransfers();
					ExpandActivities();
					CheckSuppressedActivities();
					SortResults();
					UpdateParameters();
				}
				catch (LogFileException e)
				{
					throw new TraceViewerException(SR.GetString("SL_ERROR_LOAD_TRACE"), e);
				}
				catch (TraceViewerException ex)
				{
					throw ex;
				}
				catch (Exception e2)
				{
					ExceptionManager.GeneralExceptionFilter(e2);
					throw new TraceViewerException(SR.GetString("SL_UnknownException"), e2);
				}
			}
		}

		private void UpdateParameters()
		{
			if (Parameters != null)
			{
				List<long> list = new List<long>();
				List<long> list2 = new List<long>();
				foreach (long item in list)
				{
					if (Parameters.ExpandingTransfers.ContainsKey(item))
					{
						Parameters.ExpandingTransfers.Remove(item);
						Parameters.ExpandingTransferTraceLevels.Remove(item);
					}
				}
				foreach (long item2 in list2)
				{
					if (Parameters.CollapsingTransfers.ContainsKey(item2))
					{
						Parameters.CollapsingTransfers.Remove(item2);
					}
				}
			}
		}

		private void CheckSuppressedActivities()
		{
			if (Parameters != null)
			{
				foreach (TraceRecord value in Parameters.ExpandingTransfers.Values)
				{
					if (suppressedActivityIds.Contains(value.RelatedActivityID))
					{
						suppressedActivityIds.Remove(value.RelatedActivityID);
					}
				}
				foreach (TraceRecord value2 in Parameters.CollapsingTransfers.Values)
				{
					if (!suppressedActivityIds.Contains(value2.RelatedActivityID))
					{
						suppressedActivityIds.Add(value2.RelatedActivityID);
					}
				}
				foreach (string key in Parameters.ExpandingActivities.Keys)
				{
					if (suppressedActivityIds.Contains(key))
					{
						suppressedActivityIds.Remove(key);
					}
				}
			}
		}

		private void ExpandTransfers()
		{
			if (Parameters != null && Parameters.ExpandingTransfers != null && Parameters.ExpandingTransfers.Count != 0)
			{
				foreach (TraceRecord value in Parameters.ExpandingTransfers.Values)
				{
					if (suppressedActivityIds.Contains(value.RelatedActivityID))
					{
						suppressedActivityIds.Remove(value.RelatedActivityID);
					}
					ExpandTransfer(value, Parameters.ExpandingTransferTraceLevels[value.TraceID]);
					InternalAnalysisActivityParents(AllActivities[value.RelatedActivityID], ActivityAnalyzerHelper.INIT_ACTIVITY_TREE_DEPTH);
				}
			}
		}

		private void ExpandActivity(Activity activity, ExpandingLevel level)
		{
			if (activity != null)
			{
				List<TraceRecord> list = activity.LoadTraceRecords(isLoadActivityBoundary: true);
				List<TraceRecord> list2 = new List<TraceRecord>();
				foreach (TraceRecord item in list)
				{
					if (item.IsTransfer)
					{
						if (item.ActivityID == activity.Id && !suppressedActivityIds.Contains(item.RelatedActivityID))
						{
							DecideWhetherHideActivityOrNotForExpandTrace(item.RelatedActivityID);
						}
						else if (item.RelatedActivityID == activity.Id && !suppressedActivityIds.Contains(item.ActivityID))
						{
							DecideWhetherHideActivityOrNotForExpandTrace(item.ActivityID);
						}
					}
					switch (level)
					{
					case ExpandingLevel.ExpandAll:
						list2.Add(item);
						break;
					case ExpandingLevel.ExpandTransferOut:
						if (item.IsTransfer)
						{
							list2.Add(item);
						}
						else if (item.Level == TraceEventType.Error || item.Level == TraceEventType.Warning || item.Level == TraceEventType.Critical || ((item.IsMessageReceivedRecord || item.IsMessageSentRecord) && (!string.IsNullOrEmpty(item.MessageID) || !string.IsNullOrEmpty(item.RelatesToMessageID))))
						{
							list2.Add(item);
						}
						break;
					}
				}
				RemoveTraceInActivity(activity);
				if (list2.Count != 0)
				{
					SaveTargetTraceRecords(activity, list2);
				}
				InternalAnalysisActivityParents(activity, ActivityAnalyzerHelper.INIT_ACTIVITY_TREE_DEPTH);
			}
		}

		private void RemoveTraceInActivity(Activity activity)
		{
			if (activity != null)
			{
				foreach (TraceRecord item in activity.LoadTraceRecords(isLoadActivityBoundary: true))
				{
					if (results.ContainsKey(item.TraceID))
					{
						results.Remove(item.TraceID);
					}
				}
			}
		}

		private void ExpandActivities()
		{
			if (Parameters != null && Parameters.ExpandingActivities != null && Parameters.ExpandingActivities.Count != 0)
			{
				foreach (string key in Parameters.ExpandingActivities.Keys)
				{
					ExpandActivity(AllActivities[key], Parameters.ExpandingActivities[key]);
				}
			}
		}

		private void CollapseTransfer(TraceRecord trace)
		{
			if (trace != null && AllActivities.ContainsKey(trace.RelatedActivityID))
			{
				Dictionary<string, List<long>> dictionary = new Dictionary<string, List<long>>();
				foreach (long key in Parameters.ExpandingTransfers.Keys)
				{
					TraceRecord traceRecord = Parameters.ExpandingTransfers[key];
					Activity activity = AllActivities[traceRecord.RelatedActivityID];
					if (!dictionary.ContainsKey(activity.Id))
					{
						dictionary.Add(activity.Id, new List<long>());
					}
					dictionary[activity.Id].Add(traceRecord.TraceID);
				}
				SuppressAllChildActivities(AllActivities[trace.RelatedActivityID], ActivityAnalyzerHelper.INIT_ACTIVITY_TREE_DEPTH2, dictionary, trace);
				if (AllActivities[trace.ActivityID].ActivityType == ActivityType.RootActivity)
				{
					List<TraceRecord> list = new List<TraceRecord>();
					list.Add(trace);
					SaveTargetTraceRecords(AllActivities[trace.ActivityID], list);
				}
			}
		}

		private void SuppressAllChildActivities(Activity activity, int depth, Dictionary<string, List<long>> expandedActivityToTraceIdsMap, TraceRecord collapsingTrace)
		{
			if (activity != null && depth < ActivityAnalyzerHelper.MAX_ACTIVITY_TREE_DEPTH && expandedActivityToTraceIdsMap != null)
			{
				if (!suppressedActivityIds.Contains(activity.Id) && !expandedActivityToTraceIdsMap.ContainsKey(activity.Id))
				{
					suppressedActivityIds.Add(activity.Id);
				}
				foreach (ExecutionInfo value in ActivityAnalyzerHelper.GetActivityExecutions(activity, null).Values)
				{
					ActivityAnalyzerHelper.GetDirectParentActivityTransferInTrace(activity.Id, null, AllActivities, value);
					foreach (Activity childActivity in ActivityAnalyzerHelper.GetChildActivities(activity.Id, null, AllActivities, value))
					{
						SuppressAllChildActivities(childActivity, depth + 1, expandedActivityToTraceIdsMap, collapsingTrace);
					}
				}
			}
		}

		private void CollapseTransfers()
		{
			if (Parameters != null && Parameters.CollapsingTransfers != null && Parameters.CollapsingTransfers.Count != 0)
			{
				foreach (TraceRecord value in Parameters.CollapsingTransfers.Values)
				{
					CollapseTransfer(value);
				}
			}
		}

		private void DecideWhetherHideActivityOrNotForExpandTrace(string activityId)
		{
			if (!parsedActivities.Contains(activityId) || suppressedActivityIds.Contains(activityId))
			{
				if (parameters != null)
				{
					foreach (TraceRecord value in parameters.ExpandingTransfers.Values)
					{
						if (value.RelatedActivityID == activityId)
						{
							return;
						}
					}
				}
				if (!suppressedActivityIds.Contains(activityId))
				{
					suppressedActivityIds.Add(activityId);
				}
			}
		}

		private void ExpandTransfer(TraceRecord trace, ExpandingLevel level)
		{
			if (trace != null && AllActivities.ContainsKey(trace.RelatedActivityID))
			{
				Activity activity = AllActivities[trace.RelatedActivityID];
				List<TraceRecord> list = activity.LoadTraceRecords(isLoadActivityBoundary: true);
				List<TraceRecord> list2 = new List<TraceRecord>();
				foreach (TraceRecord item in list)
				{
					if (level == ExpandingLevel.ExpandAll)
					{
						list2.Add(item);
					}
					if (item.TraceID != trace.TraceID && item.IsTransfer)
					{
						if (item.ActivityID == activity.Id && item.RelatedActivityID != trace.ActivityID && !suppressedActivityIds.Contains(item.RelatedActivityID))
						{
							DecideWhetherHideActivityOrNotForExpandTrace(item.RelatedActivityID);
						}
						else if (item.RelatedActivityID == activity.Id && item.ActivityID != trace.ActivityID && !suppressedActivityIds.Contains(item.ActivityID))
						{
							DecideWhetherHideActivityOrNotForExpandTrace(item.ActivityID);
						}
					}
				}
				if (list2.Count != 0)
				{
					SaveTargetTraceRecords(activity, list2);
					InternalAnalysisActivityParents(activity, ActivityAnalyzerHelper.INIT_ACTIVITY_TREE_DEPTH);
				}
			}
		}

		private bool IsAllExecutionColumnItemEmpty()
		{
			foreach (ExecutionColumnItem executionItem in executionItems)
			{
				if (executionItem.GetTopTraceRecordCellItem() != null)
				{
					return false;
				}
			}
			return true;
		}

		private DateTime GetMinDateTime()
		{
			DateTime dateTime = DateTime.MaxValue;
			foreach (ExecutionColumnItem executionItem in executionItems)
			{
				if (executionItem.GetTopTraceRecordCellItem() != null && executionItem.GetTopTraceRecordCellItem().CurrentTraceRecord.Time < dateTime)
				{
					dateTime = executionItem.GetTopTraceRecordCellItem().CurrentTraceRecord.Time;
				}
			}
			return dateTime;
		}

		public IEnumerator GetEnumerator()
		{
			Reset();
			return this;
		}

		private void SaveResultTraceRecordCellItem(TraceRecordCellItem cellItem)
		{
			if (cellItem != null && cellItem.CurrentTraceRecord != null)
			{
				if (!resultTraceRecordCellItems.ContainsKey(cellItem.RelatedActivityItem.CurrentActivity.Id))
				{
					resultTraceRecordCellItems.Add(cellItem.RelatedActivityItem.CurrentActivity.Id, new List<TraceRecordCellItem>());
				}
				resultTraceRecordCellItems[cellItem.RelatedActivityItem.CurrentActivity.Id].Add(cellItem);
			}
		}

		public List<TraceRecordCellItem> GetResultTraceRecordItemsForActivity(Activity activity)
		{
			if (activity != null && resultTraceRecordCellItems.ContainsKey(activity.Id))
			{
				return resultTraceRecordCellItems[activity.Id];
			}
			return null;
		}

		public bool MoveNext()
		{
			if (!IsAllExecutionColumnItemEmpty())
			{
				List<TraceRecordCellItem> list = new List<TraceRecordCellItem>();
				DateTime minDateTime = GetMinDateTime();
				foreach (ExecutionColumnItem executionItem in executionItems)
				{
					if (executionItem.GetTopTraceRecordCellItem() != null && executionItem.GetTopTraceRecordCellItem().CurrentTraceRecord.Time == minDateTime)
					{
						TraceRecordCellItem topTraceRecordCellItem = executionItem.GetTopTraceRecordCellItem();
						list.Add(topTraceRecordCellItem);
						SaveResultTraceRecordCellItem(topTraceRecordCellItem);
						executionItem.RemoveTopTraceRecordCellItem();
						if (topTraceRecordCellItem.CurrentTraceRecord.IsTransfer && executionItem.GetTopTraceRecordCellItem() != null && topTraceRecordCellItem.CurrentTraceRecord.TraceID == executionItem.GetTopTraceRecordCellItem().CurrentTraceRecord.TraceID)
						{
							TraceRecordCellItem topTraceRecordCellItem2 = executionItem.GetTopTraceRecordCellItem();
							list.Add(topTraceRecordCellItem2);
							SaveResultTraceRecordCellItem(topTraceRecordCellItem2);
							executionItem.RemoveTopTraceRecordCellItem();
						}
					}
				}
				currentHorzBundRowItem = new HorzBundRowItem(list, executionItems);
				return true;
			}
			currentHorzBundRowItem = null;
			return false;
		}

		public void Reset()
		{
			currentHorzBundRowItem = null;
		}

		private void SortResults()
		{
			Dictionary<int, List<TraceRecord>> dictionary = new Dictionary<int, List<TraceRecord>>();
			Dictionary<int, List<TraceRecord>> dictionary2 = new Dictionary<int, List<TraceRecord>>();
			long num = 0L;
			ExecutionInfo executionInfo = null;
			DateTime t = DateTime.MaxValue;
			foreach (TraceRecord value in results.Values)
			{
				if (Parameters == null || !Parameters.SuppressedExecutions.ContainsKey(value.Execution.ExecutionID))
				{
					num += value.TraceID;
				}
				if (value.Time < t)
				{
					executionInfo = value.Execution;
					t = value.Time;
				}
				if (!dictionary.ContainsKey(value.Execution.ExecutionID))
				{
					dictionary.Add(value.Execution.ExecutionID, new List<TraceRecord>());
				}
				dictionary[value.Execution.ExecutionID].Add(value);
			}
			renderingHashCode = (int)num;
			SortedList<TraceRecord, int> sortedList = new SortedList<TraceRecord, int>(new TraceRecordComparer());
			foreach (int key in dictionary.Keys)
			{
				sortedList.Add(dictionary[key][0], key);
			}
			for (int i = 0; i < sortedList.Count; i++)
			{
				dictionary2.Add(sortedList.Values[i], dictionary[sortedList.Values[i]]);
			}
			int num2 = (executionInfo != null) ? 1 : 0;
			Queue<ExecutionColumnItem> queue = new Queue<ExecutionColumnItem>();
			ExecutionColumnItem executionColumnItem = null;
			foreach (int key2 in dictionary2.Keys)
			{
				dictionary2[key2].Sort(new TraceRecordComparer());
				ExecutionColumnItem executionColumnItem2 = null;
				if (executionInfo == null)
				{
					executionColumnItem2 = new ExecutionColumnItem(dictionary2[key2][0].Execution, ActiveActivity, AllActivities, num2++, suppressedActivityIds, this);
					queue.Enqueue(executionColumnItem2);
				}
				else
				{
					executionColumnItem2 = new ExecutionColumnItem(dictionary2[key2][0].Execution, ActiveActivity, AllActivities, (executionInfo.ExecutionID != dictionary2[key2][0].Execution.ExecutionID) ? num2++ : 0, suppressedActivityIds, this);
					if (executionInfo.ExecutionID == dictionary2[key2][0].Execution.ExecutionID)
					{
						executionColumnItem = executionColumnItem2;
					}
					else
					{
						queue.Enqueue(executionColumnItem2);
					}
				}
				foreach (TraceRecord item in dictionary2[key2])
				{
					TraceRecordCellItem traceRecordCellItem = executionColumnItem2.AppendTraceRecord(item);
					if (traceRecordCellItem != null && (traceRecordCellItem.CurrentTraceRecord.IsMessageSentRecord || traceRecordCellItem.CurrentTraceRecord.IsMessageReceivedRecord || traceRecordCellItem.CurrentTraceRecord.IsMessageLogged))
					{
						messageRelatedItems.Add(traceRecordCellItem);
					}
				}
			}
			if (parameters == null)
			{
				parameters = new ActivityTraceModeAnalyzerParameters();
			}
			if (executionColumnItem != null && executionColumnItem.ActivityColumnCount != 0)
			{
				if (!Parameters.SuppressedExecutions.ContainsKey(executionColumnItem.CurrentExecutionInfo.ExecutionID))
				{
					executionItems.Add(executionColumnItem);
				}
				allInvolvedExecutionItems.Add(executionColumnItem);
				Parameters.PushExecutionColumnItem(executionColumnItem);
			}
			while (queue.Count != 0)
			{
				ExecutionColumnItem executionColumnItem3 = queue.Dequeue();
				if (executionColumnItem3.ActivityColumnCount != 0)
				{
					if (!Parameters.SuppressedExecutions.ContainsKey(executionColumnItem3.CurrentExecutionInfo.ExecutionID))
					{
						executionItems.Add(executionColumnItem3);
					}
					allInvolvedExecutionItems.Add(executionColumnItem3);
					parameters.PushExecutionColumnItem(executionColumnItem3);
				}
			}
			Parameters.ReorderExecutionColumnItems(executionItems);
			AnalyzeCrossExecutionMessageExchange();
			AnalyzePairedActivities();
		}

		private void AnalyzePairedActivities()
		{
			int num = 1;
			List<string> list = new List<string>();
			foreach (ExecutionColumnItem executionColumnItem in ExecutionColumnItems)
			{
				for (int i = 0; i < executionColumnItem.ActivityColumnCount; i++)
				{
					List<ActivityColumnItem> list2 = new List<ActivityColumnItem>();
					ActivityColumnItem activityColumnItem = executionColumnItem[i];
					if (!activityColumnItem.IsActiveActivity && !list.Contains(activityColumnItem.CurrentActivity.Id))
					{
						foreach (ExecutionColumnItem executionColumnItem2 in ExecutionColumnItems)
						{
							if (executionColumnItem != executionColumnItem2 && executionColumnItem2[activityColumnItem.CurrentActivity.Id] != null)
							{
								list2.Add(executionColumnItem2[activityColumnItem.CurrentActivity.Id]);
							}
						}
						if (list2.Count != 0)
						{
							list2.Add(activityColumnItem);
							foreach (ActivityColumnItem item in list2)
							{
								item.PairedActivityIndex = num;
							}
							list.Add(activityColumnItem.CurrentActivity.Id);
							num++;
						}
					}
				}
			}
		}

		private void AnalyzeCrossExecutionMessageExchange()
		{
			Dictionary<string, List<TraceRecordCellItem>> dictionary = new Dictionary<string, List<TraceRecordCellItem>>();
			Dictionary<string, TraceRecordCellItem> dictionary2 = new Dictionary<string, TraceRecordCellItem>();
			Dictionary<string, TraceRecordCellItem> dictionary3 = new Dictionary<string, TraceRecordCellItem>();
			foreach (TraceRecordCellItem messageRelatedItem in messageRelatedItems)
			{
				if (!string.IsNullOrEmpty(messageRelatedItem.CurrentTraceRecord.MessageID) || !string.IsNullOrEmpty(messageRelatedItem.CurrentTraceRecord.RelatesToMessageID))
				{
					string text = messageRelatedItem.CurrentTraceRecord.MessageID;
					if (string.IsNullOrEmpty(text))
					{
						text = "ReplyTo:" + messageRelatedItem.CurrentTraceRecord.RelatesToMessageID;
					}
					if (messageRelatedItem.CurrentTraceRecord.IsMessageLogged)
					{
						if (!dictionary.ContainsKey(text))
						{
							dictionary.Add(text, new List<TraceRecordCellItem>());
						}
						dictionary[text].Add(messageRelatedItem);
					}
					else if (messageRelatedItem.CurrentTraceRecord.IsMessageSentRecord && !dictionary2.ContainsKey(text))
					{
						dictionary2.Add(text, messageRelatedItem);
					}
					else if (messageRelatedItem.CurrentTraceRecord.IsMessageReceivedRecord && !dictionary3.ContainsKey(text))
					{
						dictionary3.Add(text, messageRelatedItem);
					}
				}
			}
			foreach (string key in dictionary2.Keys)
			{
				if (dictionary3.ContainsKey(key))
				{
					MessageExchangeCellItem messageExchangeCellItem = new MessageExchangeCellItem();
					messageExchangeCellItem.SentExecutionColumnItem = dictionary2[key].RelatedExecutionItem;
					messageExchangeCellItem.ReceiveExecutionColumnItem = dictionary3[key].RelatedExecutionItem;
					messageExchangeCellItem.SentTraceRecordCellItem = dictionary2[key];
					messageExchangeCellItem.ReceiveTraceRecordCellItem = dictionary3[key];
					if (dictionary.ContainsKey(key) && dictionary[key].Count != 0)
					{
						messageExchangeCellItem.RelatedMessageTraceCellItems.AddRange(dictionary[key]);
					}
					messageExchangeMap.Add(dictionary2[key].CurrentTraceRecord.TraceID, messageExchangeCellItem);
				}
			}
		}

		internal static bool IsValidForGraphFilter(TraceRecord tr, bool containsActivityBoundary, bool containsVerboseTraces)
		{
			bool result = false;
			if (tr != null)
			{
				result = ((containsActivityBoundary || (tr.Level != TraceEventType.Start && tr.Level != TraceEventType.Stop && tr.Level != TraceEventType.Resume && tr.Level != TraceEventType.Suspend)) && ((containsVerboseTraces || tr.Level != TraceEventType.Verbose || tr.IsMessageReceivedRecord || tr.IsMessageSentRecord) ? true : false));
			}
			return result;
		}

		private void SaveTargetTraceRecords(Activity activity, List<TraceRecord> traces)
		{
			if (traces != null)
			{
				foreach (TraceRecord trace in traces)
				{
					if (trace.IsTransfer && trace.ActivityID == trace.RelatedActivityID)
					{
						throw new TraceViewerException(SR.GetString("SL_InvalidTransfer"));
					}
					if (!results.ContainsKey(trace.TraceID) && IsValidForGraphFilter(trace, ContainsActivityBoundary, ContainsVerboseTraces))
					{
						results.Add(trace.TraceID, trace);
					}
				}
				if (!parsedActivities.Contains(activity.Id))
				{
					parsedActivities.Add(activity.Id);
				}
			}
		}

		private void AnalysisHostActivityChild(Activity activity, ExecutionInfo executionInfo, List<string> relatedHostActivityIdentifiers)
		{
			if (activity != null && executionInfo != null && ActivityAnalyzerHelper.IsHostRelatedActivity(activity) && !suppressedActivityIds.Contains(activity.Id))
			{
				List<TraceRecord> list = activity.LoadTraceRecords( true, executionInfo);
				List<TraceRecord> list2 = new List<TraceRecord>();
				foreach (TraceRecord item in list)
				{
					if (item.IsTransfer && item.ActivityID == activity.Id && ActivityAnalyzerHelper.IsHostRelatedActivity(AllActivities[item.RelatedActivityID]) && !suppressedActivityIds.Contains(item.RelatedActivityID))
					{
						string hostActivityNameIdentifier = ActivityAnalyzerHelper.GetHostActivityNameIdentifier(AllActivities[item.RelatedActivityID]);
						if (relatedHostActivityIdentifiers.Contains(hostActivityNameIdentifier) && item.Execution.ExecutionID == executionInfo.ExecutionID)
						{
							list2.Add(item);
							if (ActiveActivity.Id != item.RelatedActivityID)
							{
								suppressedActivityIds.Add(item.RelatedActivityID);
							}
						}
					}
				}
				SaveTargetTraceRecords(activity, list2);
			}
		}

		private void AnalysisHostActivityRoot(Activity activity)
		{
			if (activity != null && ActivityAnalyzerHelper.IsHostRelatedActivity(activity))
			{
				List<string> relatedHostActivityIdentifiers = null;
				foreach (ExecutionInfo value in ActivityAnalyzerHelper.GetActivityExecutions(activity, null).Values)
				{
					List<TraceRecord> list = activity.LoadTraceRecords(true, value);
					Activity activity2 = ActivityAnalyzerHelper.FindRootHostActivity(activity, AllActivities, value, out relatedHostActivityIdentifiers);
					if (activity2 != null)
					{
						AnalysisHostActivityChild(activity2, value, relatedHostActivityIdentifiers);
					}
					TraceRecord directParentActivityTransferInTrace = ActivityAnalyzerHelper.GetDirectParentActivityTransferInTrace(activity.Id, list, AllActivities, value);
					foreach (TraceRecord item in list)
					{
						if (item.IsTransfer)
						{
							if (!suppressedActivityIds.Contains(item.ActivityID) && !parsedActivities.Contains(item.ActivityID) && item.ActivityID != ActiveActivity.Id && (directParentActivityTransferInTrace == null || directParentActivityTransferInTrace.ActivityID != item.ActivityID))
							{
								suppressedActivityIds.Add(item.ActivityID);
							}
							else if (!suppressedActivityIds.Contains(item.RelatedActivityID) && !parsedActivities.Contains(item.RelatedActivityID) && item.RelatedActivityID != ActiveActivity.Id && (directParentActivityTransferInTrace == null || directParentActivityTransferInTrace.ActivityID != item.RelatedActivityID))
							{
								suppressedActivityIds.Add(item.RelatedActivityID);
							}
						}
					}
					SaveTargetTraceRecords(activity, list);
					InternalAnalysisActivityParents(activity, ActivityAnalyzerHelper.INIT_ACTIVITY_TREE_DEPTH);
				}
			}
		}

		private void AnalysisRootActivity(Activity rootActivity)
		{
			if (rootActivity != null)
			{
				if (ActivityAnalyzerHelper.IsHostRelatedActivity(rootActivity))
				{
					AnalysisHostActivityRoot(rootActivity);
				}
				else if (ActivityAnalyzerHelper.IsMessageRelatedActivity(rootActivity))
				{
					AnalysisMessageRelatedActivityRoot(rootActivity);
				}
				else
				{
					InternalAnalysisActivityParents(rootActivity, ActivityAnalyzerHelper.INIT_ACTIVITY_TREE_DEPTH);
					List<TraceRecord> traces = rootActivity.LoadTraceRecords(isLoadActivityBoundary: true);
					List<Activity> childActivities = ActivityAnalyzerHelper.GetChildActivities(rootActivity.Id, traces, AllActivities, null);
					if (childActivities != null)
					{
						foreach (Activity item in childActivities)
						{
							if (!suppressedActivityIds.Contains(item.Id))
							{
								suppressedActivityIds.Add(item.Id);
							}
						}
					}
					SaveTargetTraceRecords(rootActivity, traces);
				}
			}
		}

		private void AnalysisMessageRelatedActivityParents(Activity activity, Activity childMessageActivity, int depth)
		{
			if (activity != null && depth < ActivityAnalyzerHelper.MAX_ACTIVITY_TREE_DEPTH)
			{
				List<TraceRecord> list = null;
				list = activity.LoadTraceRecords(isLoadActivityBoundary: true);
				Dictionary<int, ExecutionInfo> activityExecutions = ActivityAnalyzerHelper.GetActivityExecutions(activity, list);
				foreach (int key in activityExecutions.Keys)
				{
					if (activity.ActivityType == ActivityType.ConnectionActivity && childMessageActivity != null)
					{
						TraceRecord directParentActivityTransferInTrace = ActivityAnalyzerHelper.GetDirectParentActivityTransferInTrace(activity.Id, list, AllActivities, activityExecutions[key]);
						if (directParentActivityTransferInTrace != null)
						{
							List<TraceRecord> list2 = new List<TraceRecord>();
							list2.Add(directParentActivityTransferInTrace);
							Queue<TraceRecord> queue = new Queue<TraceRecord>();
							bool flag = false;
							foreach (TraceRecord item in list)
							{
								if (item.TraceID != directParentActivityTransferInTrace.TraceID)
								{
									if (item.IsTransfer && item.ActivityID == activity.Id)
									{
										if (item.RelatedActivityID == childMessageActivity.Id)
										{
											flag = true;
										}
										else
										{
											if (flag)
											{
												break;
											}
											queue.Clear();
										}
									}
									else if (item.IsTransfer && item.RelatedActivityID == activity.Id)
									{
										if (item.RelatedActivityID == childMessageActivity.Id && flag)
										{
											queue.Enqueue(item);
											break;
										}
									}
									else
									{
										queue.Enqueue(item);
									}
								}
							}
							while (queue.Count != 0)
							{
								list2.Add(queue.Dequeue());
							}
							TraceRecord backwardTransferInTrace = ActivityAnalyzerHelper.GetBackwardTransferInTrace(directParentActivityTransferInTrace.ActivityID, directParentActivityTransferInTrace.RelatedActivityID, list, AllActivities, activityExecutions[key]);
							if (backwardTransferInTrace != null)
							{
								list2.Add(backwardTransferInTrace);
							}
							if (AllActivities.ContainsKey(directParentActivityTransferInTrace.ActivityID))
							{
								AnalysisMessageRelatedActivityParents(AllActivities[directParentActivityTransferInTrace.ActivityID], childMessageActivity, depth + 1);
								SaveTargetTraceRecords(AllActivities[directParentActivityTransferInTrace.ActivityID], list2);
							}
						}
					}
					else
					{
						TraceRecord directParentActivityTransferInTrace2 = ActivityAnalyzerHelper.GetDirectParentActivityTransferInTrace(activity.Id, list, AllActivities, activityExecutions[key]);
						if (directParentActivityTransferInTrace2 != null)
						{
							List<TraceRecord> list3 = new List<TraceRecord>();
							list3.Add(directParentActivityTransferInTrace2);
							TraceRecord backwardTransferInTrace2 = ActivityAnalyzerHelper.GetBackwardTransferInTrace(directParentActivityTransferInTrace2.ActivityID, directParentActivityTransferInTrace2.RelatedActivityID, list, AllActivities, activityExecutions[key]);
							if (backwardTransferInTrace2 != null)
							{
								list3.Add(backwardTransferInTrace2);
							}
							if (AllActivities.ContainsKey(directParentActivityTransferInTrace2.ActivityID))
							{
								AnalysisMessageRelatedActivityParents(AllActivities[directParentActivityTransferInTrace2.ActivityID], childMessageActivity, depth + 1);
								SaveTargetTraceRecords(AllActivities[directParentActivityTransferInTrace2.ActivityID], list3);
							}
						}
					}
				}
			}
		}

		private void AnalysisMessageRelatedActivityRoot(Activity activity)
		{
			if (activity != null && ActivityAnalyzerHelper.IsMessageRelatedActivity(activity))
			{
				if (activity.ActivityType == ActivityType.MessageActivity)
				{
					InternalAnalysisActivityParents(activity, ActivityAnalyzerHelper.INIT_ACTIVITY_TREE_DEPTH);
				}
				else if (activity.ActivityType == ActivityType.ConnectionActivity)
				{
					AnalysisMessageRelatedActivityParents(activity, null, ActivityAnalyzerHelper.INIT_ACTIVITY_TREE_DEPTH);
				}
				else
				{
					InternalAnalysisActivityParents(activity, ActivityAnalyzerHelper.INIT_ACTIVITY_TREE_DEPTH);
				}
				List<TraceRecord> list = activity.LoadTraceRecords(isLoadActivityBoundary: true);
				foreach (TraceRecord item in list)
				{
					if (item.IsTransfer)
					{
						if (item.ActivityID == activity.Id && !parsedActivities.Contains(item.RelatedActivityID) && !suppressedActivityIds.Contains(item.RelatedActivityID))
						{
							suppressedActivityIds.Add(item.RelatedActivityID);
						}
						if (item.RelatedActivityID == activity.Id && !parsedActivities.Contains(item.ActivityID) && !suppressedActivityIds.Contains(item.ActivityID))
						{
							suppressedActivityIds.Add(item.ActivityID);
						}
					}
				}
				SaveTargetTraceRecords(activity, list);
			}
		}

		private void InternalAnalysisActivityParents(Activity activity, int depth, ExecutionInfo execution)
		{
			if (activity != null && depth < ActivityAnalyzerHelper.MAX_ACTIVITY_TREE_DEPTH)
			{
				List<TraceRecord> list = null;
				list = activity.LoadTraceRecords(isLoadActivityBoundary: true);
				Dictionary<int, ExecutionInfo> activityExecutions = ActivityAnalyzerHelper.GetActivityExecutions(activity, list);
				foreach (int key in activityExecutions.Keys)
				{
					if (execution == null || execution.ExecutionID == key)
					{
						TraceRecord directParentActivityTransferInTrace = ActivityAnalyzerHelper.GetDirectParentActivityTransferInTrace(activity.Id, list, AllActivities, activityExecutions[key]);
						if (directParentActivityTransferInTrace != null && AllActivities.ContainsKey(directParentActivityTransferInTrace.ActivityID))
						{
							List<TraceRecord> list2 = new List<TraceRecord>();
							list2.Add(directParentActivityTransferInTrace);
							if (suppressedActivityIds.Contains(directParentActivityTransferInTrace.ActivityID))
							{
								suppressedActivityIds.Remove(directParentActivityTransferInTrace.ActivityID);
							}
							TraceRecord backwardTransferInTrace = ActivityAnalyzerHelper.GetBackwardTransferInTrace(directParentActivityTransferInTrace.ActivityID, directParentActivityTransferInTrace.RelatedActivityID, list, AllActivities, activityExecutions[key]);
							if (backwardTransferInTrace != null)
							{
								list2.Add(backwardTransferInTrace);
								if (suppressedActivityIds.Contains(backwardTransferInTrace.ActivityID))
								{
									suppressedActivityIds.Remove(backwardTransferInTrace.ActivityID);
								}
							}
							InternalAnalysisActivityParents(AllActivities[directParentActivityTransferInTrace.ActivityID], depth + 1, activityExecutions[key]);
							SaveTargetTraceRecords(AllActivities[directParentActivityTransferInTrace.ActivityID], list2);
						}
					}
				}
			}
		}

		private void InternalAnalysisActivityParents(Activity activity, int depth)
		{
			InternalAnalysisActivityParents(activity, depth, null);
		}
	}
}
