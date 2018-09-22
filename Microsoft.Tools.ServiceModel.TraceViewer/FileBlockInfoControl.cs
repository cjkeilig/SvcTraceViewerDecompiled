using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class FileBlockInfoControl : UserControl
	{
		private Graphics graphics;

		private Brush selectedItemBrush;

		private Brush unselectedItemBrush;

		private bool isDisposed;

		private FileDescriptor fileDescriptor;

		private DateTime startDateTime;

		private DateTime endDateTime;

		private const int TOOLTIP_DELAY = 100;

		private const int TOOLTIP_DISPLAY = 10000;

		private IContainer components;

		private Label lblFilePath;

		private Panel DrawPanel;

		private Label lblLoadSize;

		private Label lblLoadPercentage;

		internal FileBlockInfoControl(FileDescriptor fileDescriptor)
		{
			if (fileDescriptor == null)
			{
				throw new ArgumentNullException();
			}
			this.fileDescriptor = fileDescriptor;
			InitializeComponent();
			InitializeUI();
		}

		public void RefreshByTimeRange(DateTime start, DateTime end)
		{
			if (fileDescriptor != null && end >= start)
			{
				try
				{
					if (fileDescriptor.FileBlockCount == 0)
					{
						lblLoadSize.Text = SR.GetString("FileBlockInfo_LoadingSize") + SR.GetString("FileBlockInfo_EmptyFile");
						lblLoadPercentage.Text = string.Empty;
						graphics.FillRectangle(selectedItemBrush, new Rectangle(new Point(0, 0), DrawPanel.Size));
					}
					else
					{
						long num = 0L;
						long num2 = 0L;
						long num3 = 0L;
						if (end > start)
						{
							foreach (FileBlockInfo fileBlock in fileDescriptor.FileBlocks)
							{
								long num4 = fileBlock.EndFileOffset - fileBlock.StartFileOffset;
								if (num4 > 0)
								{
									if ((fileBlock.StartDate >= start && fileBlock.EndDate <= end) || (fileBlock.StartDate <= start && fileBlock.EndDate >= start) || (fileBlock.StartDate <= end && fileBlock.EndDate >= end))
									{
										num2 += num4;
									}
									else if (fileBlock.EndDate < start)
									{
										num += num4;
									}
									else if (fileBlock.StartDate > end)
									{
										num3 += num4;
									}
								}
								else if (fileBlock.StartDate == fileBlock.EndDate)
								{
									if (fileBlock.StartDate >= start && fileBlock.StartDate <= end)
									{
										num2 += ((fileDescriptor.FileSize <= 5000000) ? fileDescriptor.FileSize : 5000000);
									}
									else if (fileBlock.StartDate < start)
									{
										num += ((fileDescriptor.FileSize <= 5000000) ? fileDescriptor.FileSize : 5000000);
									}
									else if (fileBlock.StartDate > end)
									{
										num3 += ((fileDescriptor.FileSize <= 5000000) ? fileDescriptor.FileSize : 5000000);
									}
								}
								else
								{
									num2 += ((fileDescriptor.FileSize <= 5000000) ? fileDescriptor.FileSize : 5000000);
								}
							}
						}
						else
						{
							num = fileDescriptor.FileSize;
							num3 = 0L;
							num2 = 0L;
						}
						graphics.FillRectangle(unselectedItemBrush, new Rectangle(new Point(0, 0), DrawPanel.Size));
						if (num2 != 0L)
						{
							int num5 = (int)((double)num / (double)fileDescriptor.FileSize * (double)DrawPanel.Size.Width);
							int num6 = (int)((double)num3 / (double)fileDescriptor.FileSize * (double)DrawPanel.Size.Width);
							graphics.FillRectangle(selectedItemBrush, new Rectangle(num5, 0, DrawPanel.Size.Width - num5 - num6, DrawPanel.Size.Height));
						}
						lblLoadSize.Text = SR.GetString("FileBlockInfo_LoadingSize") + Utilities.GetFileSizeString(num2);
						double num7 = 0.0;
						num7 = ((num2 + num + num3 == 0L) ? 0.0 : ((double)num2 / (double)(num2 + num + num3) * 100.0));
						lblLoadPercentage.Text = SR.GetString("FileBlockInfo_LoadingPre") + num7.ToString("###.##", CultureInfo.CurrentCulture);
						startDateTime = start;
						endDateTime = end;
					}
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
				}
			}
		}

		public void Close()
		{
			Dispose();
		}

		~FileBlockInfoControl()
		{
			Dispose(disposing: false);
		}

		private void InitializeUI()
		{
			selectedItemBrush = new SolidBrush(Utilities.GetColor(ApplicationColors.Highlight));
			unselectedItemBrush = new SolidBrush(Utilities.GetColor(ApplicationColors.GradientInactiveCaption));
			graphics = DrawPanel.CreateGraphics();
			if (!string.IsNullOrEmpty(fileDescriptor.FilePath))
			{
				lblFilePath.Text = Path.GetFileName(fileDescriptor.FilePath);
				ToolTip toolTip = new ToolTip();
				toolTip.AutoPopDelay = 10000;
				toolTip.InitialDelay = 100;
				toolTip.ReshowDelay = 100;
				toolTip.IsBalloon = true;
				toolTip.ToolTipIcon = ToolTipIcon.Info;
				toolTip.SetToolTip(lblFilePath, fileDescriptor.FilePath);
			}
		}

		private void DrawPanel_Paint(object sender, PaintEventArgs e)
		{
			RefreshByTimeRange(startDateTime, endDateTime);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
			if (isDisposed)
			{
				if (graphics != null)
				{
					graphics.Dispose();
				}
				if (selectedItemBrush != null)
				{
					selectedItemBrush.Dispose();
				}
				if (unselectedItemBrush != null)
				{
					unselectedItemBrush.Dispose();
				}
				GC.SuppressFinalize(this);
				isDisposed = true;
			}
		}

		private void InitializeComponent()
		{
			lblFilePath = new System.Windows.Forms.Label();
			DrawPanel = new System.Windows.Forms.Panel();
			lblLoadSize = new System.Windows.Forms.Label();
			lblLoadPercentage = new System.Windows.Forms.Label();
			SuspendLayout();
			lblFilePath.Size = new System.Drawing.Size(300, 20);
			lblFilePath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lblFilePath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			lblFilePath.Location = new System.Drawing.Point(5, 5);
			lblFilePath.Name = "lblFilePath";
			lblFilePath.TabIndex = 0;
			DrawPanel.Location = new System.Drawing.Point(5, 30);
			DrawPanel.Name = "DrawPanel";
			DrawPanel.Size = new System.Drawing.Size(510, 20);
			DrawPanel.TabIndex = 1;
			DrawPanel.Paint += new System.Windows.Forms.PaintEventHandler(DrawPanel_Paint);
			lblLoadSize.AutoSize = true;
			lblLoadSize.Location = new System.Drawing.Point(3, 53);
			lblLoadSize.Name = "lblLoadSize";
			lblLoadSize.Size = new System.Drawing.Size(67, 13);
			lblLoadSize.TabIndex = 3;
			lblLoadPercentage.AutoSize = true;
			lblLoadPercentage.Location = new System.Drawing.Point(258, 53);
			lblLoadPercentage.Name = "lblLoadPercentage";
			lblLoadPercentage.Size = new System.Drawing.Size(102, 13);
			lblLoadPercentage.TabIndex = 4;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(lblLoadPercentage);
			base.Controls.Add(lblLoadSize);
			base.Controls.Add(DrawPanel);
			base.Controls.Add(lblFilePath);
			base.Name = "FileBlockInfoControl";
			base.Size = new System.Drawing.Size(520, 70);
			ResumeLayout(performLayout: false);
			PerformLayout();
		}
	}
}
