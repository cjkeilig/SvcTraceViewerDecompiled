using System;
using System.Collections;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal abstract class ListViewItemComparer : IComparer
	{
		private int column;

		private bool isAsc = true;

		public bool IsAscendingSortOrder => isAsc;

		public ListViewItemComparer()
		{
			column = 0;
		}

		public ListViewItemComparer(int column, bool isAsc)
		{
			this.column = column;
			this.isAsc = isAsc;
		}

		int IComparer.Compare(object x, object y)
		{
			try
			{
				return (column >= 0) ? Compare(((ListViewItem)x).SubItems[column].Text, ((ListViewItem)y).SubItems[column].Text) : Compare(((ListViewItem)x).Tag, ((ListViewItem)y).Tag);
			}
			catch (InvalidCastException)
			{
				return 0;
			}
		}

		protected abstract int Compare(object x, object y);
	}
}
