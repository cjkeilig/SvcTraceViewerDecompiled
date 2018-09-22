using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class DTRangeDialog : Form
	{
		private enum TabFocusIndex
		{
			OkButton,
			CancelButton,
			DTRangeControl
		}

		private List<FileDescriptor> fileDescriptors;

		private DateTimePair selectedDateTime;

		private DateTimePair dateTimeRange;

		private IContainer components;

		private DTRangeControl timeRangeControl;

		private Label lblDescription;

		private Panel reportPanel;

		private Button btnOk;

		private Button btnCancel;

		internal DateTimePair SelectedDateTime => selectedDateTime;

		public DTRangeDialog()
		{
			InitializeComponent();
		}

		private void CleanUpControls()
		{
			foreach (Control control in reportPanel.Controls)
			{
				if (control is FileBlockInfoControl)
				{
					((FileBlockInfoControl)control).Close();
				}
			}
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			CleanUpControls();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			CleanUpControls();
		}

		public void Initialize(List<FileDescriptor> fileDescriptors)
		{
			if (fileDescriptors == null)
			{
				throw new ArgumentNullException();
			}
			this.fileDescriptors = fileDescriptors;
			int x = 5;
			int num = 5;
			foreach (FileDescriptor fileDescriptor in this.fileDescriptors)
			{
				FileBlockInfoControl fileBlockInfoControl = new FileBlockInfoControl(fileDescriptor);
				fileBlockInfoControl.Location = new Point(x, num);
				num += 75;
				fileBlockInfoControl.Visible = true;
				reportPanel.Controls.Add(fileBlockInfoControl);
			}
			dateTimeRange = TraceDataSource.CalculateFileTimeRange(this.fileDescriptors);
			selectedDateTime = dateTimeRange;
			DTRangeControl dTRangeControl = timeRangeControl;
			dTRangeControl.TimeRangeChangeCallback = (DTRangeControl.TimeRangeChange)Delegate.Combine(dTRangeControl.TimeRangeChangeCallback, new DTRangeControl.TimeRangeChange(timeRangeControl_OnTimeRangeChanged));
			timeRangeControl.RefreshTimeRange(dateTimeRange.StartTime, dateTimeRange.EndTime);
			timeRangeControl.RefreshSelectedTimeRange(dateTimeRange.StartTime, dateTimeRange.EndTime);
			timeRangeControl_OnTimeRangeChanged(dateTimeRange.StartTime, dateTimeRange.EndTime);
		}

		private void timeRangeControl_OnTimeRangeChanged(DateTime start, DateTime end)
		{
			if (start > end)
			{
				start = end;
			}
			foreach (FileBlockInfoControl control in reportPanel.Controls)
			{
				control.RefreshByTimeRange(start, end);
			}
			selectedDateTime = new DateTimePair(start, end);
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
			timeRangeControl = new Microsoft.Tools.ServiceModel.TraceViewer.DTRangeControl();
			reportPanel = new System.Windows.Forms.Panel();
			btnOk = new System.Windows.Forms.Button();
			btnCancel = new System.Windows.Forms.Button();
			lblDescription = new System.Windows.Forms.Label();
			SuspendLayout();
			lblDescription.Location = new System.Drawing.Point(25, 25);
			lblDescription.Name = "lblDescription";
			lblDescription.Size = new System.Drawing.Size(680, 45);
			lblDescription.TabStop = false;
			lblDescription.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("PLDlg_Description");
			timeRangeControl.Location = new System.Drawing.Point(25, 70);
			timeRangeControl.Name = "timeRangeControl";
			timeRangeControl.Size = new System.Drawing.Size(680, 20);
			timeRangeControl.TabIndex = 2;
			reportPanel.AutoScroll = true;
			reportPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			reportPanel.Location = new System.Drawing.Point(25, 115);
			reportPanel.Name = "reportPanel";
			reportPanel.Size = new System.Drawing.Size(680, 235);
			reportPanel.TabStop = false;
			btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			btnOk.Location = new System.Drawing.Point(549, 360);
			btnOk.Name = "btnOk";
			btnOk.Size = new System.Drawing.Size(75, 23);
			btnOk.TabIndex = 0;
			btnOk.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("PLDlg_OK");
			btnOk.Click += new System.EventHandler(btnOk_Click);
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Location = new System.Drawing.Point(640, 360);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new System.Drawing.Size(75, 23);
			btnCancel.TabIndex = 1;
			btnCancel.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("PLDlg_Cancel");
			btnCancel.Click += new System.EventHandler(btnCancel_Click);
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(724, 398);
			base.MinimizeBox = false;
			base.MaximizeBox = false;
			base.CancelButton = btnCancel;
			base.AcceptButton = btnOk;
			base.Controls.Add(lblDescription);
			base.Controls.Add(btnCancel);
			base.Controls.Add(btnOk);
			base.Controls.Add(reportPanel);
			base.Controls.Add(timeRangeControl);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			base.Name = "DTRangeDialog";
			base.ShowIcon = false;
			base.ShowInTaskbar = false;
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("PLDlg_Title");
			ResumeLayout(performLayout: false);
		}
	}
}
