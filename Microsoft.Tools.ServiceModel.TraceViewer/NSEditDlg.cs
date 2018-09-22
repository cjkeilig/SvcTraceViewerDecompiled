using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class NSEditDlg : Form
	{
		private enum TabFocusIndex
		{
			OkButton,
			CancelButton,
			PrefixLabel,
			PrefixText,
			NamespaceLabel,
			NamespaceText
		}

		private CustomFilterDefineDialog parent;

		private string originalPrefix = string.Empty;

		private static List<string> cachedNSNames;

		private IErrorReport errorReport;

		private IContainer components;

		private Label lblPrefix;

		private Label lblNamespace;

		private TextBox prefix;

		private Button btnCancel;

		private Button btnOk;

		private ComboBox ns;

		public string Prefix => prefix.Text;

		public string Namespace => ns.Text;

		static NSEditDlg()
		{
			cachedNSNames = new List<string>();
			cachedNSNames.Add("http://schemas.microsoft.com/2004/06/E2ETraceEvent");
			cachedNSNames.Add("http://schemas.microsoft.com/2004/06/windows/eventlog/system");
			cachedNSNames.Add("http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord");
			cachedNSNames.Add("http://schemas.xmlsoap.org/soap/envelope/");
			cachedNSNames.Add("http://schemas.microsoft.com/2004/09/ServiceModel/Diagnostics");
			cachedNSNames.Add("http://tempuri.org/");
		}

		public NSEditDlg(IErrorReport errorReport)
		{
			InitializeComponent();
			this.errorReport = errorReport;
		}

		public void Initialize(string prefix, string ns, CustomFilterDefineDialog parent)
		{
			this.prefix.Text = prefix;
			this.ns.Text = ns;
			originalPrefix = prefix;
			Initialize(parent);
		}

		public void Initialize(CustomFilterDefineDialog parent)
		{
			this.parent = parent;
			foreach (string cachedNSName in cachedNSNames)
			{
				ns.Items.Add(cachedNSName);
			}
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(Prefix))
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_Err7"));
			}
			else if (string.IsNullOrEmpty(Namespace))
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_Err8"));
			}
			else if (parent.IsDuplicatePrefix(Prefix, originalPrefix))
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_Err9"));
			}
			else
			{
				base.DialogResult = DialogResult.OK;
				if (!cachedNSNames.Contains(Namespace))
				{
					cachedNSNames.Add(Namespace);
				}
				Close();
			}
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			base.DialogResult = DialogResult.Cancel;
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
			components = new System.ComponentModel.Container();
			lblPrefix = new System.Windows.Forms.Label();
			lblNamespace = new System.Windows.Forms.Label();
			prefix = new System.Windows.Forms.TextBox();
			btnCancel = new System.Windows.Forms.Button();
			btnOk = new System.Windows.Forms.Button();
			ns = new System.Windows.Forms.ComboBox();
			SuspendLayout();
			lblPrefix.AutoSize = true;
			lblPrefix.Location = new System.Drawing.Point(13, 13);
			lblPrefix.Name = "lblPrefix";
			lblPrefix.Size = new System.Drawing.Size(32, 13);
			lblPrefix.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_PFLL");
			lblPrefix.TabIndex = 2;
			lblNamespace.AutoSize = true;
			lblNamespace.Location = new System.Drawing.Point(14, 40);
			lblNamespace.Name = "lblNamespace";
			lblNamespace.Size = new System.Drawing.Size(63, 13);
			lblNamespace.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_NSLL");
			lblNamespace.TabIndex = 4;
			prefix.Location = new System.Drawing.Point(121, 13);
			prefix.MaxLength = 255;
			prefix.Name = "prefix";
			prefix.Size = new System.Drawing.Size(358, 20);
			prefix.TabIndex = 3;
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Location = new System.Drawing.Point(404, 68);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new System.Drawing.Size(75, 23);
			btnCancel.TabIndex = 1;
			btnCancel.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Btn_Cancel");
			btnCancel.Click += new System.EventHandler(btnCancel_Click);
			btnOk.Location = new System.Drawing.Point(319, 68);
			btnOk.Name = "btnOk";
			btnOk.Size = new System.Drawing.Size(75, 23);
			btnOk.TabIndex = 0;
			btnOk.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Btn_OK");
			btnOk.Click += new System.EventHandler(btnOk_Click);
			ns.FormattingEnabled = true;
			ns.Location = new System.Drawing.Point(121, 40);
			ns.Name = "ns";
			ns.Size = new System.Drawing.Size(358, 21);
			ns.TabIndex = 5;
			ns.MaxLength = 512;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.CancelButton = btnCancel;
			base.AcceptButton = btnOk;
			base.ClientSize = new System.Drawing.Size(490, 103);
			base.Controls.Add(ns);
			base.Controls.Add(btnOk);
			base.Controls.Add(btnCancel);
			base.Controls.Add(prefix);
			base.Controls.Add(lblNamespace);
			base.Controls.Add(lblPrefix);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "NSEditDlg";
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_Namespaces");
			ResumeLayout(performLayout: false);
			PerformLayout();
		}
	}
}
