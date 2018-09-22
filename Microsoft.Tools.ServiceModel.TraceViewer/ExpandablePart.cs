using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal abstract class ExpandablePart : UserControl
	{
		public delegate void ExpandablePartStateChanged(ExpandablePart part);

		private enum TabFocusIndex
		{
			RightPanel,
			RightControl
		}

		private ExpandablePartStateChanged expandablePartStateChangedCallback;

		private int preferredHeight;

		private Image plusImage;

		private Image minusImage;

		private Control rightPart;

		private List<ListView> internalListViewControls = new List<ListView>();

		protected const int EDGE_SIZE = 30;

		private const int SMALL_HEIGHT = 30;

		private const int LISTVIEW_COLUMN_HEADER_EDGE = 25;

		private const int DEFAULT_NAME_COLUMN_WIDTH = 150;

		private const int DEFAULT_RESIZING_COLUMN_WIDTH = 300;

		private const double DEFAULT_VALUE_COLUMN_WIDTH_PERCENTAGE = 0.8;

		private const int INIT_CTRL_PROBING_LAYER = 0;

		private const int MAX_CTRL_PROBING_LAYER = 3;

		private IContainer components;

		private SplitContainer splitContainerParent;

		private SplitContainer splitContainer;

		private Label lblPartName;

		private Panel rightPanel;

		private PictureBox btnExpand;

		protected abstract string ExpandablePartName
		{
			get;
		}

		protected ExpandablePart()
		{
			InitializeComponent();
			plusImage = TempFileManager.GetImageFromEmbededResources(Images.PlusIcon);
			minusImage = TempFileManager.GetImageFromEmbededResources(Images.MinusIcon);
			DoubleBuffered = true;
		}

		protected void UpdateUIElements()
		{
			foreach (ListView internalListViewControl in internalListViewControls)
			{
				ResizeListViewColumns(internalListViewControl);
			}
		}

		protected void SetupRightPart(Control rightPart, ExpandablePartStateChanged callback, int preferredHeight)
		{
			if (rightPart != null)
			{
				this.rightPart = rightPart;
				this.rightPart.TabIndex = 1;
				rightPanel.Controls.Add(this.rightPart);
				InitListViewControls();
				expandablePartStateChangedCallback = callback;
				lblPartName.Text = ExpandablePartName;
				this.preferredHeight = preferredHeight;
				ExpandPart();
				Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
			}
		}

		private void AttachEventsForListView(Control ctrl)
		{
			if (ctrl != null && ctrl is ListView)
			{
				ListView listView = (ListView)ctrl;
				listView.KeyDown += lv_KeyDown;
				if (!internalListViewControls.Contains(listView))
				{
					internalListViewControls.Add(listView);
				}
			}
		}

		private void AttachEventsForTreeView(Control ctrl)
		{
			if (ctrl != null && ctrl is TreeView)
			{
				((TreeView)ctrl).KeyDown += tv_KeyDown;
			}
		}

		private void tv_KeyDown(object sender, KeyEventArgs e)
		{
			if (sender != null && sender is TreeView && e.Control && e.KeyCode == Keys.C)
			{
				CopySelectedTreeViewNodeToClipboard((TreeView)sender);
			}
		}

		private void InitListViewControls()
		{
			if (rightPart != null)
			{
				AttachEventsForListView(rightPart);
				AttachEventsForTreeView(rightPart);
				foreach (Control control in rightPart.Controls)
				{
					ProbingControls(control, 0);
				}
			}
		}

		private void ProbingControls(Control ctrl, int depth)
		{
			if (ctrl != null && depth < 3)
			{
				AttachEventsForListView(ctrl);
				AttachEventsForTreeView(ctrl);
				foreach (Control control in ctrl.Controls)
				{
					if (ctrl.HasChildren)
					{
						ProbingControls(control, depth + 1);
					}
				}
			}
		}

		private void lv_KeyDown(object sender, KeyEventArgs e)
		{
			if (sender != null && sender is ListView && e.Control)
			{
				ListView listView = (ListView)sender;
				if (e.KeyCode == Keys.C)
				{
					CopySelectedListViewItemsToClipboard(listView);
				}
				else if (e.KeyCode == Keys.A)
				{
					foreach (ListViewItem item in listView.Items)
					{
						item.Selected = true;
					}
				}
			}
		}

		private void CopySelectedListViewItemsToClipboard(ListView lv)
		{
			if (lv != null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (ListViewItem selectedItem in lv.SelectedItems)
				{
					for (int i = 0; i < lv.Columns.Count; i++)
					{
						if (i != 0)
						{
							stringBuilder.Append('\t');
						}
						stringBuilder.Append(selectedItem.SubItems[i].Text);
					}
					stringBuilder.AppendLine();
				}
				Utilities.CopyTextToClipboard(stringBuilder.ToString());
			}
		}

		private void CopySelectedTreeViewNodeToClipboard(TreeView tv)
		{
			if (tv != null && tv.SelectedNode != null)
			{
				Utilities.CopyTextToClipboard(tv.SelectedNode.Text);
			}
		}

		private void ResizeListViewColumns(ListView lv)
		{
			if (lv != null)
			{
				if (lv.Columns.Count == 1)
				{
					lv.Columns[0].Width = lv.Width - 25;
				}
				else if (lv.Width > 300 && lv.Columns.Count >= 2)
				{
					if (lv.Columns.Count == 2)
					{
						lv.Columns[0].Width = 150;
						lv.Columns[1].Width = lv.Width - lv.Columns[0].Width - 25;
					}
					else
					{
						lv.Columns[0].Width = 150;
						lv.Columns[1].Width = (int)((double)(lv.Width - lv.Columns[0].Width) * 0.8);
						int width = (lv.Width - lv.Columns[0].Width - lv.Columns[1].Width - 25) / (lv.Columns.Count - 2);
						for (int i = 2; i < lv.Columns.Count; i++)
						{
							lv.Columns[i].Width = width;
						}
					}
				}
			}
		}

		private void ExpandPart()
		{
			if (rightPart != null)
			{
				btnExpand.Tag = true;
				base.Height = preferredHeight;
				btnExpand.Image = minusImage;
				rightPart.Visible = true;
			}
		}

		private void CollapsePart()
		{
			if (rightPart != null)
			{
				btnExpand.Tag = false;
				base.Height = 30;
				btnExpand.Image = plusImage;
				rightPart.Visible = false;
			}
		}

		public virtual Size GetCurrentSize()
		{
			return base.Size;
		}

		public virtual Control GetExpandablePart()
		{
			return this;
		}

		public abstract void ReloadTracePart(TraceDetailedProcessParameter parameter);

		private void PerformExpand()
		{
			if (base.Parent != null)
			{
				base.Parent.Focus();
			}
			bool flag = (bool)btnExpand.Tag;
			if (flag)
			{
				CollapsePart();
			}
			else
			{
				ExpandPart();
			}
			btnExpand.Tag = !flag;
			lblPartName.Text = ExpandablePartName;
			ExpandablePartStateChangedCallbackHelper();
		}

		private void ExpandablePartStateChangedCallbackHelper()
		{
			try
			{
				if (expandablePartStateChangedCallback != null)
				{
					expandablePartStateChangedCallback(this);
				}
			}
			catch (TargetInvocationException)
			{
			}
			catch (TargetException)
			{
			}
			catch (MemberAccessException)
			{
			}
		}

		private void btnExpand_Click(object sender, EventArgs e)
		{
			PerformExpand();
		}

		private void lblPartName_DoubleClick(object sender, EventArgs e)
		{
			PerformExpand();
		}

		private void lblPartName_MouseClick(object sender, MouseEventArgs e)
		{
			if (base.Parent != null)
			{
				base.Parent.Focus();
			}
		}

		private void Panel1_MouseClick(object sender, MouseEventArgs e)
		{
			if (base.Parent != null)
			{
				base.Parent.Focus();
			}
		}

		private void Panel2_MouseClick(object sender, MouseEventArgs e)
		{
			if (base.Parent != null)
			{
				base.Parent.Focus();
			}
		}

		private void rightPanel_Click(object sender, EventArgs e)
		{
			if (base.Parent != null)
			{
				base.Parent.Focus();
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
			splitContainerParent = new System.Windows.Forms.SplitContainer();
			btnExpand = new System.Windows.Forms.PictureBox();
			splitContainer = new System.Windows.Forms.SplitContainer();
			lblPartName = new System.Windows.Forms.Label();
			rightPanel = new System.Windows.Forms.Panel();
			splitContainerParent.Panel1.SuspendLayout();
			splitContainerParent.Panel2.SuspendLayout();
			splitContainerParent.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)btnExpand).BeginInit();
			splitContainer.Panel1.SuspendLayout();
			splitContainer.Panel2.SuspendLayout();
			splitContainer.SuspendLayout();
			SuspendLayout();
			splitContainerParent.Dock = System.Windows.Forms.DockStyle.Fill;
			splitContainerParent.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			splitContainerParent.IsSplitterFixed = true;
			splitContainerParent.Location = new System.Drawing.Point(0, 0);
			splitContainerParent.Margin = new System.Windows.Forms.Padding(2);
			splitContainerParent.Name = "splitContainerParent";
			splitContainerParent.Panel1.Controls.Add(btnExpand);
			splitContainerParent.Panel1.MouseClick += new System.Windows.Forms.MouseEventHandler(Panel1_MouseClick);
			splitContainerParent.Panel1MinSize = 8;
			splitContainerParent.Panel2.Controls.Add(splitContainer);
			splitContainerParent.Panel2.MouseClick += new System.Windows.Forms.MouseEventHandler(Panel2_MouseClick);
			splitContainerParent.Panel2MinSize = 8;
			splitContainerParent.Size = new System.Drawing.Size(448, 500);
			splitContainerParent.SplitterDistance = 10;
			splitContainerParent.TabStop = false;
			btnExpand.Dock = System.Windows.Forms.DockStyle.Top;
			btnExpand.Location = new System.Drawing.Point(0, 0);
			btnExpand.Name = "btnExpand";
			btnExpand.Size = new System.Drawing.Size(10, 10);
			btnExpand.TabStop = false;
			btnExpand.Click += new System.EventHandler(btnExpand_Click);
			splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			splitContainer.IsSplitterFixed = true;
			splitContainer.Location = new System.Drawing.Point(0, 0);
			splitContainer.Name = "splitContainer";
			splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			splitContainer.TabStop = false;
			splitContainer.Size = new System.Drawing.Size(434, 500);
			splitContainer.SplitterDistance = 20;
			splitContainer.SplitterWidth = 1;
			splitContainer.Panel1.Controls.Add(lblPartName);
			splitContainer.Panel1MinSize = 20;
			splitContainer.Panel2.Controls.Add(rightPanel);
			splitContainer.Panel2MinSize = 0;
			lblPartName.Dock = System.Windows.Forms.DockStyle.Fill;
			lblPartName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
			lblPartName.Location = new System.Drawing.Point(0, 0);
			lblPartName.Name = "lblPartName";
			lblPartName.Size = new System.Drawing.Size(434, 20);
			lblPartName.TabStop = false;
			lblPartName.DoubleClick += new System.EventHandler(lblPartName_DoubleClick);
			lblPartName.MouseClick += new System.Windows.Forms.MouseEventHandler(lblPartName_MouseClick);
			rightPanel.AutoScroll = true;
			rightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			rightPanel.Location = new System.Drawing.Point(0, 0);
			rightPanel.Name = "rightPanel";
			rightPanel.Size = new System.Drawing.Size(434, 479);
			rightPanel.TabIndex = 0;
			rightPanel.Click += new System.EventHandler(rightPanel_Click);
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			BackColor = System.Drawing.SystemColors.Window;
			base.BorderStyle = System.Windows.Forms.BorderStyle.None;
			base.Controls.Add(splitContainerParent);
			base.Name = "ExpandablePart";
			base.Size = new System.Drawing.Size(448, 500);
			splitContainerParent.Panel1.ResumeLayout(performLayout: false);
			splitContainerParent.Panel2.ResumeLayout(performLayout: false);
			splitContainerParent.ResumeLayout(performLayout: false);
			((System.ComponentModel.ISupportInitialize)btnExpand).EndInit();
			splitContainer.Panel1.ResumeLayout(performLayout: false);
			splitContainer.Panel2.ResumeLayout(performLayout: false);
			splitContainer.ResumeLayout(performLayout: false);
			ResumeLayout(performLayout: false);
		}
	}
}
