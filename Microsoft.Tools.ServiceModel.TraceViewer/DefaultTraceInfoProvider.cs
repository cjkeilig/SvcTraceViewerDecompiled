using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class DefaultTraceInfoProvider : UserControl, IAdvancedTraceInfoProvider
	{
		private enum TabFocusIndex
		{
			MainPanel,
			BasicPart,
			AppDataPart,
			ExceptionPart,
			MessageInfoPart,
			MessageLogInfoPart,
			ListPart,
			DiagonsticsPart
		}

		private LinkedList<ExpandablePart> activeParts = new LinkedList<ExpandablePart>();

		private TraceDetailBasicInfoPart basicInfoPart;

		private TraceDetailListPart listPart;

		private TraceDetailExceptionPart exceptionPart;

		private TraceDetailDiagnosticsPart diagPart;

		private TraceDetailMessageInfoPart messageInfoPart;

		private TraceDetailMessageLogInfoPart messageLogInfoPart;

		private TraceDetailAppDataPart appDataPart;

		private const int EDGE_SIZE_X = 5;

		private const int EDGE_SIZE_Y = 10;

		private IContainer components;

		private Panel mainPanel;

		private Panel dummyHead;

		private Panel dummyFoot;

		public DefaultTraceInfoProvider()
		{
			InitializeComponent();
			Initialize();
		}

		private void Initialize()
		{
			ExpandablePart.ExpandablePartStateChanged callback = RestructLayout;
			mainPanel.Controls.Add(dummyHead);
			basicInfoPart = new TraceDetailBasicInfoPart(callback);
			basicInfoPart.TabIndex = 1;
			mainPanel.Controls.Add(basicInfoPart);
			appDataPart = new TraceDetailAppDataPart(callback);
			appDataPart.TabIndex = 2;
			mainPanel.Controls.Add(appDataPart);
			listPart = new TraceDetailListPart(callback);
			listPart.TabIndex = 6;
			mainPanel.Controls.Add(listPart);
			exceptionPart = new TraceDetailExceptionPart(callback);
			exceptionPart.TabIndex = 3;
			mainPanel.Controls.Add(exceptionPart);
			diagPart = new TraceDetailDiagnosticsPart(callback);
			diagPart.TabIndex = 7;
			mainPanel.Controls.Add(diagPart);
			messageInfoPart = new TraceDetailMessageInfoPart(callback);
			messageInfoPart.TabIndex = 4;
			mainPanel.Controls.Add(messageInfoPart);
			messageLogInfoPart = new TraceDetailMessageLogInfoPart(callback);
			messageLogInfoPart.TabIndex = 5;
			mainPanel.Controls.Add(messageLogInfoPart);
			mainPanel.Controls.Add(dummyFoot);
			HideAllParts();
		}

		private void mainPanel_MouseClick(object sender, MouseEventArgs e)
		{
			mainPanel.Focus();
		}

		private void HideAllParts()
		{
			basicInfoPart.Visible = false;
			appDataPart.Visible = false;
			listPart.Visible = false;
			exceptionPart.Visible = false;
			diagPart.Visible = false;
			messageInfoPart.Visible = false;
			messageLogInfoPart.Visible = false;
		}

		bool IAdvancedTraceInfoProvider.CanSupport(TraceRecord trace)
		{
			if (trace != null && !string.IsNullOrEmpty(trace.Xml))
			{
				return true;
			}
			return false;
		}

		Control IAdvancedTraceInfoProvider.GetAdvancedTraceInfoControl()
		{
			return this;
		}

		void IAdvancedTraceInfoProvider.ReloadTrace(TraceRecord trace, TraceDetailInfoControlParam param)
		{
			if (trace != null)
			{
				SuspendLayout();
				try
				{
					activeParts.Clear();
					HideAllParts();
					TraceDetailedProcessParameter traceDetailedProcessParameter = new TraceDetailedProcessParameter(trace);
					if (param != null && param.ShowBasicInfo)
					{
						basicInfoPart.Visible = true;
						basicInfoPart.ReloadTracePart(traceDetailedProcessParameter);
						activeParts.AddLast(basicInfoPart);
					}
					if (TraceDetailAppDataPart.ContainsMatchProperties(traceDetailedProcessParameter))
					{
						appDataPart.Visible = true;
						appDataPart.ReloadTracePart(traceDetailedProcessParameter);
						activeParts.AddLast(appDataPart);
					}
					if (TraceDetailExceptionPart.ContainsMatchProperties(traceDetailedProcessParameter))
					{
						exceptionPart.Visible = true;
						exceptionPart.ReloadTracePart(traceDetailedProcessParameter);
						activeParts.AddLast(exceptionPart);
					}
					if (TraceDetailMessageInfoPart.ContainsMatchProperties(traceDetailedProcessParameter))
					{
						messageInfoPart.Visible = true;
						messageInfoPart.ReloadTracePart(traceDetailedProcessParameter);
						activeParts.AddLast(messageInfoPart);
					}
					if (TraceDetailMessageLogInfoPart.ContainsMatchProperties(traceDetailedProcessParameter))
					{
						messageLogInfoPart.Visible = true;
						messageLogInfoPart.ReloadTracePart(traceDetailedProcessParameter);
						activeParts.AddLast(messageLogInfoPart);
					}
					if (traceDetailedProcessParameter.PropertyCount != 0)
					{
						listPart.Visible = true;
						listPart.ReloadTracePart(traceDetailedProcessParameter);
						activeParts.AddLast(listPart);
					}
					if (param != null && param.ShowDiagnosticsInfo && TraceDetailDiagnosticsPart.ContainsMatchProperties(traceDetailedProcessParameter))
					{
						diagPart.Visible = true;
						diagPart.ReloadTracePart(traceDetailedProcessParameter);
						activeParts.AddLast(diagPart);
					}
					RestructLayout(null);
				}
				finally
				{
					ResumeLayout();
				}
			}
		}

		public void RestructLayout(ExpandablePart part)
		{
			int num = dummyHead.Top + 5;
			int left = dummyHead.Left;
			bool flag = false;
			if (part != null)
			{
				SuspendLayout();
			}
			mainPanel.SuspendLayout();
			foreach (ExpandablePart activePart in activeParts)
			{
				Control expandablePart = activePart.GetExpandablePart();
				if (expandablePart != null)
				{
					if (part == null || (part != null && flag))
					{
						expandablePart.Location = new Point(left, num);
						num += activePart.GetCurrentSize().Height + 10;
						flag = true;
					}
					else if (activePart == part)
					{
						flag = true;
						num = expandablePart.Location.Y + expandablePart.Size.Height + 10;
					}
				}
			}
			dummyFoot.Location = new Point(left, num);
			mainPanel.ResumeLayout();
			if (part != null)
			{
				ResumeLayout();
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
			mainPanel = new System.Windows.Forms.Panel();
			dummyHead = new System.Windows.Forms.Panel();
			dummyFoot = new System.Windows.Forms.Panel();
			SuspendLayout();
			mainPanel.AutoScroll = true;
			mainPanel.BackColor = System.Drawing.SystemColors.Window;
			mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			mainPanel.Location = new System.Drawing.Point(0, 0);
			mainPanel.Name = "mainPanel";
			mainPanel.Size = new System.Drawing.Size(460, 400);
			mainPanel.TabIndex = 0;
			mainPanel.MouseClick += new System.Windows.Forms.MouseEventHandler(mainPanel_MouseClick);
			mainPanel.AutoSize = true;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(mainPanel);
			base.Name = "DefaultTraceInfoProvider";
			base.Size = new System.Drawing.Size(460, 400);
			ResumeLayout(performLayout: false);
			base.Name = "dummyHead";
			dummyHead.Height = 0;
			dummyHead.Size = new System.Drawing.Size(0, 0);
			dummyHead.Location = new System.Drawing.Point(5, 0);
			base.Name = "dummyFoot";
			dummyFoot.Height = 0;
			dummyFoot.Size = new System.Drawing.Size(0, 0);
			dummyFoot.Location = new System.Drawing.Point(5, 0);
		}
	}
}
