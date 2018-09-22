using System;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class FileBlockInfo
	{
		private long startFileOffset = -1L;

		private long endFileOffset = -1L;

		private DateTime startDate = DateTime.MaxValue;

		private DateTime endDate = DateTime.MinValue;

		public DateTime StartDate
		{
			get
			{
				return startDate;
			}
			set
			{
				startDate = value;
			}
		}

		public DateTime EndDate
		{
			get
			{
				return endDate;
			}
			set
			{
				endDate = value;
			}
		}

		public long StartFileOffset
		{
			get
			{
				return startFileOffset;
			}
			set
			{
				startFileOffset = value;
			}
		}

		public long EndFileOffset
		{
			get
			{
				return endFileOffset;
			}
			set
			{
				endFileOffset = value;
			}
		}
	}
}
