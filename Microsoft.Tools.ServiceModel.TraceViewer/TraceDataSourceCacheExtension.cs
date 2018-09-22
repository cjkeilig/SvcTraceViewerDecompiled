using System;
using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceDataSourceCacheExtension
	{
		private class InternalTraceRecordQueueItem
		{
			public string filePath;

			public long fileOffset = -1L;

			public InternalTraceRecordQueueItem(string filePath, long fileOffset)
			{
				this.fileOffset = fileOffset;
				this.filePath = filePath;
			}
		}

		private Dictionary<string, Dictionary<long, TraceRecord>> internalCachedTraceRecords;

		private Queue<InternalTraceRecordQueueItem> internalCachedTraceRecordQueue;

		private Dictionary<int, List<TraceRecord>> internalTraceRecordListCache;

		private object thisLock = new object();

		private TraceDataSource relatedDataSource;

		private const int MAX_TRACE_CACHE_SIZE = 10000;

		private object ThisLock => thisLock;

		public void Attach(TraceDataSource dataSource)
		{
			relatedDataSource = dataSource;
			internalCachedTraceRecords = new Dictionary<string, Dictionary<long, TraceRecord>>();
			internalCachedTraceRecordQueue = new Queue<InternalTraceRecordQueueItem>();
			internalTraceRecordListCache = new Dictionary<int, List<TraceRecord>>();
			TraceDataSource traceDataSource = relatedDataSource;
			traceDataSource.AppendFileBeginCallback = (TraceDataSource.AppendFileBegin)Delegate.Combine(traceDataSource.AppendFileBeginCallback, new TraceDataSource.AppendFileBegin(DataSource_OnAppendFilesBegin));
			traceDataSource = relatedDataSource;
			traceDataSource.ReloadFilesBeginCallback = (TraceDataSource.ReloadFilesBegin)Delegate.Combine(traceDataSource.ReloadFilesBeginCallback, new TraceDataSource.ReloadFilesBegin(DataSource_OnReloadFilesBegin));
			traceDataSource = relatedDataSource;
			traceDataSource.RemoveAllFileFinishedCallback = (TraceDataSource.RemoveAllFileFinished)Delegate.Combine(traceDataSource.RemoveAllFileFinishedCallback, new TraceDataSource.RemoveAllFileFinished(DataSource_OnRemoveAllFileFinished));
			traceDataSource = relatedDataSource;
			traceDataSource.RemoveFileBeginCallback = (TraceDataSource.RemoveFileBegin)Delegate.Combine(traceDataSource.RemoveFileBeginCallback, new TraceDataSource.RemoveFileBegin(DataSource_OnRemoveFilesBegin));
		}

		public void Detech()
		{
			InvalidateCache();
			internalCachedTraceRecords = null;
		}

		private void InternalInvalidateCacheForFiles(string[] fileNames)
		{
			if (fileNames != null && fileNames.Length != 0)
			{
				foreach (string path in fileNames)
				{
					InvalidateCache(path);
				}
			}
			internalTraceRecordListCache.Clear();
		}

		private void DataSource_OnAppendFilesBegin(string[] fileNames)
		{
			InternalInvalidateCacheForFiles(fileNames);
		}

		private void DataSource_OnReloadFilesBegin()
		{
			InvalidateCache();
		}

		private void DataSource_OnRemoveAllFileFinished()
		{
			InvalidateCache();
		}

		private void DataSource_OnRemoveFilesBegin(string[] fileNames)
		{
			InternalInvalidateCacheForFiles(fileNames);
		}

		public TraceRecord TryAndGetTraceRecord(TraceRecordPosition pos)
		{
			lock (ThisLock)
			{
				TraceRecord result = null;
				if (internalCachedTraceRecords != null && pos != null && pos.RelatedFileDescriptor != null && internalCachedTraceRecords.ContainsKey(pos.RelatedFileDescriptor.FilePath) && internalCachedTraceRecords[pos.RelatedFileDescriptor.FilePath].ContainsKey(pos.FileOffset))
				{
					result = internalCachedTraceRecords[pos.RelatedFileDescriptor.FilePath][pos.FileOffset];
				}
				return result;
			}
		}

		public void OnTraceRecordLoadedFromSource(TraceRecord trace)
		{
			lock (ThisLock)
			{
				if (trace != null && internalCachedTraceRecords != null && trace != null && trace.TraceRecordPos != null)
				{
					if (!internalCachedTraceRecords.ContainsKey(trace.FileDescriptor.FilePath))
					{
						internalCachedTraceRecords.Add(trace.FileDescriptor.FilePath, new Dictionary<long, TraceRecord>());
					}
					if (internalCachedTraceRecords[trace.FileDescriptor.FilePath].ContainsKey(trace.TraceRecordPos.FileOffset))
					{
						internalCachedTraceRecords[trace.FileDescriptor.FilePath][trace.TraceRecordPos.FileOffset] = trace;
					}
					else
					{
						if (internalCachedTraceRecordQueue != null && internalCachedTraceRecordQueue.Count >= 10000)
						{
							InternalTraceRecordQueueItem internalTraceRecordQueueItem = internalCachedTraceRecordQueue.Dequeue();
							if (internalCachedTraceRecords.ContainsKey(internalTraceRecordQueueItem.filePath) && internalCachedTraceRecords[internalTraceRecordQueueItem.filePath].ContainsKey(internalTraceRecordQueueItem.fileOffset))
							{
								internalCachedTraceRecords[internalTraceRecordQueueItem.filePath].Remove(internalTraceRecordQueueItem.fileOffset);
							}
						}
						internalCachedTraceRecords[trace.FileDescriptor.FilePath].Add(trace.TraceRecordPos.FileOffset, trace);
						internalCachedTraceRecordQueue.Enqueue(new InternalTraceRecordQueueItem(trace.FileDescriptor.FilePath, trace.TraceRecordPos.FileOffset));
					}
				}
			}
		}

		private void InvalidateCache(string path)
		{
			lock (ThisLock)
			{
				if (internalCachedTraceRecords != null && !string.IsNullOrEmpty(path) && internalCachedTraceRecords.ContainsKey(path))
				{
					Dictionary<long, TraceRecord> dictionary = internalCachedTraceRecords[path];
					Queue<InternalTraceRecordQueueItem> queue = internalCachedTraceRecordQueue;
					internalCachedTraceRecordQueue = new Queue<InternalTraceRecordQueueItem>();
					while (queue.Count != 0)
					{
						InternalTraceRecordQueueItem internalTraceRecordQueueItem = queue.Dequeue();
						if (internalTraceRecordQueueItem.filePath != path)
						{
							internalCachedTraceRecordQueue.Enqueue(internalTraceRecordQueueItem);
						}
					}
					internalCachedTraceRecords[path].Clear();
					internalCachedTraceRecords.Remove(path);
					internalTraceRecordListCache.Clear();
				}
			}
		}

		private void InvalidateCache()
		{
			lock (ThisLock)
			{
				if (internalCachedTraceRecords != null)
				{
					foreach (Dictionary<long, TraceRecord> value in internalCachedTraceRecords.Values)
					{
						value?.Clear();
					}
					internalCachedTraceRecords.Clear();
					internalCachedTraceRecordQueue.Clear();
					internalTraceRecordListCache.Clear();
				}
			}
		}
	}
}
