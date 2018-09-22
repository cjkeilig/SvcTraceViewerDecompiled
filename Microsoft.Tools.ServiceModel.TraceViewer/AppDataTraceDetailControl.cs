using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class AppDataTraceDetailControl : UserControl
	{
		private IContainer components;

		private TextBox txtAppData;

		public AppDataTraceDetailControl()
		{
			InitializeComponent();
		}

		public void ReloadAppData(string appData)
		{
			if (appData != null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(appData[0]);
				char c = appData[0];
				for (int i = 1; i < appData.Length; i++)
				{
					if (c != '\r' && appData[i] == '\n')
					{
						stringBuilder.Append('\r');
					}
					stringBuilder.Append(appData[i]);
					c = appData[i];
				}
				appData = stringBuilder.ToString();
			}
			txtAppData.Text = appData;
		}

		public void CleanUp()
		{
			txtAppData.Clear();
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
			txtAppData = new System.Windows.Forms.TextBox();
			SuspendLayout();
			txtAppData.AcceptsReturn = true;
			txtAppData.AcceptsTab = true;
			txtAppData.AccessibleRole = System.Windows.Forms.AccessibleRole.ButtonDropDown;
			txtAppData.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			txtAppData.HideSelection = false;
			txtAppData.Location = new System.Drawing.Point(5, 0);
			txtAppData.Multiline = true;
			txtAppData.Name = "txtAppData";
			txtAppData.ReadOnly = true;
			txtAppData.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			txtAppData.Size = new System.Drawing.Size(360, 128);
			txtAppData.TabIndex = 0;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(txtAppData);
			base.Name = "AppDataTraceDetailControl";
			base.Size = new System.Drawing.Size(370, 128);
			ResumeLayout(performLayout: false);
			PerformLayout();
		}
	}
}
