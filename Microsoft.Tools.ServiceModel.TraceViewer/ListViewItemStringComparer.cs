using System.Globalization;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class ListViewItemStringComparer : ListViewItemComparer
	{
		public ListViewItemStringComparer()
		{
		}

		public ListViewItemStringComparer(int column, bool isAsc)
			: base(column, isAsc)
		{
		}

		protected override int Compare(object x, object y)
		{
			int num = string.Compare((string)x, (string)y,  true, CultureInfo.CurrentCulture);
			if (!base.IsAscendingSortOrder)
			{
				switch (num)
				{
				case 1:
					return -1;
				case -1:
					return 1;
				default:
					return 0;
				}
			}
			return num;
		}
	}
}
