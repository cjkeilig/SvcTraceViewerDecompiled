using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class MessageExchangeCellItem
	{
		private ExecutionColumnItem sentColumnItem;

		private ExecutionColumnItem receiveColumnItem;

		private List<TraceRecordCellItem> relatedMessageTraceCellItems = new List<TraceRecordCellItem>();

		private TraceRecordCellItem sentCellItem;

		private TraceRecordCellItem receiveCellItem;

		internal ExecutionColumnItem SentExecutionColumnItem
		{
			get
			{
				return sentColumnItem;
			}
			set
			{
				sentColumnItem = value;
			}
		}

		internal ExecutionColumnItem ReceiveExecutionColumnItem
		{
			get
			{
				return receiveColumnItem;
			}
			set
			{
				receiveColumnItem = value;
			}
		}

		internal List<TraceRecordCellItem> RelatedMessageTraceCellItems => relatedMessageTraceCellItems;

		internal TraceRecordCellItem SentTraceRecordCellItem
		{
			get
			{
				return sentCellItem;
			}
			set
			{
				sentCellItem = value;
			}
		}

		internal TraceRecordCellItem ReceiveTraceRecordCellItem
		{
			get
			{
				return receiveCellItem;
			}
			set
			{
				receiveCellItem = value;
			}
		}
	}
}
