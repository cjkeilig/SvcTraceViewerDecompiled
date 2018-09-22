using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal static class ActivityAnalyzerHelper
	{
		public enum DirectActivityRelationship
		{
			Parent,
			Child,
			NoRelationship
		}

		private class ActivityAnalyzerHelperMethodResultCache
		{
			private Dictionary<int, Dictionary<string, Activity>> detectPossibleParentActivitiesCache = new Dictionary<int, Dictionary<string, Activity>>();

			private Dictionary<int, Dictionary<string, Activity>> detectAllChildActivitiesCache = new Dictionary<int, Dictionary<string, Activity>>();

			private Dictionary<int, List<Activity>> getChildActivitiesCache = new Dictionary<int, List<Activity>>();

			public bool DetectPossibleParentActivitiesProxy(Activity activity, Dictionary<string, Activity> allActivities, Dictionary<string, Activity> parentActivities, int depth, ExecutionInfo execution)
			{
				if (depth == INIT_ACTIVITY_TREE_DEPTH)
				{
					int key = activity.Id.GetHashCode() * allActivities.GetHashCode() * (execution?.ExecutionID ?? 1);
					if (detectPossibleParentActivitiesCache.ContainsKey(key))
					{
						foreach (string key2 in detectPossibleParentActivitiesCache[key].Keys)
						{
							if (!parentActivities.ContainsKey(key2))
							{
								parentActivities.Add(key2, detectPossibleParentActivitiesCache[key][key2]);
							}
						}
						return true;
					}
				}
				return false;
			}

			public void DetectPossibleParentActivitiesResultCache(Activity activity, Dictionary<string, Activity> allActivities, Dictionary<string, Activity> parentActivities, int depth, ExecutionInfo execution)
			{
				if (depth == INIT_ACTIVITY_TREE_DEPTH && parentActivities != null)
				{
					int key = activity.Id.GetHashCode() * allActivities.GetHashCode() * (execution?.ExecutionID ?? 1);
					if (detectPossibleParentActivitiesCache.ContainsKey(key))
					{
						detectPossibleParentActivitiesCache[key] = parentActivities;
					}
					else
					{
						detectPossibleParentActivitiesCache.Add(key, parentActivities);
					}
				}
			}

			public bool DetectAllChildActivitiesProxy(string currentActivityID, Dictionary<string, Activity> allActivities, Dictionary<string, Activity> resultActivities, ExecutionInfo execution, int depth)
			{
				if (depth == INIT_ACTIVITY_TREE_DEPTH)
				{
					int key = currentActivityID.GetHashCode() * allActivities.GetHashCode() * (execution?.ExecutionID ?? 1);
					if (detectAllChildActivitiesCache.ContainsKey(key))
					{
						foreach (string key2 in detectAllChildActivitiesCache[key].Keys)
						{
							if (!resultActivities.ContainsKey(key2))
							{
								resultActivities.Add(key2, detectAllChildActivitiesCache[key][key2]);
							}
						}
						return true;
					}
				}
				return false;
			}

			public void DetectAllChildActivitiesResultCache(string currentActivityID, Dictionary<string, Activity> allActivities, Dictionary<string, Activity> resultActivities, ExecutionInfo execution, int depth)
			{
				if (depth == INIT_ACTIVITY_TREE_DEPTH && resultActivities != null)
				{
					int key = currentActivityID.GetHashCode() * allActivities.GetHashCode() * (execution?.ExecutionID ?? 1);
					if (detectAllChildActivitiesCache.ContainsKey(key))
					{
						detectAllChildActivitiesCache[key] = resultActivities;
					}
					else
					{
						detectAllChildActivitiesCache.Add(key, resultActivities);
					}
				}
			}

			public List<Activity> GetChildActivitiesProxy(string currentActivityID, Dictionary<string, Activity> allActivities, ExecutionInfo execution)
			{
				int key = currentActivityID.GetHashCode() * allActivities.GetHashCode() * (execution?.ExecutionID ?? 1);
				if (getChildActivitiesCache.ContainsKey(key))
				{
					return getChildActivitiesCache[key];
				}
				return null;
			}

			public void GetChildActivitiesResultCache(string currentActivityID, Dictionary<string, Activity> allActivities, ExecutionInfo execution, List<Activity> result)
			{
				if (result != null)
				{
					int key = currentActivityID.GetHashCode() * allActivities.GetHashCode() * (execution?.ExecutionID ?? 1);
					if (getChildActivitiesCache.ContainsKey(key))
					{
						getChildActivitiesCache[key] = result;
					}
					else
					{
						getChildActivitiesCache.Add(key, result);
					}
				}
			}

			public ActivityAnalyzerHelperMethodResultCache(TraceViewerForm parentForm)
			{
				if (parentForm != null)
				{
					parentForm.DataSourceChangedHandler = (TraceViewerForm.DataSourceChanged)Delegate.Combine(parentForm.DataSourceChangedHandler, new TraceViewerForm.DataSourceChanged(DataSource_Changed));
				}
			}

			private void DataSource_Changed(TraceDataSource dataSource)
			{
				if (dataSource != null)
				{
					dataSource.AppendFileFinishedCallback = (TraceDataSource.AppendFileFinished)Delegate.Combine(dataSource.AppendFileFinishedCallback, new TraceDataSource.AppendFileFinished(DataSource_FileAppended));
					dataSource.ReloadFilesFinishedCallback = (TraceDataSource.ReloadFilesFinished)Delegate.Combine(dataSource.ReloadFilesFinishedCallback, new TraceDataSource.ReloadFilesFinished(DataSource_Reloaded));
					dataSource.RemoveAllFileFinishedCallback = (TraceDataSource.RemoveAllFileFinished)Delegate.Combine(dataSource.RemoveAllFileFinishedCallback, new TraceDataSource.RemoveAllFileFinished(DataSource_RemoveAllFilesFinished));
					dataSource.RemoveFileFinishedCallback = (TraceDataSource.RemoveFileFinished)Delegate.Combine(dataSource.RemoveFileFinishedCallback, new TraceDataSource.RemoveFileFinished(DataSource_RemoveFilesFinished));
				}
			}

			private void DataSource_FileAppended(string[] fileNames, TaskInfoBase task)
			{
				InvalidateCache();
			}

			private void DataSource_Reloaded()
			{
				InvalidateCache();
			}

			private void DataSource_RemoveAllFilesFinished()
			{
				InvalidateCache();
			}

			private void DataSource_RemoveFilesFinished(string[] fileNames)
			{
				InvalidateCache();
			}

			private void InvalidateCache()
			{
				detectPossibleParentActivitiesCache.Clear();
				getChildActivitiesCache.Clear();
				detectAllChildActivitiesCache.Clear();
			}
		}

		internal static int MAX_ACTIVITY_TREE_DEPTH = 20;

		internal static int INIT_ACTIVITY_TREE_DEPTH = 1;

		internal static int INIT_ACTIVITY_TREE_DEPTH2 = 12;

		private static ActivityAnalyzerHelperMethodResultCache resultCache = null;

		internal static void Initialize(TraceViewerForm parentForm)
		{
			resultCache = new ActivityAnalyzerHelperMethodResultCache(parentForm);
		}

		private static List<TraceRecord> InternalLoadTraceRecordWarpper(Activity activity, ExecutionInfo execution, bool containsActivityBoundary)
		{
			List<TraceRecord> list = null;
			if (activity != null)
			{
				try
				{
					list = ((execution != null) ? activity.LoadTraceRecords(containsActivityBoundary, execution) : activity.LoadTraceRecords(containsActivityBoundary));
				}
				catch (LogFileException e)
				{
					throw new TraceViewerException(SR.GetString("SL_ERROR_LOAD_TRACE"), e);
				}
			}
			if (list == null)
			{
				list = new List<TraceRecord>();
			}
			return list;
		}

		public static DirectActivityRelationship DetectDirectRelationshipBetweenActivities(Activity activity1, Activity activity2, ExecutionInfo execution)
		{
			if (activity1 != null && activity2 != null)
			{
				foreach (TraceRecord item in InternalLoadTraceRecordWarpper(activity1, execution, containsActivityBoundary: false))
				{
					if (item.IsTransfer && item.ActivityID == activity1.Id && item.RelatedActivityID == activity2.Id)
					{
						return DirectActivityRelationship.Parent;
					}
					if (item.IsTransfer && item.ActivityID == activity2.Id && item.RelatedActivityID == activity1.Id)
					{
						return DirectActivityRelationship.Child;
					}
				}
			}
			return DirectActivityRelationship.NoRelationship;
		}

		internal static void DetectPossibleParentActivities(Activity activity, Dictionary<string, Activity> allActivities, Dictionary<string, Activity> parentActivities, int depth, ExecutionInfo execution)
		{
			if (activity != null && allActivities != null && parentActivities != null && depth < MAX_ACTIVITY_TREE_DEPTH && (resultCache == null || !resultCache.DetectPossibleParentActivitiesProxy(activity, allActivities, parentActivities, depth, execution)))
			{
				List<TraceRecord> traces = InternalLoadTraceRecordWarpper(activity, execution, containsActivityBoundary: true);
				Dictionary<int, ExecutionInfo> dictionary = null;
				if (execution == null)
				{
					dictionary = GetActivityExecutions(activity, traces);
				}
				else
				{
					dictionary = new Dictionary<int, ExecutionInfo>();
					dictionary.Add(execution.ExecutionID, execution);
				}
				foreach (ExecutionInfo value in dictionary.Values)
				{
					ExecutionInfo executionInfo = value;
					TraceRecord directParentActivityTransferInTrace = GetDirectParentActivityTransferInTrace(activity.Id, traces, allActivities, execution);
					if (directParentActivityTransferInTrace != null && allActivities.ContainsKey(directParentActivityTransferInTrace.ActivityID))
					{
						if (!parentActivities.ContainsKey(directParentActivityTransferInTrace.ActivityID))
						{
							parentActivities.Add(directParentActivityTransferInTrace.ActivityID, allActivities[directParentActivityTransferInTrace.ActivityID]);
						}
						DetectPossibleParentActivities(allActivities[directParentActivityTransferInTrace.ActivityID], allActivities, parentActivities, depth + 1, execution);
					}
				}
				if (resultCache != null)
				{
					resultCache.DetectPossibleParentActivitiesResultCache(activity, allActivities, parentActivities, depth, execution);
				}
			}
		}

		internal static void DetectErrorOrWarningOnActivity(Activity activity, Dictionary<string, Activity> allActivities, List<TraceRecord> traces, ExecutionInfo execution, bool isChild, bool isDetectSeverityLevel, ref TraceRecordSetSeverityLevel severityLevel, ref TraceRecord firstErrorTrace, int depth)
		{
			if (activity != null && depth < MAX_ACTIVITY_TREE_DEPTH)
			{
				traces = ((traces == null) ? InternalLoadTraceRecordWarpper(activity, execution, containsActivityBoundary: true) : traces);
				foreach (TraceRecord trace in traces)
				{
					if (execution == null || execution.ExecutionID == trace.Execution.ExecutionID)
					{
						if (trace.Level == TraceEventType.Critical || trace.Level == TraceEventType.Error)
						{
							if (firstErrorTrace == null)
							{
								firstErrorTrace = trace;
							}
							if (severityLevel < TraceRecordSetSeverityLevel.Error)
							{
								severityLevel = TraceRecordSetSeverityLevel.Error;
							}
							return;
						}
						if (trace.Level == TraceEventType.Warning)
						{
							if (firstErrorTrace == null)
							{
								firstErrorTrace = trace;
							}
							if (isDetectSeverityLevel)
							{
								break;
							}
							if (severityLevel < TraceRecordSetSeverityLevel.Warning)
							{
								severityLevel = TraceRecordSetSeverityLevel.Warning;
							}
						}
					}
				}
				if (isChild)
				{
					foreach (Activity childActivity in GetChildActivities(activity.Id, traces, allActivities, execution))
					{
						DetectErrorOrWarningOnActivity(childActivity, allActivities, null, null, isChild, isDetectSeverityLevel, ref severityLevel, ref firstErrorTrace, depth + 1);
						if (firstErrorTrace != null)
						{
							switch (isDetectSeverityLevel)
							{
							case false:
								return;
							}
							if (severityLevel == TraceRecordSetSeverityLevel.Error)
							{
								break;
							}
						}
					}
				}
			}
		}

		internal static Dictionary<int, ExecutionInfo> GetActivityExecutions(Activity activity, List<TraceRecord> traces)
		{
			Dictionary<int, ExecutionInfo> dictionary = new Dictionary<int, ExecutionInfo>();
			if (activity != null)
			{
				traces = ((traces == null) ? InternalLoadTraceRecordWarpper(activity, null, containsActivityBoundary: true) : traces);
				{
					foreach (TraceRecord trace in traces)
					{
						if (!dictionary.ContainsKey(trace.Execution.ExecutionID))
						{
							dictionary.Add(trace.Execution.ExecutionID, trace.Execution);
						}
					}
					return dictionary;
				}
			}
			return dictionary;
		}

		internal static Activity InternalFindParentActivityInType(Activity activity, Dictionary<string, Activity> allActivities, ActivityType expectedParentType, bool isFirstForwardOccur, ExecutionInfo execution)
		{
			if (activity != null && allActivities != null)
			{
				if (isFirstForwardOccur && activity.ActivityType == expectedParentType)
				{
					return activity;
				}
				int i = INIT_ACTIVITY_TREE_DEPTH;
				Activity activity2 = activity;
				TraceRecord directParentActivityTransferInTrace = GetDirectParentActivityTransferInTrace(activity.Id, null, allActivities, execution);
				for (; i < MAX_ACTIVITY_TREE_DEPTH; i++)
				{
					if (directParentActivityTransferInTrace == null)
					{
						break;
					}
					if (!allActivities.ContainsKey(directParentActivityTransferInTrace.ActivityID))
					{
						break;
					}
					if (allActivities[directParentActivityTransferInTrace.ActivityID].ActivityType == expectedParentType)
					{
						if (isFirstForwardOccur)
						{
							return allActivities[directParentActivityTransferInTrace.ActivityID];
						}
						activity2 = allActivities[directParentActivityTransferInTrace.ActivityID];
					}
					directParentActivityTransferInTrace = GetDirectParentActivityTransferInTrace(allActivities[directParentActivityTransferInTrace.ActivityID].Id, null, allActivities, execution);
				}
				if (!isFirstForwardOccur && activity2 != null && activity2.ActivityType == expectedParentType)
				{
					return activity2;
				}
			}
			return null;
		}

		internal static string GetHostActivityNameIdentifier(Activity activity)
		{
			if (activity != null && IsHostRelatedActivity(activity) && !string.IsNullOrEmpty(activity.Name))
			{
				string text = activity.Name.Trim();
				int num = text.LastIndexOf('/');
				if (num != -1 && text.Length > num + 1)
				{
					return text.Substring(num + 1);
				}
			}
			return null;
		}

		internal static Activity FindRootHostActivity(Activity activity, Dictionary<string, Activity> allActivities, ExecutionInfo hostExecution, out List<string> relatedHostActivityIdentifiers)
		{
			relatedHostActivityIdentifiers = new List<string>();
			if (activity != null && allActivities != null && hostExecution != null && allActivities.ContainsKey("{00000000-0000-0000-0000-000000000000}"))
			{
				Activity activity2 = InternalFindParentActivityInType(activity, allActivities, ActivityType.ServiceHostActivity, false, hostExecution);
				if (activity2 != null)
				{
					string hostActivityNameIdentifier = GetHostActivityNameIdentifier(activity2);
					if (!string.IsNullOrEmpty(hostActivityNameIdentifier) && !relatedHostActivityIdentifiers.Contains(hostActivityNameIdentifier))
					{
						relatedHostActivityIdentifiers.Add(hostActivityNameIdentifier);
					}
					TraceRecord directParentActivityTransferInTrace = GetDirectParentActivityTransferInTrace(activity2.Id, null, allActivities, null);
					while (directParentActivityTransferInTrace != null && allActivities.ContainsKey(directParentActivityTransferInTrace.ActivityID) && allActivities[directParentActivityTransferInTrace.ActivityID].ActivityType == ActivityType.ServiceHostActivity)
					{
						hostActivityNameIdentifier = GetHostActivityNameIdentifier(allActivities[directParentActivityTransferInTrace.ActivityID]);
						if (!string.IsNullOrEmpty(hostActivityNameIdentifier) && !relatedHostActivityIdentifiers.Contains(hostActivityNameIdentifier))
						{
							relatedHostActivityIdentifiers.Add(hostActivityNameIdentifier);
						}
						directParentActivityTransferInTrace = GetDirectParentActivityTransferInTrace(allActivities[directParentActivityTransferInTrace.ActivityID].Id, null, allActivities, null);
					}
					InternalEnlistAllChildHostActivityIdentifier(activity2, allActivities, relatedHostActivityIdentifiers, INIT_ACTIVITY_TREE_DEPTH2);
				}
				Activity activity3 = activity2;
				if (activity3 != null)
				{
					Activity result = activity3;
					activity3 = InternalFindParentActivityInType(activity3, allActivities, ActivityType.RootActivity, false, hostExecution);
					if (activity3 != null)
					{
						return activity3;
					}
					return result;
				}
			}
			return null;
		}

		private static void InternalEnlistAllChildHostActivityIdentifier(Activity activity, Dictionary<string, Activity> allActivities, List<string> relatedHostActivityIdentifiers, int depth)
		{
			if (activity != null && allActivities != null && depth < MAX_ACTIVITY_TREE_DEPTH && relatedHostActivityIdentifiers != null && activity.ActivityType == ActivityType.ServiceHostActivity)
			{
				string hostActivityNameIdentifier = GetHostActivityNameIdentifier(activity);
				if (!string.IsNullOrEmpty(hostActivityNameIdentifier) && !relatedHostActivityIdentifiers.Contains(hostActivityNameIdentifier))
				{
					relatedHostActivityIdentifiers.Add(hostActivityNameIdentifier);
				}
				foreach (Activity childActivity in GetChildActivities(activity.Id, null, allActivities, null))
				{
					if (childActivity.ActivityType == ActivityType.ServiceHostActivity)
					{
						InternalEnlistAllChildHostActivityIdentifier(childActivity, allActivities, relatedHostActivityIdentifiers, depth + 1);
					}
				}
			}
		}

		internal static bool IsMessageRelatedActivity(Activity activity)
		{
			if (activity != null && activity.ActivityType >= ActivityType.ConnectionActivity && activity.ActivityType < ActivityType.NormalActivity)
			{
				return true;
			}
			return false;
		}

		internal static bool IsHostRelatedActivity(Activity activity)
		{
			if (activity != null && activity.ActivityType < ActivityType.ConnectionActivity)
			{
				return true;
			}
			return false;
		}

		internal static TraceRecord GetBackwardTransferInTrace(string parentActivityID, string childActivityID, List<TraceRecord> traces, Dictionary<string, Activity> allActivities, ExecutionInfo execution)
		{
			if (!string.IsNullOrEmpty(parentActivityID) && !string.IsNullOrEmpty(childActivityID) && allActivities != null && allActivities.ContainsKey(parentActivityID) && allActivities.ContainsKey(childActivityID))
			{
				traces = ((traces == null) ? InternalLoadTraceRecordWarpper(allActivities[childActivityID], execution, containsActivityBoundary: false) : traces);
				foreach (TraceRecord trace in traces)
				{
					if ((execution == null || execution.ExecutionID == trace.Execution.ExecutionID) && trace.IsTransfer && trace.ActivityID == childActivityID && trace.RelatedActivityID == parentActivityID)
					{
						return trace;
					}
				}
			}
			return null;
		}

		internal static TraceRecord GetDirectParentActivityTransferInTrace(string currentActivityID, List<TraceRecord> traces, Dictionary<string, Activity> allActivities, ExecutionInfo execution)
		{
			if (!string.IsNullOrEmpty(currentActivityID) && allActivities != null && allActivities.ContainsKey(currentActivityID) && allActivities[currentActivityID].ActivityType != 0)
			{
				bool flag = false;
				traces = ((traces == null) ? InternalLoadTraceRecordWarpper(allActivities[currentActivityID], execution, containsActivityBoundary: false) : traces);
				foreach (TraceRecord trace in traces)
				{
					if (execution == null || execution.ExecutionID == trace.Execution.ExecutionID)
					{
						if (!flag && trace.IsTransfer)
						{
							if (trace.ActivityID == currentActivityID)
							{
								return null;
							}
							flag = true;
						}
						if (trace.IsTransfer && trace.RelatedActivityID == currentActivityID && allActivities.ContainsKey(trace.ActivityID))
						{
							return trace;
						}
					}
				}
			}
			return null;
		}

		internal static void DetectAllChildActivities(string currentActivityID, Dictionary<string, Activity> allActivities, Dictionary<string, Activity> resultActivities, ExecutionInfo execution, int depth)
		{
			if (!string.IsNullOrEmpty(currentActivityID) && allActivities != null && resultActivities != null && allActivities.ContainsKey(currentActivityID) && depth < MAX_ACTIVITY_TREE_DEPTH && (resultCache == null || !resultCache.DetectAllChildActivitiesProxy(currentActivityID, allActivities, resultActivities, execution, depth)))
			{
				foreach (Activity childActivity in GetChildActivities(currentActivityID, null, allActivities, execution))
				{
					if (!resultActivities.ContainsKey(childActivity.Id))
					{
						resultActivities.Add(childActivity.Id, childActivity);
						DetectAllChildActivities(childActivity.Id, allActivities, resultActivities, execution, depth + 1);
					}
				}
				if (resultCache != null)
				{
					resultCache.DetectAllChildActivitiesResultCache(currentActivityID, allActivities, resultActivities, execution, depth);
				}
			}
		}

		internal static List<Activity> GetChildActivities(string currentActivityID, List<TraceRecord> traces, Dictionary<string, Activity> allActivities, ExecutionInfo execution)
		{
			List<Activity> list = new List<Activity>();
			if (allActivities != null && !string.IsNullOrEmpty(currentActivityID) && allActivities.ContainsKey(currentActivityID))
			{
				if (resultCache != null)
				{
					List<Activity> childActivitiesProxy = resultCache.GetChildActivitiesProxy(currentActivityID, allActivities, execution);
					if (childActivitiesProxy != null)
					{
						return childActivitiesProxy;
					}
				}
				Dictionary<string, Activity> dictionary = new Dictionary<string, Activity>();
				DetectPossibleParentActivities(allActivities[currentActivityID], allActivities, dictionary, INIT_ACTIVITY_TREE_DEPTH2, execution);
				List<string> list2 = new List<string>();
				List<string> list3 = new List<string>();
				traces = ((traces == null) ? InternalLoadTraceRecordWarpper(allActivities[currentActivityID], execution, containsActivityBoundary: false) : traces);
				foreach (TraceRecord trace in traces)
				{
					if ((execution == null || execution.ExecutionID == trace.Execution.ExecutionID) && trace.IsTransfer)
					{
						if (trace.ActivityID == currentActivityID && allActivities.ContainsKey(trace.RelatedActivityID) && !list3.Contains(trace.RelatedActivityID) && !list2.Contains(trace.RelatedActivityID) && !dictionary.ContainsKey(trace.RelatedActivityID))
						{
							list.Add(allActivities[trace.RelatedActivityID]);
							list2.Add(trace.RelatedActivityID);
						}
						if (trace.RelatedActivityID == currentActivityID && allActivities.ContainsKey(trace.ActivityID) && !list3.Contains(trace.ActivityID) && !list2.Contains(trace.ActivityID))
						{
							list3.Add(trace.ActivityID);
						}
					}
				}
				if (resultCache != null)
				{
					resultCache.GetChildActivitiesResultCache(currentActivityID, allActivities, execution, list);
				}
			}
			return list;
		}
	}
}
