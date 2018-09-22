using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class FileLoadingReport : Form
	{
		private enum TabFocusIndex
		{
			OkButton,
			ErrorGroup,
			ErrorList
		}

		private List<TraceViewerException> exceptionList;

		private IContainer components;

		private GroupBox errorGroup;

		private ListView listError;

		private ColumnHeader errorDescriptionColumn;

		private ColumnHeader errorSourceColumn;

		private ColumnHeader fileOffsetColumn;

		private Button btnOK;

		public FileLoadingReport()
		{
			InitializeComponent();
		}

		public void Initialize(List<TraceViewerException> exceptionList)
		{
			this.exceptionList = exceptionList;
			foreach (TraceViewerException exception in exceptionList)
			{
				try
				{
					AppendExceptionToList(exception);
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
				}
			}
		}

		private void AppendExceptionToList(TraceViewerException e)
		{
			if (e != null)
			{
				string text = string.Empty;
				string text2 = string.Empty;
				if (e is LogFileException)
				{
					text = ((LogFileException)e).FilePath;
				}
				else if (e is E2EInvalidFileException)
				{
					text2 = ((E2EInvalidFileException)e).FileOffset.ToString(CultureInfo.CurrentUICulture);
					text = ((E2EInvalidFileException)e).FilePath;
				}
				ListViewItem listViewItem = new ListViewItem(new string[3]
				{
					e.Message,
					(!string.IsNullOrEmpty(text)) ? Path.GetFileName(text) : string.Empty,
					text2
				});
				if (e is LogFileException)
				{
					listViewItem.Group = listError.Groups[0];
				}
				else if (e is E2EInvalidFileException)
				{
					listViewItem.Group = listError.Groups[1];
				}
				else
				{
					listViewItem.Group = listError.Groups[2];
				}
				listError.Items.Add(listViewItem);
			}
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			Close();
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
			System.Windows.Forms.ListViewGroup listViewGroup = new System.Windows.Forms.ListViewGroup("", System.Windows.Forms.HorizontalAlignment.Left);
			System.Windows.Forms.ListViewGroup listViewGroup2 = new System.Windows.Forms.ListViewGroup("", System.Windows.Forms.HorizontalAlignment.Left);
			System.Windows.Forms.ListViewGroup listViewGroup3 = new System.Windows.Forms.ListViewGroup("", System.Windows.Forms.HorizontalAlignment.Left);
			errorGroup = new System.Windows.Forms.GroupBox();
			listError = new System.Windows.Forms.ListView();
			errorDescriptionColumn = new System.Windows.Forms.ColumnHeader();
			errorSourceColumn = new System.Windows.Forms.ColumnHeader();
			fileOffsetColumn = new System.Windows.Forms.ColumnHeader();
			btnOK = new System.Windows.Forms.Button();
			errorGroup.SuspendLayout();
			SuspendLayout();
			errorGroup.Controls.Add(listError);
			errorGroup.Location = new System.Drawing.Point(12, 12);
			errorGroup.Name = "errorGroup";
			errorGroup.Size = new System.Drawing.Size(456, 261);
			errorGroup.TabIndex = 1;
			errorGroup.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("LE_GErrors");
			listError.Columns.AddRange(new System.Windows.Forms.ColumnHeader[3]
			{
				errorDescriptionColumn,
				errorSourceColumn,
				fileOffsetColumn
			});
			listError.FullRowSelect = true;
			listViewGroup.Header = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("LE_GPFError");
			listViewGroup.Name = "fileErrorGroup";
			listViewGroup2.Header = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("LE_GPTError");
			listViewGroup2.Name = "traceRecordErrorGroup";
			listViewGroup3.Header = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("LE_GPUError");
			listViewGroup3.Name = "unknownErrorGroup";
			listError.Groups.AddRange(new System.Windows.Forms.ListViewGroup[3]
			{
				listViewGroup,
				listViewGroup2,
				listViewGroup3
			});
			listError.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			listError.Location = new System.Drawing.Point(6, 19);
			listError.MultiSelect = false;
			listError.Name = "listError";
			listError.ShowItemToolTips = true;
			listError.Size = new System.Drawing.Size(444, 236);
			listError.TabIndex = 2;
			listError.View = System.Windows.Forms.View.Details;
			errorDescriptionColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("LE_DescriptionCol");
			errorDescriptionColumn.Width = 150;
			errorSourceColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("LE_SourceCol");
			errorSourceColumn.Width = 150;
			fileOffsetColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("LE_OffsetCol");
			btnOK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnOK.Location = new System.Drawing.Point(386, 287);
			btnOK.Name = "btnOK";
			btnOK.Size = new System.Drawing.Size(75, 23);
			btnOK.TabIndex = 0;
			btnOK.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Btn_OK");
			btnOK.Click += new System.EventHandler(btnOK_Click);
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.CancelButton = btnOK;
			base.AcceptButton = btnOK;
			base.ClientSize = new System.Drawing.Size(480, 322);
			base.Controls.Add(btnOK);
			base.Controls.Add(errorGroup);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.ShowInTaskbar = false;
			base.Name = "FileLoadingReport";
			Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("LE_Title");
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			errorGroup.ResumeLayout(performLayout: false);
			ResumeLayout(performLayout: false);
		}
	}
}
