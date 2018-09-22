using System;
using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class HorzBundRowItem
	{
		private List<TraceRecordCellItem> traceRecordCellItems;

		private DateTime date = DateTime.MinValue;

		public DateTime Date => date;

		internal List<TraceRecordCellItem> TraceRecordCellItems => traceRecordCellItems;

		public HorzBundRowItem(List<TraceRecordCellItem> items, List<ExecutionColumnItem> executionColumns)
		{
			if (items != null)
			{
				traceRecordCellItems = items;
				if (items.Count != 0)
				{
					date = items[0].CurrentTraceRecord.Time;
				}
			}
		}
	}
}
