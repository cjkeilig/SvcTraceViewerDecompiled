using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceLocationComparer : IComparer<TraceRecordPosition>
	{
		public int Compare(TraceRecordPosition x, TraceRecordPosition y)
		{
			if (x.TraceRecordDateTime < y.TraceRecordDateTime)
			{
				return -1;
			}
			if (x.TraceRecordDateTime > y.TraceRecordDateTime)
			{
				return 1;
			}
			if (x.TypePositionPriority < y.TypePositionPriority)
			{
				return 1;
			}
			if (x.TypePositionPriority > y.TypePositionPriority)
			{
				return -1;
			}
			if (x.RelatedFileDescriptor == y.RelatedFileDescriptor)
			{
				if (x.FileOffset < y.FileOffset)
				{
					return -1;
				}
				if (x.FileOffset > y.FileOffset)
				{
					return 1;
				}
				return 0;
			}
			if (x.RelatedFileDescriptor.FilePath.GetHashCode() < y.RelatedFileDescriptor.FilePath.GetHashCode())
			{
				return -1;
			}
			return 1;
		}
	}
}
