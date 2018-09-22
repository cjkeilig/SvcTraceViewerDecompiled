using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceRecordComparer : IComparer<TraceRecord>
	{
		public int Compare(TraceRecord x, TraceRecord y)
		{
			return new TraceLocationComparer().Compare(x.TraceRecordPos, y.TraceRecordPos);
		}
	}
}
