using System;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class ListViewItemTagComparer : ListViewItemStringComparer
	{
		private ListViewItemTagComparerTarget tagComparison = ListViewItemTagComparerTarget.TraceTime;

		private static TraceLocationComparer traceLocationComparer = new TraceLocationComparer();

		public ListViewItemTagComparer(bool isAsc, ListViewItemTagComparerTarget tagComp)
			: base(-1, isAsc)
		{
			tagComparison = tagComp;
		}

		protected override int Compare(object x, object y)
		{
			int result = 0;
			try
			{
				if (tagComparison == ListViewItemTagComparerTarget.TraceTime && x is TraceRecord && y is TraceRecord)
				{
					result = traceLocationComparer.Compare(((TraceRecord)x).TraceRecordPos, ((TraceRecord)y).TraceRecordPos);
					int result2;
					if (!base.IsAscendingSortOrder)
					{
						switch (result)
						{
						default:
							result2 = 1;
							goto IL_0055;
						case 1:
							result2 = -1;
							goto IL_0055;
						case 0:
							{
								return result;
							}
							IL_0055:
							return result2;
						}
					}
					return result;
				}
				if (!(x is Activity))
				{
					return result;
				}
				if (y is Activity)
				{
					DateTime startTime = ((Activity)x).StartTime;
					DateTime endTime = ((Activity)x).EndTime;
					DateTime startTime2 = ((Activity)y).StartTime;
					DateTime endTime2 = ((Activity)y).EndTime;
					switch (tagComparison)
					{
					case ListViewItemTagComparerTarget.ActivityDuration:
						if (!(endTime >= startTime))
						{
							return result;
						}
						if (endTime2 >= startTime2)
						{
							TimeSpan t = endTime - startTime;
							TimeSpan t2 = endTime2 - startTime2;
							if (t == t2)
							{
								return 0;
							}
							if (t > t2)
							{
								return base.IsAscendingSortOrder ? 1 : (-1);
							}
							return (!base.IsAscendingSortOrder) ? 1 : (-1);
						}
						return result;
					case ListViewItemTagComparerTarget.ActivityStartTime:
						if (startTime == startTime2)
						{
							if (endTime > endTime2)
							{
								return -1;
							}
							if (endTime < endTime2)
							{
								return 1;
							}
							return 0;
						}
						if (startTime > startTime2)
						{
							return base.IsAscendingSortOrder ? 1 : (-1);
						}
						return (!base.IsAscendingSortOrder) ? 1 : (-1);
					case ListViewItemTagComparerTarget.ActivityEndTime:
						if (endTime == endTime2)
						{
							if (startTime < startTime2)
							{
								return -1;
							}
							if (startTime > startTime2)
							{
								return 1;
							}
							return 0;
						}
						if (endTime > endTime2)
						{
							return base.IsAscendingSortOrder ? 1 : (-1);
						}
						return (!base.IsAscendingSortOrder) ? 1 : (-1);
					default:
						return result;
					}
				}
				return result;
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
				return 0;
			}
		}
	}
}
