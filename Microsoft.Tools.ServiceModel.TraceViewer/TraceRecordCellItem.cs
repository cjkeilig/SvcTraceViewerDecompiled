using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceRecordCellItem
	{
		private ActivityColumnItem relatedActivityItem;

		private TraceRecord currentTraceRecord;

		private TraceRecordCellItem relatedTraceRecordCellItem;

		private bool isParentTransferTraceRecord;

		private bool isToChildControl;

		private bool isAnalyzedToChildControl;

		private ActivityTraceModeAnalyzer analyzer;

		private TraceRecordSetSeverityLevel severityLevel;

		internal TraceRecordSetSeverityLevel SeverityLevel
		{
			get
			{
				return severityLevel;
			}
			set
			{
				severityLevel = value;
			}
		}

		internal ActivityTraceModeAnalyzer Analyzer => analyzer;

		public bool IsParentTransferTrace
		{
			get
			{
				return isParentTransferTraceRecord;
			}
			set
			{
				isParentTransferTraceRecord = value;
			}
		}

		public bool IsToChildTransferTrace
		{
			get
			{
				if (isAnalyzedToChildControl)
				{
					return isToChildControl;
				}
				if (CurrentTraceRecord != null && CurrentTraceRecord.IsTransfer && CurrentTraceRecord.ActivityID == RelatedActivityItem.CurrentActivity.Id)
				{
					TraceRecord directParentActivityTransferInTrace = ActivityAnalyzerHelper.GetDirectParentActivityTransferInTrace(RelatedActivityItem.CurrentActivity.Id, null, CurrentTraceRecord.DataSource.Activities, RelatedExecutionItem.CurrentExecutionInfo);
					if (directParentActivityTransferInTrace != null && directParentActivityTransferInTrace.ActivityID == CurrentTraceRecord.RelatedActivityID)
					{
						isAnalyzedToChildControl = true;
						isToChildControl = false;
						return isToChildControl;
					}
					if (ActivityAnalyzerHelper.DetectDirectRelationshipBetweenActivities(CurrentTraceRecord.DataSource.Activities[CurrentTraceRecord.ActivityID], CurrentTraceRecord.DataSource.Activities[CurrentTraceRecord.RelatedActivityID], RelatedExecutionItem.CurrentExecutionInfo) != 0 && RelatedExecutionItem[CurrentTraceRecord.RelatedActivityID] != null)
					{
						isAnalyzedToChildControl = true;
						isToChildControl = false;
						return isToChildControl;
					}
					List<Activity> childActivities = ActivityAnalyzerHelper.GetChildActivities(RelatedActivityItem.CurrentActivity.Id, null, CurrentTraceRecord.DataSource.Activities, RelatedExecutionItem.CurrentExecutionInfo);
					if (childActivities != null && childActivities.Count != 0)
					{
						foreach (Activity item in childActivities)
						{
							if (item.Id == RelatedActivityItem.CurrentActivity.Id)
							{
								isAnalyzedToChildControl = true;
								isToChildControl = false;
								return isToChildControl;
							}
						}
						isAnalyzedToChildControl = true;
						isToChildControl = true;
						return isToChildControl;
					}
				}
				isAnalyzedToChildControl = true;
				isToChildControl = false;
				return isToChildControl;
			}
		}

		internal TraceRecordCellItem RelatedTraceRecordCellItem
		{
			get
			{
				return relatedTraceRecordCellItem;
			}
			set
			{
				relatedTraceRecordCellItem = value;
			}
		}

		internal TraceRecord CurrentTraceRecord => currentTraceRecord;

		internal ActivityColumnItem RelatedActivityItem => relatedActivityItem;

		internal ExecutionColumnItem RelatedExecutionItem => RelatedActivityItem.RelatedExecutionItem;

		public TraceRecordCellItem(TraceRecord trace, ActivityColumnItem item, ActivityTraceModeAnalyzer analyzer)
		{
			currentTraceRecord = trace;
			relatedActivityItem = item;
			this.analyzer = analyzer;
		}
	}
}
