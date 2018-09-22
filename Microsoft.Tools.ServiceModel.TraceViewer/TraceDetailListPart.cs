using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceDetailListPart : ExpandablePart
	{
		private ListView infoList;

		protected override string ExpandablePartName => SR.GetString("FV_List_Title");

		public TraceDetailListPart(ExpandablePartStateChanged callback)
		{
			infoList = new ListView();
			infoList.SuspendLayout();
			ColumnHeader value = new ColumnHeader
			{
				Text = SR.GetString("FV_List_NameCol")
			};
			ColumnHeader value2 = new ColumnHeader
			{
				Text = SR.GetString("FV_List_ValueCol")
			};
			infoList.FullRowSelect = true;
			infoList.Columns.Add(value);
			infoList.Columns.Add(value2);
			infoList.ShowItemToolTips = true;
			infoList.MultiSelect = true;
			infoList.View = View.Details;
			infoList.Scrollable = true;
			infoList.HideSelection = false;
			infoList.Location = new Point(5, 5);
			infoList.Size = new Size(420, 145);
			infoList.TabIndex = 0;
			infoList.Dock = DockStyle.Fill;
			infoList.ResumeLayout();
			SetupRightPart(infoList, callback, infoList.Height + 30);
		}

		private void CleanUp()
		{
			infoList.Items.Clear();
		}

		public override void ReloadTracePart(TraceDetailedProcessParameter parameter)
		{
			CleanUp();
			if (parameter != null)
			{
				foreach (TraceDetailedProcessParameter.TraceProperty item in parameter)
				{
					if (!item.IsXmlFormat)
					{
						infoList.Items.Add(new ListViewItem(new string[2]
						{
							item.PropertyName,
							item.PropertyValue
						}));
						parameter.RemoveProperty(item);
					}
				}
			}
			UpdateUIElements();
		}
	}
}
