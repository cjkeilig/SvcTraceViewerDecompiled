using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class BasicTraceInfoControl : UserControl
	{
		private enum TabFocusIndex
		{
			ListView
		}

		private readonly string dateTimeFormat = SR.GetString("FV_DateTimeFormat");

		private IContainer components;

		private ListView listView;

		private ColumnHeader nameColumn;

		private ColumnHeader valueColumn;

		public BasicTraceInfoControl()
		{
			InitializeComponent();
		}

		public void CleanUp()
		{
			listView.Items.Clear();
		}

		public void ReloadTrace(TraceRecord trace)
		{
			CleanUp();
			if (trace != null)
			{
				ListViewItem listViewItem = null;
				string text = TraceRecord.NormalizeActivityId(trace.ActivityID);
				listViewItem = ((!TraceViewerForm.IsActivityDisplayNameInCache(text)) ? new ListViewItem(new string[2]
				{
					SR.GetString("FV_Basic_ActivityID"),
					text
				}) : new ListViewItem(new string[2]
				{
					SR.GetString("FV_Basic_ActivityName"),
					TraceViewerForm.GetActivityDisplayName(text)
				}));
				listView.Items.Add(listViewItem);
				if (trace.IsTransfer && !string.IsNullOrEmpty(trace.RelatedActivityID))
				{
					string text2 = TraceRecord.NormalizeActivityId(trace.RelatedActivityID);
					listViewItem = ((!TraceViewerForm.IsActivityDisplayNameInCache(text2)) ? new ListViewItem(new string[2]
					{
						SR.GetString("FV_Basic_RelatedActivityID"),
						text2
					}) : new ListViewItem(new string[2]
					{
						SR.GetString("FV_Basic_RelatedActivityName"),
						TraceViewerForm.GetActivityDisplayName(text2)
					}));
					listView.Items.Add(listViewItem);
				}
				listViewItem = new ListViewItem(new string[2]
				{
					SR.GetString("FV_Basic_Time"),
					trace.Time.ToString(dateTimeFormat, CultureInfo.CurrentUICulture)
				});
				listView.Items.Add(listViewItem);
				listViewItem = new ListViewItem(new string[2]
				{
					SR.GetString("FV_Basic_Level"),
					trace.Level.ToString()
				});
				listView.Items.Add(listViewItem);
				listViewItem = new ListViewItem(new string[2]
				{
					SR.GetString("FV_Basic_Source"),
					trace.SourceName
				});
				listView.Items.Add(listViewItem);
				listViewItem = new ListViewItem(new string[2]
				{
					SR.GetString("FV_Basic_Process"),
					trace.ProcessName
				});
				listView.Items.Add(listViewItem);
				listViewItem = new ListViewItem(new string[2]
				{
					SR.GetString("FV_Basic_Thread"),
					trace.ThreadId.ToString(CultureInfo.CurrentCulture)
				});
				listView.Items.Add(listViewItem);
				listViewItem = new ListViewItem(new string[2]
				{
					SR.GetString("FV_Basic_Computer"),
					(trace.Execution != null) ? trace.Execution.ComputerName : string.Empty
				});
				listView.Items.Add(listViewItem);
				listViewItem = new ListViewItem(new string[2]
				{
					SR.GetString("FV_Basic_TraceIdentifier"),
					trace.TraceCode
				});
				listView.Items.Add(listViewItem);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			listView = new System.Windows.Forms.ListView();
			nameColumn = new System.Windows.Forms.ColumnHeader();
			valueColumn = new System.Windows.Forms.ColumnHeader();
			SuspendLayout();
			listView.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[2]
			{
				nameColumn,
				valueColumn
			});
			listView.FullRowSelect = true;
			listView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			listView.HideSelection = false;
			listView.Location = new System.Drawing.Point(5, 0);
			listView.Name = "listView";
			listView.ShowItemToolTips = true;
			listView.Size = new System.Drawing.Size(420, 150);
			listView.TabIndex = 0;
			listView.UseCompatibleStateImageBehavior = false;
			listView.View = System.Windows.Forms.View.Details;
			nameColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_List_NameCol");
			nameColumn.Width = 113;
			valueColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_List_ValueCol");
			valueColumn.Width = 293;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			BackColor = System.Drawing.SystemColors.Window;
			base.Controls.Add(listView);
			base.Name = "BasicTraceInfoControl";
			base.Size = new System.Drawing.Size(430, 150);
			ResumeLayout(performLayout: false);
		}
	}
}
