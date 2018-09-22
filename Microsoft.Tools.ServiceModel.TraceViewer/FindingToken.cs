using System.Collections.Generic;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class FindingToken
	{
		private List<ListViewItem> findActivitySequenceList = new List<ListViewItem>();

		private List<ListViewItem> findTraceRecordLVISequenceList = new List<ListViewItem>();

		private List<TraceRecordCellControl> findTraceRecordTRCSequenceList = new List<TraceRecordCellControl>();

		public List<ListViewItem> FindActivitySequenceList => findActivitySequenceList;

		public List<ListViewItem> FindTraceRecordLVISequenceList => findTraceRecordLVISequenceList;

		internal List<TraceRecordCellControl> FindTraceRecordTRCSequenceList => findTraceRecordTRCSequenceList;
	}
}
