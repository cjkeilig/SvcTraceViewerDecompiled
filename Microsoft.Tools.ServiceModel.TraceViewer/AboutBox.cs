using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class AboutBox : Form
	{
		private IContainer components;

		private TableLayoutPanel tableLayoutPanel;

		private PictureBox logoPictureBox;

		private Label labelProductName;

		private Label labelVersion;

		private Label labelCopyright;

		private Label labelCompanyName;

		private TextBox textBoxDescription;

		private Panel buttonPanel;

		private Button btnSysInfo;

		private Button btnOk;

		public string AssemblyCompany
		{
			get
			{
				object[] customAttributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), inherit: false);
				if (customAttributes.Length == 0)
				{
					return string.Empty;
				}
				return ((AssemblyCompanyAttribute)customAttributes[0]).Company;
			}
		}

		public string AssemblyProduct
		{
			get
			{
				object[] customAttributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), inherit: false);
				if (customAttributes.Length == 0)
				{
					return string.Empty;
				}
				return ((AssemblyProductAttribute)customAttributes[0]).Product;
			}
		}

		public string AssemblyTitle
		{
			get
			{
				object[] customAttributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), inherit: false);
				if (customAttributes.Length != 0)
				{
					AssemblyTitleAttribute assemblyTitleAttribute = (AssemblyTitleAttribute)customAttributes[0];
					if (assemblyTitleAttribute.Title != string.Empty)
					{
						return assemblyTitleAttribute.Title;
					}
				}
				return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
			}
		}

		public AboutBox()
		{
			InitializeComponent();
			Text = SR.GetString("About_Title");
			labelProductName.Text = AssemblyProduct;
			labelVersion.Text = string.Format(CultureInfo.CurrentCulture, "4.6.1055.0");
			labelCompanyName.Text = AssemblyCompany;
		}

		private void btnSysInfo_Click(object sender, EventArgs e)
		{
			try
			{
				Process.Start("Msinfo32.exe");
			}
			catch (Win32Exception)
			{
				MessageBox.Show(this, SR.GetString("About_FailSysInfo"), string.Format(CultureInfo.CurrentCulture, SR.GetString("About_Title"), new object[1]
				{
					AssemblyTitle
				}), MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
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
			tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			logoPictureBox = new System.Windows.Forms.PictureBox();
			labelProductName = new System.Windows.Forms.Label();
			labelVersion = new System.Windows.Forms.Label();
			labelCopyright = new System.Windows.Forms.Label();
			labelCompanyName = new System.Windows.Forms.Label();
			textBoxDescription = new System.Windows.Forms.TextBox();
			buttonPanel = new System.Windows.Forms.Panel();
			btnSysInfo = new System.Windows.Forms.Button();
			btnOk = new System.Windows.Forms.Button();
			tableLayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
			buttonPanel.SuspendLayout();
			SuspendLayout();
			tableLayoutPanel.ColumnCount = 2;
			tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			tableLayoutPanel.Controls.Add(logoPictureBox, 0, 0);
			tableLayoutPanel.Controls.Add(labelProductName, 1, 0);
			tableLayoutPanel.Controls.Add(labelVersion, 1, 1);
			tableLayoutPanel.Controls.Add(labelCopyright, 1, 2);
			tableLayoutPanel.Controls.Add(labelCompanyName, 1, 3);
			tableLayoutPanel.Controls.Add(textBoxDescription, 1, 4);
			tableLayoutPanel.Controls.Add(buttonPanel, 1, 5);
			tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			tableLayoutPanel.Location = new System.Drawing.Point(9, 9);
			tableLayoutPanel.Name = "tableLayoutPanel";
			tableLayoutPanel.RowCount = 6;
			tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10f));
			tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10f));
			tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10f));
			tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10f));
			tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 36.22641f));
			tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 24.5283f));
			tableLayoutPanel.Size = new System.Drawing.Size(417, 265);
			tableLayoutPanel.TabIndex = 0;
			logoPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
			logoPictureBox.Location = new System.Drawing.Point(3, 3);
			logoPictureBox.Name = "logoPictureBox";
			tableLayoutPanel.SetRowSpan(logoPictureBox, 6);
			logoPictureBox.Size = new System.Drawing.Size(125, 256);
			logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			logoPictureBox.Image = Microsoft.Tools.ServiceModel.TraceViewer.TempFileManager.GetImageFromEmbededResources(Microsoft.Tools.ServiceModel.TraceViewer.Images.AboutBox);
			logoPictureBox.TabIndex = 12;
			logoPictureBox.TabStop = false;
			labelProductName.Dock = System.Windows.Forms.DockStyle.Fill;
			labelProductName.Location = new System.Drawing.Point(143, 0);
			labelProductName.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
			labelProductName.MaximumSize = new System.Drawing.Size(0, 17);
			labelProductName.Name = "labelProductName";
			labelProductName.Size = new System.Drawing.Size(271, 17);
			labelProductName.TabIndex = 19;
			labelProductName.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("About_ProduceName");
			labelProductName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			labelVersion.Dock = System.Windows.Forms.DockStyle.Fill;
			labelVersion.Location = new System.Drawing.Point(143, 26);
			labelVersion.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
			labelVersion.MaximumSize = new System.Drawing.Size(0, 17);
			labelVersion.Name = "labelVersion";
			labelVersion.Size = new System.Drawing.Size(271, 17);
			labelVersion.TabIndex = 0;
			labelVersion.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("About_Version2");
			labelVersion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			labelCopyright.Dock = System.Windows.Forms.DockStyle.Fill;
			labelCopyright.Location = new System.Drawing.Point(143, 52);
			labelCopyright.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
			labelCopyright.MaximumSize = new System.Drawing.Size(0, 17);
			labelCopyright.Name = "labelCopyright";
			labelCopyright.Size = new System.Drawing.Size(271, 17);
			labelCopyright.TabIndex = 21;
			labelCopyright.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("About_Copyright");
			labelCopyright.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			labelCompanyName.Dock = System.Windows.Forms.DockStyle.Fill;
			labelCompanyName.Location = new System.Drawing.Point(143, 78);
			labelCompanyName.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
			labelCompanyName.MaximumSize = new System.Drawing.Size(0, 17);
			labelCompanyName.Name = "labelCompanyName";
			labelCompanyName.Size = new System.Drawing.Size(271, 17);
			labelCompanyName.TabIndex = 22;
			labelCompanyName.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("About_CName");
			labelCompanyName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			textBoxDescription.Dock = System.Windows.Forms.DockStyle.Fill;
			textBoxDescription.Location = new System.Drawing.Point(143, 107);
			textBoxDescription.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
			textBoxDescription.Multiline = true;
			textBoxDescription.Name = "textBoxDescription";
			textBoxDescription.ReadOnly = true;
			textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			textBoxDescription.Size = new System.Drawing.Size(271, 89);
			textBoxDescription.TabIndex = 23;
			textBoxDescription.TabStop = false;
			textBoxDescription.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("About_Description");
			buttonPanel.Controls.Add(btnSysInfo);
			buttonPanel.Controls.Add(btnOk);
			buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			buttonPanel.Location = new System.Drawing.Point(140, 202);
			buttonPanel.Name = "buttonPanel";
			buttonPanel.Size = new System.Drawing.Size(274, 60);
			buttonPanel.TabIndex = 24;
			btnSysInfo.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			btnSysInfo.Location = new System.Drawing.Point(146, 34);
			btnSysInfo.Name = "btnSysInfo";
			btnSysInfo.Size = new System.Drawing.Size(125, 23);
			btnSysInfo.TabIndex = 1;
			btnSysInfo.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("About_SysInfo");
			btnSysInfo.UseVisualStyleBackColor = true;
			btnSysInfo.Click += new System.EventHandler(btnSysInfo_Click);
			btnOk.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			btnOk.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnOk.Location = new System.Drawing.Point(146, 5);
			btnOk.Name = "btnOk";
			btnOk.Size = new System.Drawing.Size(125, 23);
			btnOk.TabIndex = 0;
			btnOk.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("About_OK");
			btnOk.UseVisualStyleBackColor = true;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(535, 283);
			base.Controls.Add(tableLayoutPanel);
			base.CancelButton = btnOk;
			base.AcceptButton = btnOk;
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "AboutBox";
			base.Padding = new System.Windows.Forms.Padding(9);
			base.ShowIcon = false;
			base.ShowInTaskbar = false;
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			tableLayoutPanel.ResumeLayout(performLayout: false);
			tableLayoutPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)logoPictureBox).EndInit();
			buttonPanel.ResumeLayout(performLayout: false);
			ResumeLayout(performLayout: false);
		}
	}
}
