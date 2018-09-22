using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[ObjectStateMachine(typeof(FileEntireLoadingState), true, typeof(FilePartialLoadingState))]
	[ObjectStateMachine(typeof(FilePartialLoadingState), false, typeof(FileEntireLoadingState))]
	[ObjectStateMachine(typeof(SourceFileChangedState), false, typeof(SourceFileUnChangedState), "SourceFileChanged")]
	[ObjectStateMachine(typeof(SourceFileUnChangedState), false, typeof(SourceFileChangedState), "SourceFileChanged")]
	[ObjectStateMachine(typeof(TraceDataSourceIdleState), false, null, "TraceDataSourceState")]
	[ObjectStateMachine(typeof(TraceDataSourceValidatingState), false, typeof(TraceDataSourceIdleState), "TraceDataSourceState")]
	internal class TraceDataSource : IStateAwareObject, IDisposable
	{
		public delegate void MessageLoggedTracesAppended(List<TraceRecord> traces);

		public delegate void MessageLoggedTracesCleared();

		public delegate void ActivitiesAppended(List<Activity> activities);

		public delegate void RemoveFileFinished(string[] filesPath);

		public delegate void RemoveFileBegin(string[] filesPath);

		public delegate void AppendFileBegin(string[] filesPath);

		public delegate void AppendFileFinished(string[] filesPath, TaskInfoBase task);

		public delegate void RemoveAllFileFinished();

		public delegate void ReloadFilesBegin();

		public delegate void ReloadFilesFinished();

		public delegate bool TraceRecordReady(List<TraceRecord> traceRecords, Activity activity, List<Activity> activities);

		public delegate void TraceRecordLoadingFinished(List<Activity> activities);

		public delegate void TraceRecordLoadingBegin(List<Activity> activities);

		public delegate void FileLoadingTimeRangeChanged(DateTime start, DateTime end);

		public delegate void FileLoadingSelectedTimeRangeChanged(DateTime start, DateTime end);

		public delegate void SourceFileModified(string fileNames);

		private class LoadActivitiesFromFileByBlockTask : IDisposable
		{
			private List<FileBlockInfo> blockInfos = new List<FileBlockInfo>();

			private int loadedTraceCount;

			private FileDescriptor fileDesp;

			private FileLoadingTaskInfo task;

			private ManualResetEvent finishEvent;

			private bool isDisposed;

			public List<FileBlockInfo> BlockInfos
			{
				get
				{
					return blockInfos;
				}
				set
				{
					blockInfos = value;
				}
			}

			public FileDescriptor FileDesp
			{
				get
				{
					return fileDesp;
				}
				set
				{
					fileDesp = value;
				}
			}

			public FileLoadingTaskInfo RelatedFileLoadingTask
			{
				get
				{
					return task;
				}
				set
				{
					task = value;
				}
			}

			public ManualResetEvent FinishEvent
			{
				get
				{
					if (finishEvent == null)
					{
						finishEvent = new ManualResetEvent(initialState: false);
					}
					return finishEvent;
				}
			}

			public int LoadedTraceCount
			{
				get
				{
					return loadedTraceCount;
				}
				set
				{
					loadedTraceCount = value;
				}
			}

			public bool IsFinishEventAttached()
			{
				if (finishEvent == null)
				{
					return false;
				}
				return true;
			}

			public void Dispose()
			{
				if (!isDisposed)
				{
					if (finishEvent != null)
					{
						finishEvent.Close();
					}
					isDisposed = true;
					GC.SuppressFinalize(this);
				}
			}

			~LoadActivitiesFromFileByBlockTask()
			{
				if (!isDisposed)
				{
					Dispose();
				}
			}
		}

		public const string ROOT_ACTIVITY_GUID = "{00000000-0000-0000-0000-000000000000}";

		public StateMachineController PartialLoadingStateController;

		public StateMachineController SourceFileChangedStateController;

		public StateMachineController DataSourceStateController;

		private object currentTaskLock = new object();

		private object thisLock = new object();

		private const int BATCH_ACTIVITY_SIZE = 150;

		private const int BATCH_MESSAGE_TRACE_SIZE = 500;

		private const long MULTITHREAD_FILE_LOADING_SIZE = 20000000L;

		private const long PARTIAL_LOADING_SIZE = 40000000L;

		private const double AVERAGE_TRACE_SIZE = 900.0;

		public const int BATCH_TRACE_LOADING_SIZE = 500;

		private const int MAX_THREAD_COUNT = 4;

		private Dictionary<string, Activity> activities = new Dictionary<string, Activity>();

		private int activityCount;

		private int traceCount;

		private XmlUtils xmlUtils;

		private List<Activity> appendedActivitiesCache;

		private List<TraceRecord> appendedMessageTracesCache;

		private List<FileDescriptor> fileDescriptors = new List<FileDescriptor>();

		private IErrorReport errorReport;

		private IProgressReport progressReport;

		private IUserInterfaceProvider userInterfaceProvider;

		private TaskInfoBase currentTask;

		private TraceDataSourceCacheExtension cacheExtension;

		public MessageLoggedTracesAppended MessageLoggedTracesAppendedCallback;

		public MessageLoggedTracesCleared MessageLoggedTracesClearedCallback;

		public ActivitiesAppended ActivitiesAppendedCallback;

		public RemoveFileFinished RemoveFileFinishedCallback;

		public RemoveFileBegin RemoveFileBeginCallback;

		public AppendFileBegin AppendFileBeginCallback;

		public AppendFileFinished AppendFileFinishedCallback;

		public RemoveAllFileFinished RemoveAllFileFinishedCallback;

		public ReloadFilesBegin ReloadFilesBeginCallback;

		public ReloadFilesFinished ReloadFilesFinishedCallback;

		public TraceRecordReady TraceRecordReadyCallback;

		public TraceRecordLoadingFinished TraceRecordLoadingFinishedCallback;

		public TraceRecordLoadingBegin TraceRecordLoadingBeginCallback;

		public FileLoadingTimeRangeChanged FileLoadingTimeRangeChangedCallback;

		public FileLoadingSelectedTimeRangeChanged FileLoadingSelectedTimeRangeChangedCallback;

		public SourceFileModified SourceFileModifiedCallback;

		private FilterCriteria baseFilterCriteria;

		private object ThisLock => thisLock;

		private object CurrentTaskLock => currentTaskLock;

		public Dictionary<string, Activity> Activities => activities;

		public int TraceCount => traceCount;

		public List<string> LoadedFileNames
		{
			get
			{
				List<string> list = new List<string>();
				if (fileDescriptors != null)
				{
					foreach (FileDescriptor fileDescriptor in fileDescriptors)
					{
						list.Add(fileDescriptor.FilePath);
					}
					return list;
				}
				return list;
			}
		}

		private FilterCriteria BaseFilterCriteria
		{
			get
			{
				return baseFilterCriteria;
			}
			set
			{
				baseFilterCriteria = value;
			}
		}

		public DateTimePair TraceTimeRange
		{
			get
			{
				if (fileDescriptors != null && TraceCount != 0)
				{
					return CalculateFileTimeRange(fileDescriptors);
				}
				return null;
			}
		}

		void IStateAwareObject.PreStateSwitch(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		void IStateAwareObject.PostStateSwitch(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		void IStateAwareObject.StateSwitchSuccess(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		void IStateAwareObject.StateSwitchFailed(ObjectStateBase fromState, ObjectStateBase toState, ObjectStateSwitchFailReason reason)
		{
		}

		public void LoadTracesFromActivities(List<Activity> activities)
		{
			CancelCurrentTask();
			if (activities == null || activities.Count == 0)
			{
				Utilities.UIThreadInvokeHelper(userInterfaceProvider, TraceRecordLoadingBeginCallback, null);
				Utilities.UIThreadInvokeHelper(userInterfaceProvider, TraceRecordLoadingFinishedCallback, null);
			}
			else
			{
				TraceLoadingTaskInfo state = new TraceLoadingTaskInfo(activities);
				SetCurrentTask(state);
				ThreadPool.QueueUserWorkItem(TraceLoaderWorkerThreadProc, state);
			}
		}

		public TraceDataSource(IErrorReport errorReport, IProgressReport progressReport, IUserInterfaceProvider userIP)
		{
			if (userIP == null)
			{
				throw new ArgumentException();
			}
			PartialLoadingStateController = new StateMachineController(this);
			SourceFileChangedStateController = new StateMachineController(this, "SourceFileChanged");
			DataSourceStateController = new StateMachineController(this, "TraceDataSourceState");
			SourceFileChangedStateController.SwitchState("SourceFileUnChangedState");
			DataSourceStateController.SwitchState("TraceDataSourceIdleState");
			this.progressReport = progressReport;
			this.errorReport = errorReport;
			xmlUtils = new XmlUtils(errorReport);
			userInterfaceProvider = userIP;
		}

		public void BeginInit()
		{
			cacheExtension = new TraceDataSourceCacheExtension();
			cacheExtension.Attach(this);
		}

		public void EngInit()
		{
			RuntimeHelpers.PrepareDelegate(ActivitiesAppendedCallback);
			RuntimeHelpers.PrepareDelegate(AppendFileBeginCallback);
			RuntimeHelpers.PrepareDelegate(AppendFileFinishedCallback);
			RuntimeHelpers.PrepareDelegate(FileLoadingSelectedTimeRangeChangedCallback);
			RuntimeHelpers.PrepareDelegate(FileLoadingTimeRangeChangedCallback);
			RuntimeHelpers.PrepareDelegate(MessageLoggedTracesAppendedCallback);
			RuntimeHelpers.PrepareDelegate(MessageLoggedTracesClearedCallback);
			RuntimeHelpers.PrepareDelegate(ReloadFilesBeginCallback);
			RuntimeHelpers.PrepareDelegate(ReloadFilesFinishedCallback);
			RuntimeHelpers.PrepareDelegate(RemoveAllFileFinishedCallback);
			RuntimeHelpers.PrepareDelegate(RemoveFileBeginCallback);
			RuntimeHelpers.PrepareDelegate(RemoveFileFinishedCallback);
			RuntimeHelpers.PrepareDelegate(SourceFileModifiedCallback);
			RuntimeHelpers.PrepareDelegate(TraceRecordLoadingBeginCallback);
			RuntimeHelpers.PrepareDelegate(TraceRecordLoadingFinishedCallback);
			RuntimeHelpers.PrepareDelegate(TraceRecordReadyCallback);
		}

		private void AddTraceToActivities(Dictionary<string, Activity> activities, TraceRecord trace, string activityID)
		{
			AddTraceToActivities(activities, trace, activityID, null);
		}

		private void AddTraceToActivities(Dictionary<string, Activity> activities, TraceRecord trace, string activityID, object source)
		{
			Activity activity = null;
			bool flag = false;
			lock (ThisLock)
			{
				if (activities.ContainsKey(activityID))
				{
					activity = activities[activityID];
					if (activity.StartTime > trace.Time)
					{
						activity.StartTime = trace.Time;
					}
					if (activity.EndTime < trace.Time)
					{
						activity.EndTime = trace.Time;
					}
					if ((activity.Level > trace.Level && trace.Level > (TraceEventType)0) || activity.Level == (TraceEventType)0)
					{
						activity.Level = trace.Level;
					}
					activity.TraceCount++;
				}
				else
				{
					activity = new Activity(this);
					activity.Id = activityID;
					activity.Level = trace.Level;
					activity.StartTime = trace.Time;
					activity.EndTime = trace.Time;
					activity.TraceCount++;
					activities.Add(activityID, activity);
					activityCount++;
					flag = true;
				}
				activity.TraceRecordPositionList.Add(trace.TraceRecordPos);
				if (!activity.IsMessageActivity && (trace.IsMessageSentRecord || trace.IsMessageReceivedRecord))
				{
					activity.IsMessageActivity = true;
				}
				if (trace.IsData)
				{
					activity.HasData = true;
				}
				if ((trace.Level & TraceEventType.Error) > (TraceEventType)0 || (trace.Level & TraceEventType.Critical) > (TraceEventType)0)
				{
					activity.HasError = true;
				}
				if ((trace.Level & TraceEventType.Warning) > (TraceEventType)0)
				{
					activity.HasWarning = true;
				}
				if (trace.IsActivityName && !string.IsNullOrEmpty(trace.ActivityName))
				{
					if (string.IsNullOrEmpty(activity.Name))
					{
						activity.Name = trace.ActivityName;
					}
					if (!activity.NameList.Contains(activity.Name))
					{
						activity.NameList.Add(trace.ActivityName);
					}
					if (trace.Level == TraceEventType.Start && trace.ActivityType != ActivityType.UnknownActivity)
					{
						activity.ActivityType = trace.ActivityType;
					}
				}
				if (trace.IsMessageLogged)
				{
					AppendMessageTracesByBatch(trace);
				}
				if (flag)
				{
					AppendActivityByBatch(activity);
				}
				cacheExtension.OnTraceRecordLoadedFromSource(trace);
			}
		}

		private void AddTraceToActivities(Dictionary<string, Activity> activities, TraceRecord trace, object source)
		{
			AddTraceToActivities(activities, trace, trace.ActivityID, source);
			if (trace.RelatedActivityID != null)
			{
				AddTraceToActivities(activities, trace, trace.RelatedActivityID);
			}
		}

		public void Clear()
		{
			Clear(withEvent: true, isCloseStream: true);
			PartialLoadingStateController.SwitchState("FileEntireLoadingState");
			SourceFileChangedStateController.SwitchState("SourceFileUnChangedState");
			progressReport.IndicateProgress(activityCount, TraceCount);
		}

		private void Clear(bool withEvent, bool isCloseStream)
		{
			traceCount = 0;
			activityCount = 0;
			activities = new Dictionary<string, Activity>();
			if (isCloseStream)
			{
				foreach (FileDescriptor fileDescriptor in fileDescriptors)
				{
					fileDescriptor.Close();
				}
			}
			fileDescriptors = new List<FileDescriptor>();
			Utilities.UIThreadInvokeHelper(userInterfaceProvider, MessageLoggedTracesClearedCallback, null);
			if (withEvent)
			{
				Utilities.UIThreadInvokeHelper(userInterfaceProvider, RemoveAllFileFinishedCallback, null);
			}
		}

		public void RefreshDataSource()
		{
			SourceFileChangedStateController.SwitchState("SourceFileUnChangedState");
			if (fileDescriptors.Count != 0)
			{
				List<string> list = new List<string>();
				foreach (FileDescriptor fileDescriptor in fileDescriptors)
				{
					list.Add(fileDescriptor.FilePath);
				}
				string[] array = new string[list.Count];
				list.CopyTo(array);
				Clear();
				AppendFiles(array);
			}
		}

		public void ReloadFiles()
		{
			if (fileDescriptors.Count != 0)
			{
				userInterfaceProvider.InvokeOnUIThread(ReloadFilesBeginCallback);
				FileLoadingTaskInfo state = new FileLoadingTaskInfo(fileDescriptors, false, OnReloadFileLoadingTaskFinished);
				Clear(withEvent: true, isCloseStream: false);
				ThreadPool.QueueUserWorkItem(AddFileThreadProc, state);
			}
		}

		private void OnReloadFileLoadingTaskFinished(TaskInfoBase taskInfo)
		{
			userInterfaceProvider.InvokeOnUIThread(ReloadFilesFinishedCallback);
		}

		private void BeginPushActivitiesByBatch()
		{
			if (appendedActivitiesCache == null)
			{
				EndPushActivitiesByBatch();
			}
			appendedActivitiesCache = new List<Activity>();
		}

		private void BeginPushMessageTracesByBatch()
		{
			appendedMessageTracesCache = new List<TraceRecord>();
		}

		private void EndPushMessageTracesByBatch()
		{
			if (appendedMessageTracesCache != null && appendedMessageTracesCache.Count != 0)
			{
				Utilities.UIThreadInvokeHelper(userInterfaceProvider, MessageLoggedTracesAppendedCallback, appendedMessageTracesCache);
			}
			appendedMessageTracesCache = null;
		}

		private void EndPushActivitiesByBatch()
		{
			if (appendedActivitiesCache != null && appendedActivitiesCache.Count != 0)
			{
				Utilities.UIThreadInvokeHelper(userInterfaceProvider, ActivitiesAppendedCallback, appendedActivitiesCache);
			}
			appendedActivitiesCache = null;
		}

		private void AppendActivityByBatch(Activity activity)
		{
			if (appendedActivitiesCache != null)
			{
				appendedActivitiesCache.Add(activity);
				if (appendedActivitiesCache.Count >= 150)
				{
					Utilities.UIThreadInvokeHelper(userInterfaceProvider, ActivitiesAppendedCallback, appendedActivitiesCache);
					appendedActivitiesCache = new List<Activity>();
				}
			}
		}

		private void AppendMessageTracesByBatch(TraceRecord trace)
		{
			if (appendedMessageTracesCache != null)
			{
				appendedMessageTracesCache.Add(trace);
				if (appendedMessageTracesCache.Count >= 500)
				{
					Utilities.UIThreadInvokeHelper(userInterfaceProvider, MessageLoggedTracesAppendedCallback, appendedMessageTracesCache);
					appendedMessageTracesCache = new List<TraceRecord>();
				}
			}
		}

		private void ResortTracesInActivities()
		{
			foreach (Activity value in activities.Values)
			{
				value.TraceRecordPositionList.Sort(new TraceLocationComparer());
			}
		}

		private void AddFileThreadProc(object o)
		{
			if (o != null)
			{
				SwitchOffFileModificationListener();
				int num = 0;
				int num2 = 0;
				FileLoadingTaskInfo fileLoadingTaskInfo = (FileLoadingTaskInfo)o;
				SetCurrentTask(fileLoadingTaskInfo);
				string[] array = new string[fileLoadingTaskInfo.FileCount];
				foreach (FileDescriptor loadingFileDescriptor in fileLoadingTaskInfo.LoadingFileDescriptors)
				{
					array[num++] = loadingFileDescriptor.FilePath;
				}
				if (fileLoadingTaskInfo.IsEventTrigger)
				{
					Utilities.UIThreadInvokeHelper(userInterfaceProvider, AppendFileBeginCallback, array);
				}
				if (fileLoadingTaskInfo.FileCount == 0)
				{
					if (fileLoadingTaskInfo.IsEventTrigger)
					{
						Utilities.UIThreadInvokeHelper(userInterfaceProvider, AppendFileFinishedCallback, null, fileLoadingTaskInfo);
					}
					fileLoadingTaskInfo.Close();
					SwitchOnFileModificationListener();
				}
				else
				{
					List<string> list = new List<string>();
					BeginPushActivitiesByBatch();
					BeginPushMessageTracesByBatch();
					int expectedSteps = (int)((double)fileLoadingTaskInfo.LoadingFileSize / 900.0);
					progressReport.Begin(expectedSteps);
					foreach (FileDescriptor loadingFileDescriptor2 in fileLoadingTaskInfo.LoadingFileDescriptors)
					{
						if (loadingFileDescriptor2.SelectedBlockFileSize >= 20000000 && loadingFileDescriptor2.SelectedFileBlocks.Count > 1)
						{
							int num4 = 0;
							List<LoadActivitiesFromFileByBlockTask> list2 = new List<LoadActivitiesFromFileByBlockTask>();
							num4 = ((loadingFileDescriptor2.SelectedFileBlocks.Count > 4) ? 4 : loadingFileDescriptor2.SelectedFileBlocks.Count);
							List<ManualResetEvent> list3 = new List<ManualResetEvent>();
							for (int i = 0; i < num4; i++)
							{
								LoadActivitiesFromFileByBlockTask loadActivitiesFromFileByBlockTask = new LoadActivitiesFromFileByBlockTask();
								loadActivitiesFromFileByBlockTask.FileDesp = loadingFileDescriptor2;
								loadActivitiesFromFileByBlockTask.RelatedFileLoadingTask = fileLoadingTaskInfo;
								list3.Add(loadActivitiesFromFileByBlockTask.FinishEvent);
								list2.Add(loadActivitiesFromFileByBlockTask);
							}
							int num5 = 0;
							foreach (FileBlockInfo selectedFileBlock in loadingFileDescriptor2.SelectedFileBlocks)
							{
								if (num5 != num4)
								{
									list2[num5++].BlockInfos.Add(selectedFileBlock);
								}
								else
								{
									num5 = 0;
									list2[num5++].BlockInfos.Add(selectedFileBlock);
								}
							}
							foreach (LoadActivitiesFromFileByBlockTask item in list2)
							{
								ThreadPool.QueueUserWorkItem(LoadActivitiesFromFileByBlockThreadProc, item);
							}
							ManualResetEvent[] array2 = new ManualResetEvent[list3.Count];
							list3.CopyTo(array2);
							WaitHandle.WaitAll(array2);
							foreach (LoadActivitiesFromFileByBlockTask item2 in list2)
							{
								traceCount += item2.LoadedTraceCount;
								num2 += item2.LoadedTraceCount;
								item2.Dispose();
							}
						}
						else
						{
							LoadActivitiesFromFileByBlockTask loadActivitiesFromFileByBlockTask2 = new LoadActivitiesFromFileByBlockTask();
							loadActivitiesFromFileByBlockTask2.FileDesp = loadingFileDescriptor2;
							loadActivitiesFromFileByBlockTask2.BlockInfos = loadingFileDescriptor2.SelectedFileBlocks;
							loadActivitiesFromFileByBlockTask2.RelatedFileLoadingTask = fileLoadingTaskInfo;
							LoadActivitiesFromFileByBlockThreadProc(loadActivitiesFromFileByBlockTask2);
							traceCount += loadActivitiesFromFileByBlockTask2.LoadedTraceCount;
							num2 += loadActivitiesFromFileByBlockTask2.LoadedTraceCount;
							loadActivitiesFromFileByBlockTask2.Dispose();
						}
						list.Add(loadingFileDescriptor2.FilePath);
						fileDescriptors.Add(loadingFileDescriptor2);
						if (fileLoadingTaskInfo.HasSystemException && userInterfaceProvider.ShowMessageBox(SR.GetString("MsgSysExpLF") + loadingFileDescriptor2.FilePath + SR.GetString("MsgReturn2") + fileLoadingTaskInfo.GetSystemException().Message + SR.GetString("MsgSysExpC"), null, MessageBoxIcon.Hand, MessageBoxButtons.YesNo) == DialogResult.No)
						{
							num2 = 0;
							break;
						}
						if (num2 == 0)
						{
							fileLoadingTaskInfo.SaveException(new LogFileException(SR.GetString("MsgNoValidTraceInFile"), loadingFileDescriptor2.FilePath, null));
						}
						num2 = 0;
					}
					progressReport.Complete();
					EndPushActivitiesByBatch();
					EndPushMessageTracesByBatch();
					progressReport.IndicateProgress(activityCount, TraceCount);
					ResortTracesInActivities();
					if (fileLoadingTaskInfo.IsEventTrigger)
					{
						string[] array3 = new string[list.Count];
						list.CopyTo(array3);
						Utilities.UIThreadInvokeHelper(userInterfaceProvider, AppendFileFinishedCallback, array3, fileLoadingTaskInfo);
					}
					fileLoadingTaskInfo.Close();
					SwitchOnFileModificationListener();
				}
			}
		}

		private void SwitchOnFileModificationListener()
		{
			foreach (FileDescriptor fileDescriptor in fileDescriptors)
			{
				fileDescriptor.RegisterFileChangeCallback();
			}
		}

		private void SwitchOffFileModificationListener()
		{
			foreach (FileDescriptor fileDescriptor in fileDescriptors)
			{
				fileDescriptor.UnRegisterFileChangeCallback();
			}
		}

		public void CancelCurrentTask()
		{
			lock (CurrentTaskLock)
			{
				if (currentTask != null)
				{
					currentTask.Cancel();
					currentTask = null;
				}
			}
		}

		private void SetCurrentTask(TaskInfoBase task)
		{
			lock (CurrentTaskLock)
			{
				if (currentTask != null)
				{
					currentTask.Close();
				}
				currentTask = task;
			}
		}

		public void CancelAllTasks()
		{
			CancelCurrentTask();
		}

		private void LoadActivitiesFromFileByBlockThreadProc(object o)
		{
			if (o != null)
			{
				LoadActivitiesFromFileByBlockTask loadActivitiesFromFileByBlockTask = (LoadActivitiesFromFileByBlockTask)o;
				if (string.IsNullOrEmpty(loadActivitiesFromFileByBlockTask.FileDesp.FilePath))
				{
					if (loadActivitiesFromFileByBlockTask.IsFinishEventAttached())
					{
						loadActivitiesFromFileByBlockTask.FinishEvent.Set();
					}
				}
				else
				{
					FileStream fileStream = null;
					RuntimeHelpers.PrepareConstrainedRegions();
					try
					{
						try
						{
							fileStream = Utilities.CreateFileStreamHelper(loadActivitiesFromFileByBlockTask.FileDesp.FilePath);
						}
						catch (LogFileException exception)
						{
							loadActivitiesFromFileByBlockTask.RelatedFileLoadingTask.SaveException(exception);
							if (loadActivitiesFromFileByBlockTask.IsFinishEventAttached())
							{
								loadActivitiesFromFileByBlockTask.FinishEvent.Set();
							}
							return;
						}
						foreach (FileBlockInfo blockInfo in loadActivitiesFromFileByBlockTask.BlockInfos)
						{
							try
							{
								long fileOffset = blockInfo.StartFileOffset;
								long foundTraceFileOffset = 0L;
								long potentialNextTraceOffset = 0L;
								while (foundTraceFileOffset <= blockInfo.EndFileOffset && (loadActivitiesFromFileByBlockTask.RelatedFileLoadingTask == null || !loadActivitiesFromFileByBlockTask.RelatedFileLoadingTask.IsCancelled()))
								{
									TraceRecord firstValidTrace = loadActivitiesFromFileByBlockTask.FileDesp.GetFirstValidTrace(fileStream, fileOffset, out foundTraceFileOffset, out potentialNextTraceOffset, loadActivitiesFromFileByBlockTask.RelatedFileLoadingTask);
									if (firstValidTrace == null || foundTraceFileOffset > blockInfo.EndFileOffset)
									{
										break;
									}
									if ((BaseFilterCriteria == null || FilterEngine.MatchFilter(BaseFilterCriteria, firstValidTrace)) && FilterEngine.MatchFilter(firstValidTrace))
									{
										loadActivitiesFromFileByBlockTask.LoadedTraceCount++;
										AddTraceToActivities(activities, firstValidTrace, fileStream);
									}
									fileOffset = potentialNextTraceOffset;
									progressReport.Step();
								}
							}
							catch (LogFileException exception2)
							{
								loadActivitiesFromFileByBlockTask.RelatedFileLoadingTask.SaveException(exception2);
								return;
							}
							catch (SystemException exception3)
							{
								loadActivitiesFromFileByBlockTask.RelatedFileLoadingTask.SaveException(exception3);
								return;
							}
						}
					}
					catch (Exception e)
					{
						Program.GlobalSeriousExceptionHandler(e);
					}
					finally
					{
						Utilities.CloseStreamWithoutException(fileStream, isFlushStream: false);
						if (loadActivitiesFromFileByBlockTask.IsFinishEventAttached())
						{
							loadActivitiesFromFileByBlockTask.FinishEvent.Set();
						}
					}
				}
			}
		}

		private FileDescriptor GetFileDescriptor(string filePath)
		{
			if (string.IsNullOrEmpty(filePath) || !LoadedFileNames.Contains(filePath))
			{
				return null;
			}
			foreach (FileDescriptor fileDescriptor in fileDescriptors)
			{
				if (fileDescriptor.FilePath == filePath)
				{
					return fileDescriptor;
				}
			}
			return null;
		}

		private void OnSourceFileChanged(string fileName)
		{
			if (SourceFileChangedStateController.CurrentStateName == "SourceFileUnChangedState")
			{
				SourceFileChangedStateController.SwitchState("SourceFileChangedState");
				Utilities.UIThreadInvokeHelper(userInterfaceProvider, SourceFileModifiedCallback, fileName);
			}
		}

		public void AppendFiles(string[] fileNames)
		{
			DataSourceStateController.SwitchState("TraceDataSourceValidatingState");
			try
			{
				List<string> list = new List<string>();
				foreach (string item in fileNames)
				{
					if (!LoadedFileNames.Contains(item))
					{
						list.Add(item);
					}
				}
				List<FileDescriptor> list2;
				if (list.Count != 0)
				{
					list2 = new List<FileDescriptor>();
					foreach (string item2 in list)
					{
						string filePath = item2.Trim().ToLowerInvariant();
						FileDescriptor fileDescriptor = null;
						try
						{
							fileDescriptor = new FileDescriptor(filePath, OnSourceFileChanged, this);
						}
						catch (LogFileException ex)
						{
							errorReport.ReportErrorToUser(ex.Message);
							continue;
						}
						list2.Add(fileDescriptor);
					}
					if (PartialLoadingStateController.CurrentStateName == "FilePartialLoadingState")
					{
						AdpoteFileDescriptorsForPartialLoadingTimeRange(list2);
						List<FileDescriptor> list3 = new List<FileDescriptor>();
						foreach (FileDescriptor fileDescriptor2 in fileDescriptors)
						{
							list3.Add(fileDescriptor2);
						}
						foreach (FileDescriptor item3 in list2)
						{
							list3.Add(item3);
						}
						FirePartialLoadingEvents(list3);
						goto IL_0223;
					}
					if (!TestPartialLoadingStateForAppend(list2))
					{
						AdopteFileDescriptorsForAllBlocks(list2);
						BaseFilterCriteria = null;
						goto IL_0223;
					}
					foreach (FileDescriptor fileDescriptor3 in fileDescriptors)
					{
						list2.Add(fileDescriptor3);
					}
					if (ResetPartialLoadingTimeRangeByDialog(list2))
					{
						AdpoteFileDescriptorsForPartialLoadingTimeRange(list2);
						Clear(withEvent: true, isCloseStream: false);
						PartialLoadingStateController.SwitchState("FilePartialLoadingState");
						FirePartialLoadingEvents(list2);
						goto IL_0223;
					}
					foreach (FileDescriptor item4 in list2)
					{
						item4.Close();
					}
				}
				goto end_IL_0011;
				IL_0223:
				FileLoadingTaskInfo state = new FileLoadingTaskInfo(list2);
				ThreadPool.QueueUserWorkItem(AddFileThreadProc, state);
				end_IL_0011:;
			}
			finally
			{
				DataSourceStateController.SwitchState("TraceDataSourceIdleState");
			}
		}

		private void FirePartialLoadingEvents(List<FileDescriptor> fileDescriptors)
		{
			if (PartialLoadingStateController.CurrentStateName == "FilePartialLoadingState")
			{
				DateTimePair dateTimePair = CalculateFileTimeRange(fileDescriptors);
				if (dateTimePair != null)
				{
					Utilities.UIThreadInvokeHelper(userInterfaceProvider, FileLoadingTimeRangeChangedCallback, dateTimePair.StartTime, dateTimePair.EndTime);
				}
				if (BaseFilterCriteria != null && BaseFilterCriteria.SearchOption == SearchOptions.TimeRange && BaseFilterCriteria.searchCondition != null)
				{
					DateTimePair dateTimePair2 = (DateTimePair)BaseFilterCriteria.searchCondition;
					Utilities.UIThreadInvokeHelper(userInterfaceProvider, FileLoadingSelectedTimeRangeChangedCallback, dateTimePair2.StartTime, dateTimePair2.EndTime);
				}
			}
		}

		private void AdopteFileDescriptorsForAllBlocks(List<FileDescriptor> fileDescriptors)
		{
			foreach (FileDescriptor fileDescriptor in fileDescriptors)
			{
				foreach (FileBlockInfo fileBlock in fileDescriptor.FileBlocks)
				{
					fileDescriptor.SelectFileBlock(fileBlock);
				}
			}
		}

		private void AdpoteFileDescriptorsForPartialLoadingTimeRange(List<FileDescriptor> fileDescriptors)
		{
			if (BaseFilterCriteria != null && BaseFilterCriteria.SearchOption == SearchOptions.TimeRange)
			{
				DateTimePair dateTimePair = (DateTimePair)BaseFilterCriteria.searchCondition;
				foreach (FileDescriptor fileDescriptor in fileDescriptors)
				{
					RefreshFileDescriptorSelectedBlocksByTimeRange(fileDescriptor, dateTimePair.StartTime, dateTimePair.EndTime);
				}
			}
		}

		public void ResetPartialLoadingTimeRange(DateTime start, DateTime end)
		{
			ResetPartialLoadingTimeRange(new DateTimePair(start, end));
			FirePartialLoadingEvents(fileDescriptors);
			AdpoteFileDescriptorsForPartialLoadingTimeRange(fileDescriptors);
			ReloadFiles();
		}

		private void ResetPartialLoadingTimeRange(DateTimePair dtPair)
		{
			FilterCriteria filterCriteria = new FilterCriteria();
			filterCriteria.FilterSourceLevel = SourceLevels.All;
			filterCriteria.SearchOption = SearchOptions.TimeRange;
			filterCriteria.searchCondition = dtPair;
			BaseFilterCriteria = filterCriteria;
		}

		private bool ResetPartialLoadingTimeRangeByDialog(List<FileDescriptor> fileDesps)
		{
			if (fileDesps == null)
			{
				return false;
			}
			DTRangeDialog dTRangeDialog = new DTRangeDialog();
			dTRangeDialog.Initialize(fileDesps);
			dTRangeDialog.ShowDialog();
			if (dTRangeDialog.DialogResult == DialogResult.Cancel)
			{
				return false;
			}
			ResetPartialLoadingTimeRange(dTRangeDialog.SelectedDateTime);
			return true;
		}

		public void RemoveFiles(string[] fileNames)
		{
			DataSourceStateController.SwitchState("TraceDataSourceValidatingState");
			try
			{
				if (LoadedFileNames.Count != 0)
				{
					List<FileDescriptor> list = new List<FileDescriptor>();
					foreach (string filePath in fileNames)
					{
						FileDescriptor fileDescriptor = GetFileDescriptor(filePath);
						if (fileDescriptor != null)
						{
							list.Add(fileDescriptor);
						}
					}
					List<FileDescriptor> list2 = new List<FileDescriptor>();
					foreach (FileDescriptor fileDescriptor2 in fileDescriptors)
					{
						if (!list.Contains(fileDescriptor2))
						{
							list2.Add(fileDescriptor2);
						}
					}
					if (SourceFileChangedStateController.CurrentStateName == "SourceFileChangedState" && list2.Count == 0)
					{
						SourceFileChangedStateController.SwitchState("SourceFileUnChangedState");
					}
					if (PartialLoadingStateController.CurrentStateName == "FilePartialLoadingState")
					{
						if (!TestPartialLoadingStateForRemove(list))
						{
							AdopteFileDescriptorsForAllBlocks(list2);
							PartialLoadingStateController.SwitchState("FileEntireLoadingState");
							BaseFilterCriteria = null;
						}
						else if (BaseFilterCriteria != null && BaseFilterCriteria.SearchOption == SearchOptions.TimeRange && BaseFilterCriteria.searchCondition != null)
						{
							DateTimePair dateTimePair = CalculateFileTimeRange(list2);
							if (dateTimePair != null)
							{
								DateTimePair dateTimePair2 = (DateTimePair)BaseFilterCriteria.searchCondition;
								DateTime startTime = dateTimePair2.StartTime;
								DateTime endTime = dateTimePair2.EndTime;
								if (dateTimePair2.StartTime < dateTimePair.StartTime)
								{
									startTime = dateTimePair.StartTime;
									endTime = ((dateTimePair2.EndTime < startTime) ? startTime : ((!(dateTimePair2.EndTime > startTime) || !(dateTimePair2.EndTime > dateTimePair.EndTime)) ? dateTimePair2.EndTime : dateTimePair.EndTime));
								}
								else if (dateTimePair2.StartTime >= dateTimePair.StartTime && dateTimePair2.StartTime <= dateTimePair.EndTime)
								{
									startTime = dateTimePair2.StartTime;
									endTime = ((dateTimePair2.EndTime < startTime) ? startTime : ((!(dateTimePair2.EndTime > startTime) || !(dateTimePair2.EndTime > dateTimePair.EndTime)) ? dateTimePair2.EndTime : dateTimePair.EndTime));
								}
								else
								{
									startTime = dateTimePair.EndTime;
									endTime = startTime;
								}
								ResetPartialLoadingTimeRange(new DateTimePair(startTime, endTime));
							}
						}
					}
					FirePartialLoadingEvents(list2);
					ThreadPool.QueueUserWorkItem(RemoveFileThreadProc, list);
				}
			}
			finally
			{
				DataSourceStateController.SwitchState("TraceDataSourceIdleState");
			}
		}

		private void RemoveFileThreadProc(object o)
		{
			List<FileDescriptor> list = (List<FileDescriptor>)o;
			string[] array = new string[list.Count];
			int num = 0;
			foreach (FileDescriptor item in list)
			{
				array[num++] = item.FilePath;
			}
			Utilities.UIThreadInvokeHelper(userInterfaceProvider, RemoveFileBeginCallback, array);
			if (list == null || list.Count == 0)
			{
				Utilities.UIThreadInvokeHelper(userInterfaceProvider, RemoveFileFinishedCallback, null);
			}
			else
			{
				foreach (FileDescriptor item2 in list)
				{
					FileDescriptor fileDescriptor = GetFileDescriptor(item2.FilePath);
					if (fileDescriptor != null)
					{
						fileDescriptor.Close();
						fileDescriptors.Remove(fileDescriptor);
					}
				}
				List<FileDescriptor> list2 = fileDescriptors;
				Clear(withEvent: false, isCloseStream: false);
				if (list2.Count != 0)
				{
					FileLoadingTaskInfo fileLoadingTaskInfo = new FileLoadingTaskInfo(list2);
					fileLoadingTaskInfo.IsEventTrigger = false;
					AddFileThreadProc(fileLoadingTaskInfo);
				}
				Utilities.UIThreadInvokeHelper(userInterfaceProvider, RemoveFileFinishedCallback, LoadedFileNames.ToArray());
				progressReport.IndicateProgress(activityCount, TraceCount);
			}
		}

		public List<TraceRecord> LoadTraceRecordsFromActivity(Activity activity, bool isLoadActivityBoundary, ExecutionInfo executionInfo)
		{
			List<TraceRecord> list = new List<TraceRecord>();
			if (activity != null)
			{
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					foreach (TraceRecordPosition traceRecordPosition in activity.TraceRecordPositionList)
					{
						TraceRecord traceRecord = cacheExtension.TryAndGetTraceRecord(traceRecordPosition);
						if (traceRecord == null)
						{
							long foundTraceFileOffset = 0L;
							FileStream cachedFileStream = traceRecordPosition.RelatedFileDescriptor.GetCachedFileStream();
							if (cachedFileStream != null)
							{
								traceRecord = traceRecordPosition.RelatedFileDescriptor.GetFirstValidTrace(cachedFileStream, traceRecordPosition.FileOffset, out foundTraceFileOffset);
								cacheExtension.OnTraceRecordLoadedFromSource(traceRecord);
							}
						}
						if (traceRecord != null && (isLoadActivityBoundary || (!traceRecord.IsActivityBoundary && (executionInfo == null || traceRecord.Execution.ExecutionID == executionInfo.ExecutionID))))
						{
							list.Add(traceRecord);
						}
					}
					return list;
				}
				catch (Exception e)
				{
					Program.GlobalSeriousExceptionHandler(e);
					return list;
				}
			}
			return list;
		}

		private void TraceLoaderWorkerThreadProc(object o)
		{
			if (o != null)
			{
				TraceLoadingTaskInfo traceLoadingTaskInfo = (TraceLoadingTaskInfo)o;
				if (!traceLoadingTaskInfo.IsCancelled() && traceLoadingTaskInfo.LoadingActivities != null && traceLoadingTaskInfo.LoadingActivities.Count != 0)
				{
					Utilities.UIThreadInvokeHelper(userInterfaceProvider, TraceRecordLoadingBeginCallback, traceLoadingTaskInfo.LoadingActivities);
					RuntimeHelpers.PrepareConstrainedRegions();
					try
					{
						int num = 0;
						Activity activity = null;
						while (num < traceLoadingTaskInfo.LoadingActivities.Count)
						{
							try
							{
								activity = traceLoadingTaskInfo.LoadingActivities[num++];
								List<TraceRecord> list = new List<TraceRecord>();
								foreach (TraceRecordPosition traceRecordPosition in activity.TraceRecordPositionList)
								{
									if (traceLoadingTaskInfo.IsCancelled())
									{
										break;
									}
									TraceRecord traceRecord = cacheExtension.TryAndGetTraceRecord(traceRecordPosition);
									if (traceRecord == null)
									{
										FileStream cachedFileStream = traceRecordPosition.RelatedFileDescriptor.GetCachedFileStream();
										if (cachedFileStream != null)
										{
											long foundTraceFileOffset = 0L;
											cachedFileStream.Seek(traceRecordPosition.FileOffset, SeekOrigin.Begin);
											traceRecord = traceRecordPosition.RelatedFileDescriptor.GetFirstValidTrace(cachedFileStream, traceRecordPosition.FileOffset, out foundTraceFileOffset);
											cacheExtension.OnTraceRecordLoadedFromSource(traceRecord);
										}
									}
									if (traceRecord != null)
									{
										list.Add(traceRecord);
										if (list.Count >= 500)
										{
											Utilities.UIThreadInvokeHelper(userInterfaceProvider, TraceRecordReadyCallback, list, activity, traceLoadingTaskInfo.LoadingActivities);
											list = new List<TraceRecord>();
										}
									}
								}
								if (traceLoadingTaskInfo.IsCancelled())
								{
									return;
								}
								Utilities.UIThreadInvokeHelper(userInterfaceProvider, TraceRecordReadyCallback, list, activity, traceLoadingTaskInfo.LoadingActivities);
							}
							catch (SystemException)
							{
								if (activity != null && traceLoadingTaskInfo != null)
								{
									SystemException systemException = traceLoadingTaskInfo.GetSystemException();
									if (systemException != null && userInterfaceProvider.ShowMessageBox(SR.GetString("MsgSysExpLA1") + (string.IsNullOrEmpty(activity.Name) ? activity.Id : activity.Name) + SR.GetString("MsgReturn2") + systemException.Message + SR.GetString("MsgSysExpC"), null, MessageBoxIcon.Hand, MessageBoxButtons.YesNo) == DialogResult.No)
									{
										return;
									}
								}
							}
						}
					}
					catch (Exception e)
					{
						traceLoadingTaskInfo.Cancel();
						Program.GlobalSeriousExceptionHandler(e);
					}
					finally
					{
						traceLoadingTaskInfo.Close();
						Utilities.UIThreadInvokeHelper(userInterfaceProvider, TraceRecordLoadingFinishedCallback, traceLoadingTaskInfo.LoadingActivities);
					}
				}
			}
		}

		private bool TestPartialLoadingStateForAppend(List<FileDescriptor> fileDesps)
		{
			if (fileDesps == null)
			{
				return PartialLoadingStateController.CurrentStateName == "FilePartialLoadingState";
			}
			long num = 0L;
			foreach (FileDescriptor fileDescriptor in fileDescriptors)
			{
				num += fileDescriptor.FileSize;
			}
			foreach (FileDescriptor fileDesp in fileDesps)
			{
				num += fileDesp.FileSize;
			}
			if (num > 40000000)
			{
				return true;
			}
			return false;
		}

		private bool TestPartialLoadingStateForRemove(List<FileDescriptor> fileDesps)
		{
			if (fileDesps == null)
			{
				return PartialLoadingStateController.CurrentStateName == "FilePartialLoadingState";
			}
			long num = 0L;
			foreach (FileDescriptor fileDescriptor in fileDescriptors)
			{
				num += fileDescriptor.FileSize;
			}
			foreach (FileDescriptor fileDesp in fileDesps)
			{
				num -= fileDesp.FileSize;
			}
			if (num > 40000000)
			{
				return true;
			}
			return false;
		}

		public static DateTimePair CalculateFileTimeRange(List<FileDescriptor> fileDescriptors)
		{
			if (fileDescriptors == null || fileDescriptors.Count == 0)
			{
				return null;
			}
			DateTime dateTime = DateTime.MaxValue;
			DateTime t = DateTime.MinValue;
			foreach (FileDescriptor fileDescriptor in fileDescriptors)
			{
				if (fileDescriptor != null && fileDescriptor.FileBlockCount != 0)
				{
					foreach (FileBlockInfo fileBlock in fileDescriptor.FileBlocks)
					{
						if (fileBlock != null)
						{
							if (fileBlock.StartDate < dateTime)
							{
								dateTime = fileBlock.StartDate;
							}
							if (fileBlock.EndDate > t)
							{
								t = fileBlock.EndDate;
							}
						}
					}
				}
			}
			return new DateTimePair(Utilities.RoundDateTimeToSecond(dateTime), Utilities.RoundDateTimeToSecond(t.AddSeconds(1.0)));
		}

		internal static void RefreshFileDescriptorSelectedBlocksByTimeRange(FileDescriptor fileDesp, DateTime start, DateTime end)
		{
			if (fileDesp != null && start <= end)
			{
				fileDesp.ClearSelectedFileBlocks();
				TimeSpan timeSpan = end - start;
				if (timeSpan.Days != 0 || timeSpan.Hours != 0 || timeSpan.Minutes != 0 || timeSpan.Seconds != 0)
				{
					List<FileBlockInfo> list = new List<FileBlockInfo>();
					foreach (FileBlockInfo fileBlock in fileDesp.FileBlocks)
					{
						if (fileBlock != null)
						{
							if (fileBlock.StartDate >= start && fileBlock.EndDate <= end && !list.Contains(fileBlock))
							{
								list.Add(fileBlock);
							}
							else if (fileBlock.StartDate <= start && fileBlock.EndDate >= start && !list.Contains(fileBlock))
							{
								list.Add(fileBlock);
							}
							else if (fileBlock.StartDate <= end && fileBlock.EndDate >= end && !list.Contains(fileBlock))
							{
								list.Add(fileBlock);
							}
						}
					}
					foreach (FileBlockInfo item in list)
					{
						fileDesp.SelectFileBlock(item);
					}
				}
			}
		}

		public void Dispose()
		{
			if (cacheExtension != null)
			{
				cacheExtension.Detech();
			}
		}
	}
}
