using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class FilterOptionsDialog : Form
	{
		private enum TabFocusIndex
		{
			OkButton,
			CancelButton,
			ShowWCFTraces,
			ShowActivityOption,
			ShowTM,
			ShowSM,
			ShowRM,
			ShowMessageSentReceiveOption
		}

		private AppConfigManager config;

		private bool isChanged;

		private bool isInitializing = true;

		private bool promptUser;

		private IContainer components;

		private Label lblDescription;

		private Button btnCancel;

		private Button btnOk;

		private CheckBox chkMessageSentReceived;

		private CheckBox chkTransfer;

		private CheckBox chkShowWCFTraces;

		private CheckBox chkShowTMs;

		private CheckBox chkShowRMs;

		private CheckBox chkShowSMs;

		public FilterOptionsDialog()
		{
			InitializeComponent();
		}

		public void Initialize(AppConfigManager config, bool promptUser)
		{
			this.config = config;
			this.promptUser = promptUser;
			CustomFilterOptionSettings customFilterOptionSettings = config.LoadCustomFilterOptionSettings();
			if (customFilterOptionSettings != null)
			{
				if (customFilterOptionSettings.ShowWCFTraces)
				{
					chkShowWCFTraces.Checked = true;
				}
				if (customFilterOptionSettings.ShowTransfer)
				{
					chkTransfer.Checked = true;
				}
				if (customFilterOptionSettings.ShowMessageSentReceived)
				{
					chkMessageSentReceived.Checked = true;
				}
				if (customFilterOptionSettings.ShowSecurityMessage)
				{
					chkShowSMs.Checked = true;
				}
				if (customFilterOptionSettings.ShowReliableMessage)
				{
					chkShowRMs.Checked = true;
				}
				if (customFilterOptionSettings.ShowTransactionMessage)
				{
					chkShowTMs.Checked = true;
				}
				UpdateEnabledStatusForWCFOptions(chkShowWCFTraces.Checked);
			}
			isInitializing = false;
		}

		private void SetChangedTag()
		{
			if (!isInitializing)
			{
				isChanged = true;
			}
		}

		private void chkShowWCFTraces_CheckedChanged(object sender, EventArgs e)
		{
			SetChangedTag();
			UpdateEnabledStatusForWCFOptions(chkShowWCFTraces.Checked);
		}

		private void chkTransfer_CheckedChanged(object sender, EventArgs e)
		{
			SetChangedTag();
		}

		private void chkMessageSentReceived_CheckedChanged(object sender, EventArgs e)
		{
			SetChangedTag();
		}

		private void chkShowSMs_CheckedChanged(object sender, EventArgs e)
		{
			SetChangedTag();
		}

		private void chkShowRMs_CheckedChanged(object sender, EventArgs e)
		{
			SetChangedTag();
		}

		private void chkShowTMs_CheckedChanged(object sender, EventArgs e)
		{
			SetChangedTag();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (isChanged)
			{
				CustomFilterOptionSettings customFilterOptionSettings = config.LoadCustomFilterOptionSettings();
				if (customFilterOptionSettings != null)
				{
					customFilterOptionSettings.ShowWCFTraces = chkShowWCFTraces.Checked;
					customFilterOptionSettings.ShowMessageSentReceived = chkMessageSentReceived.Checked;
					customFilterOptionSettings.ShowTransfer = chkTransfer.Checked;
					customFilterOptionSettings.ShowSecurityMessage = chkShowSMs.Checked;
					customFilterOptionSettings.ShowReliableMessage = chkShowRMs.Checked;
					customFilterOptionSettings.ShowTransactionMessage = chkShowTMs.Checked;
					if (!config.UpdateConfigFile())
					{
						return;
					}
					if (promptUser)
					{
						if (MessageBox.Show(this, SR.GetString("FO_MSG1"), SR.GetString("FO_Title"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0) == DialogResult.Yes)
						{
							base.DialogResult = DialogResult.Yes;
						}
						else
						{
							base.DialogResult = DialogResult.No;
						}
					}
				}
			}
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			base.DialogResult = DialogResult.Cancel;
			Close();
		}

		private void UpdateEnabledStatusForWCFOptions(bool isEnabled)
		{
			chkTransfer.Enabled = isEnabled;
			chkShowTMs.Enabled = isEnabled;
			chkShowSMs.Enabled = isEnabled;
			chkShowRMs.Enabled = isEnabled;
			chkMessageSentReceived.Enabled = isEnabled;
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
			lblDescription = new System.Windows.Forms.Label();
			chkMessageSentReceived = new System.Windows.Forms.CheckBox();
			chkTransfer = new System.Windows.Forms.CheckBox();
			btnCancel = new System.Windows.Forms.Button();
			btnOk = new System.Windows.Forms.Button();
			chkShowWCFTraces = new System.Windows.Forms.CheckBox();
			chkShowTMs = new System.Windows.Forms.CheckBox();
			chkShowRMs = new System.Windows.Forms.CheckBox();
			chkShowSMs = new System.Windows.Forms.CheckBox();
			SuspendLayout();
			lblDescription.AutoSize = true;
			lblDescription.Location = new System.Drawing.Point(5, 10);
			lblDescription.Name = "lblDescription";
			lblDescription.Size = new System.Drawing.Size(93, 13);
			lblDescription.TabStop = false;
			lblDescription.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FO_Description");
			chkMessageSentReceived.AutoSize = true;
			chkMessageSentReceived.Location = new System.Drawing.Point(25, 130);
			chkMessageSentReceived.Name = "chkMessageSentReceived";
			chkMessageSentReceived.Size = new System.Drawing.Size(199, 17);
			chkMessageSentReceived.TabIndex = 7;
			chkMessageSentReceived.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FO_O2");
			chkMessageSentReceived.CheckedChanged += new System.EventHandler(chkMessageSentReceived_CheckedChanged);
			chkTransfer.AutoSize = true;
			chkTransfer.Location = new System.Drawing.Point(25, 50);
			chkTransfer.Name = "chkTransfer";
			chkTransfer.Size = new System.Drawing.Size(121, 17);
			chkTransfer.TabIndex = 3;
			chkTransfer.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FO_O1");
			chkTransfer.CheckedChanged += new System.EventHandler(chkTransfer_CheckedChanged);
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Location = new System.Drawing.Point(194, 153);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new System.Drawing.Size(75, 23);
			btnCancel.TabIndex = 1;
			btnCancel.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Btn_Cancel");
			btnCancel.Click += new System.EventHandler(btnCancel_Click);
			btnOk.Location = new System.Drawing.Point(109, 153);
			btnOk.Name = "btnOk";
			btnOk.Size = new System.Drawing.Size(75, 23);
			btnOk.TabIndex = 0;
			btnOk.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Btn_OK");
			btnOk.Click += new System.EventHandler(btnOk_Click);
			chkShowWCFTraces.AutoSize = true;
			chkShowWCFTraces.Location = new System.Drawing.Point(10, 30);
			chkShowWCFTraces.Name = "chkShowWCFTraces";
			chkShowWCFTraces.Size = new System.Drawing.Size(112, 17);
			chkShowWCFTraces.TabIndex = 2;
			chkShowWCFTraces.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FO_O3");
			chkShowWCFTraces.UseVisualStyleBackColor = true;
			chkShowWCFTraces.CheckedChanged += new System.EventHandler(chkShowWCFTraces_CheckedChanged);
			chkShowTMs.AutoSize = true;
			chkShowTMs.Location = new System.Drawing.Point(25, 70);
			chkShowTMs.Name = "chkShowTMs";
			chkShowTMs.Size = new System.Drawing.Size(158, 17);
			chkShowTMs.TabIndex = 4;
			chkShowTMs.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FO_O4");
			chkShowTMs.UseVisualStyleBackColor = true;
			chkShowTMs.CheckedChanged += new System.EventHandler(chkShowTMs_CheckedChanged);
			chkShowRMs.AutoSize = true;
			chkShowRMs.Location = new System.Drawing.Point(25, 110);
			chkShowRMs.Name = "chkShowRMs";
			chkShowRMs.Size = new System.Drawing.Size(139, 17);
			chkShowRMs.TabIndex = 6;
			chkShowRMs.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FO_O5");
			chkShowRMs.UseVisualStyleBackColor = true;
			chkShowRMs.CheckedChanged += new System.EventHandler(chkShowRMs_CheckedChanged);
			chkShowSMs.AutoSize = true;
			chkShowSMs.Location = new System.Drawing.Point(25, 90);
			chkShowSMs.Name = "chkShowSMs";
			chkShowSMs.Size = new System.Drawing.Size(145, 17);
			chkShowSMs.TabIndex = 5;
			chkShowSMs.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FO_O6");
			chkShowSMs.UseVisualStyleBackColor = true;
			chkShowSMs.CheckedChanged += new System.EventHandler(chkShowSMs_CheckedChanged);
			AutoSize = true;
			base.AcceptButton = btnOk;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.CancelButton = btnCancel;
			base.Controls.Add(chkShowTMs);
			base.Controls.Add(chkShowRMs);
			base.Controls.Add(chkShowWCFTraces);
			base.Controls.Add(chkShowSMs);
			base.Controls.Add(lblDescription);
			base.Controls.Add(chkTransfer);
			base.Controls.Add(chkMessageSentReceived);
			base.Controls.Add(btnOk);
			base.Controls.Add(btnCancel);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			MaximumSize = new System.Drawing.Size(600, 220);
			MinimumSize = new System.Drawing.Size(277, 220);
			base.Name = "FilterOptionsDialog";
			base.ShowInTaskbar = false;
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FO_FilterOptionDlg");
			ResumeLayout(performLayout: false);
			PerformLayout();
		}
	}
}
