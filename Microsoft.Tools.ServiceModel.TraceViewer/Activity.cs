using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class Activity
	{
		private string guid;

		private string name;

		private int traceCount;

		private bool hasData;

		private DateTime endTime = DateTime.MinValue;

		private DateTime startTime = DateTime.MaxValue;

		private TraceEventType level;

		private List<TraceRecordPosition> traceRecordPositionList = new List<TraceRecordPosition>();

		private bool isMessageActivity;

		private bool hasError;

		private bool hasWarning;

		private TraceDataSource dataSource;

		private ActivityType activityType = ActivityType.NormalActivity;

		private List<string> namedList = new List<string>();

		public ActivityType ActivityType
		{
			get
			{
				if (Id.Contains("{00000000-0000-0000-0000-000000000000}"))
				{
					return ActivityType.RootActivity;
				}
				return activityType;
			}
			set
			{
				activityType = value;
			}
		}

		public bool IsMessageActivity
		{
			get
			{
				return isMessageActivity;
			}
			set
			{
				isMessageActivity = value;
			}
		}

		public bool IsMultipleName => namedList.Count > 1;

		public List<string> NameList => namedList;

		public bool HasError
		{
			get
			{
				return hasError;
			}
			set
			{
				hasError = value;
			}
		}

		public bool HasWarning
		{
			get
			{
				return hasWarning;
			}
			set
			{
				hasWarning = value;
			}
		}

		public List<TraceRecordPosition> TraceRecordPositionList => traceRecordPositionList;

		public bool HasData
		{
			set
			{
				hasData = value;
			}
		}

		public string Id
		{
			get
			{
				return guid;
			}
			set
			{
				guid = value;
			}
		}

		public string Name
		{
			get
			{
				return name;
			}
			set
			{
				name = value;
			}
		}

		public DateTime EndTime
		{
			get
			{
				return endTime;
			}
			set
			{
				endTime = value;
			}
		}

		public DateTime StartTime
		{
			get
			{
				return startTime;
			}
			set
			{
				startTime = value;
			}
		}

		public TraceEventType Level
		{
			get
			{
				return level;
			}
			set
			{
				level = value;
			}
		}

		public int TraceCount
		{
			get
			{
				return traceCount;
			}
			set
			{
				traceCount = value;
			}
		}

		public List<TraceRecord> LoadTraceRecords(bool isLoadActivityBoundary)
		{
			return LoadTraceRecords(isLoadActivityBoundary, null);
		}

		public List<TraceRecord> LoadTraceRecords(bool isLoadActivityBoundary, ExecutionInfo executionInfo)
		{
			List<TraceRecord> result = null;
			if (dataSource != null)
			{
				result = dataSource.LoadTraceRecordsFromActivity(this, isLoadActivityBoundary, executionInfo);
			}
			return result;
		}

		public Activity(TraceDataSource dataSource)
		{
			HasData = false;
			this.dataSource = dataSource;
		}

		public static string ShortID(string guid)
		{
			if (!string.IsNullOrEmpty(guid))
			{
				if (guid[0] == '{' && guid.Length > 37)
				{
					return guid.Substring(25, 12);
				}
				if (guid.Length > 35)
				{
					return guid.Substring(24, 12);
				}
			}
			return guid;
		}
	}
}
