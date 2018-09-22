using System;
using System.Globalization;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class ListViewItemNumberComparer : ListViewItemComparer
	{
		public ListViewItemNumberComparer()
		{
		}

		public ListViewItemNumberComparer(int column, bool isAsc)
			: base(column, isAsc)
		{
		}

		protected override int Compare(object x, object y)
		{
			int result = 0;
			try
			{
				if (x == null)
				{
					return result;
				}
				if (y == null)
				{
					return result;
				}
				long num = long.Parse((string)x, CultureInfo.InvariantCulture);
				long num2 = long.Parse((string)y, CultureInfo.InvariantCulture);
				if (num > num2)
				{
					return (!base.IsAscendingSortOrder) ? (result = -1) : (result = 1);
				}
				if (num < num2)
				{
					return (!base.IsAscendingSortOrder) ? (result = 1) : (result = -1);
				}
				return result;
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
				return result;
			}
		}
	}
}
