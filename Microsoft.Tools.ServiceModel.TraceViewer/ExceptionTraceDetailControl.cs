using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class ExceptionTraceDetailControl : UserControl
	{
		private class ExceptionWrapper
		{
			private string exceptionType = string.Empty;

			private string exceptionMessage = string.Empty;

			private string nativeErrorCode = string.Empty;

			private List<string> stackTraceList = new List<string>();

			public string ExceptionType
			{
				get
				{
					return exceptionType;
				}
				set
				{
					exceptionType = value;
				}
			}

			public string ExceptionMessage
			{
				get
				{
					return exceptionMessage;
				}
				set
				{
					exceptionMessage = value;
				}
			}

			public string NativeErrorCode
			{
				get
				{
					return nativeErrorCode;
				}
				set
				{
					nativeErrorCode = value;
				}
			}

			public List<string> StackTraceList => stackTraceList;
		}

		private enum TabFocusIndex
		{
			MainPanel,
			ExceptionTreeLabel,
			ExceptionTree,
			ExceptionInfoGroup,
			ExceptionTypeLabel,
			ExceptionType,
			ExceptionMessageLabel,
			ExceptionMessage,
			NativeCodeLabel,
			NativeCode,
			StackTraceLabel,
			StackTraceList
		}

		private IContainer components;

		private Panel mainPanel;

		private Label lblExceptionTree;

		private TreeView exceptionTree;

		private GroupBox exceptionInfoGroup;

		private ListView stackTraceList;

		private ColumnHeader stackTraceMethodHeader;

		private TextBox nativeErrorCode;

		private TextBox exceptionMessage;

		private TextBox exceptionType;

		private Label lblStackTrace;

		private Label lblNativeErrorCode;

		private Label lblMessage;

		private Label lblExceptionType;

		public ExceptionTraceDetailControl()
		{
			InitializeComponent();
		}

		public void CleanUp()
		{
			exceptionTree.Nodes.Clear();
			exceptionType.Clear();
			exceptionMessage.Clear();
			nativeErrorCode.Clear();
			stackTraceList.Items.Clear();
		}

		private ExceptionWrapper ExtractException(XmlElement node, out bool hasInnerException)
		{
			ExceptionWrapper exceptionWrapper = null;
			hasInnerException = false;
			if (node != null)
			{
				exceptionWrapper = new ExceptionWrapper();
				if (node["ExceptionType"] != null)
				{
					exceptionWrapper.ExceptionType = node["ExceptionType"].InnerText;
				}
				if (node["Message"] != null)
				{
					exceptionWrapper.ExceptionMessage = node["Message"].InnerText;
				}
				if (node["NativeErrorCode"] != null)
				{
					exceptionWrapper.NativeErrorCode = node["NativeErrorCode"].InnerText;
				}
				if (node["InnerException"] != null)
				{
					hasInnerException = true;
				}
				if (node["StackTrace"] != null)
				{
					string innerText = node["StackTrace"].InnerText;
					innerText = innerText.Trim();
					if (innerText.StartsWith("at ", StringComparison.OrdinalIgnoreCase))
					{
						innerText = innerText.Substring("at ".Length);
					}
					string[] array = innerText.Split(new string[1]
					{
						" at "
					}, StringSplitOptions.RemoveEmptyEntries);
					if (array != null)
					{
						string[] array2 = array;
						foreach (string text in array2)
						{
							exceptionWrapper.StackTraceList.Add(text.Trim());
						}
					}
				}
			}
			return exceptionWrapper;
		}

		public void ReloadExceptions(List<string> exceptionXmlList)
		{
			CleanUp();
			if (exceptionXmlList != null)
			{
				foreach (string exceptionXml in exceptionXmlList)
				{
					DisplayExceptionTree(exceptionXml);
				}
				if (exceptionTree.Nodes.Count != 0)
				{
					exceptionTree.Nodes[0].ExpandAll();
					exceptionTree.SelectedNode = exceptionTree.Nodes[0];
				}
			}
		}

		private void DisplayExceptionTree(string exceptionXml)
		{
			if (!string.IsNullOrEmpty(exceptionXml))
			{
				CleanUp();
				try
				{
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.LoadXml(exceptionXml);
					XmlElement documentElement = xmlDocument.DocumentElement;
					if (documentElement != null && string.Compare(documentElement.Name, "Exception",  true, CultureInfo.CurrentUICulture) == 0)
					{
						bool hasInnerException = false;
						ExceptionWrapper exceptionWrapper = ExtractException(documentElement, out hasInnerException);
						if (exceptionWrapper != null)
						{
							TreeNode treeNode = new TreeNode(exceptionWrapper.ExceptionType);
							exceptionTree.Nodes.Add(treeNode);
							treeNode.Tag = exceptionWrapper;
							treeNode.ToolTipText = exceptionWrapper.ExceptionMessage;
							TreeNode treeNode2 = exceptionTree.Nodes[0];
							XmlElement xmlElement = documentElement["InnerException"];
							while (hasInnerException && xmlElement != null)
							{
								exceptionWrapper = ExtractException(xmlElement, out hasInnerException);
								if (exceptionWrapper == null)
								{
									break;
								}
								treeNode2.Nodes.Add(exceptionWrapper.ExceptionType);
								treeNode2.Nodes[0].Tag = exceptionWrapper;
								treeNode2.Nodes[0].ToolTipText = exceptionWrapper.ExceptionMessage;
								treeNode2 = treeNode2.Nodes[0];
								xmlElement = xmlElement["InnerException"];
							}
						}
					}
				}
				catch (XmlException e)
				{
					throw new TraceViewerException(SR.GetString("FV_ERROR"), e);
				}
			}
		}

		private void exceptionTree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (e.Node.Tag != null && e.Node.Tag is ExceptionWrapper)
			{
				ExceptionWrapper exceptionWrapper = (ExceptionWrapper)e.Node.Tag;
				exceptionType.Text = exceptionWrapper.ExceptionType;
				exceptionMessage.Text = exceptionWrapper.ExceptionMessage;
				nativeErrorCode.Text = exceptionWrapper.NativeErrorCode;
				stackTraceList.Items.Clear();
				foreach (string stackTrace in exceptionWrapper.StackTraceList)
				{
					string text = stackTrace.Trim();
					if (!string.IsNullOrEmpty(text))
					{
						ListViewItem listViewItem = new ListViewItem(text);
						if (!text.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
						{
							ListViewItem listViewItem2 = listViewItem;
							listViewItem2.Font = new Font(listViewItem2.Font, FontStyle.Bold);
						}
						stackTraceList.Items.Add(listViewItem);
					}
				}
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
			lblExceptionTree = new System.Windows.Forms.Label();
			exceptionTree = new System.Windows.Forms.TreeView();
			exceptionInfoGroup = new System.Windows.Forms.GroupBox();
			stackTraceList = new System.Windows.Forms.ListView();
			stackTraceMethodHeader = new System.Windows.Forms.ColumnHeader();
			nativeErrorCode = new System.Windows.Forms.TextBox();
			exceptionMessage = new System.Windows.Forms.TextBox();
			exceptionType = new System.Windows.Forms.TextBox();
			lblStackTrace = new System.Windows.Forms.Label();
			lblNativeErrorCode = new System.Windows.Forms.Label();
			lblMessage = new System.Windows.Forms.Label();
			lblExceptionType = new System.Windows.Forms.Label();
			mainPanel.SuspendLayout();
			exceptionInfoGroup.SuspendLayout();
			SuspendLayout();
			mainPanel.Controls.Add(exceptionInfoGroup);
			mainPanel.Controls.Add(lblExceptionTree);
			mainPanel.Controls.Add(exceptionTree);
			mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			mainPanel.Location = new System.Drawing.Point(0, 0);
			mainPanel.Name = "mainPanel";
			mainPanel.Size = new System.Drawing.Size(430, 385);
			mainPanel.TabIndex = 0;
			lblExceptionTree.Location = new System.Drawing.Point(5, 0);
			lblExceptionTree.Name = "lblExceptionTree";
			lblExceptionTree.Size = new System.Drawing.Size(100, 20);
			lblExceptionTree.TabIndex = 1;
			lblExceptionTree.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_Exp_ExpTree");
			exceptionTree.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			exceptionTree.FullRowSelect = true;
			exceptionTree.HideSelection = false;
			exceptionTree.Location = new System.Drawing.Point(5, 20);
			exceptionTree.Name = "exceptionTree";
			exceptionTree.ShowNodeToolTips = true;
			exceptionTree.Size = new System.Drawing.Size(420, 68);
			exceptionTree.TabIndex = 2;
			exceptionTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(exceptionTree_AfterSelect);
			exceptionInfoGroup.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			exceptionInfoGroup.Controls.Add(stackTraceList);
			exceptionInfoGroup.Controls.Add(nativeErrorCode);
			exceptionInfoGroup.Controls.Add(exceptionMessage);
			exceptionInfoGroup.Controls.Add(exceptionType);
			exceptionInfoGroup.Controls.Add(lblStackTrace);
			exceptionInfoGroup.Controls.Add(lblNativeErrorCode);
			exceptionInfoGroup.Controls.Add(lblMessage);
			exceptionInfoGroup.Controls.Add(lblExceptionType);
			exceptionInfoGroup.Location = new System.Drawing.Point(5, 95);
			exceptionInfoGroup.Name = "exceptionInfoGroup";
			exceptionInfoGroup.Size = new System.Drawing.Size(420, 325);
			exceptionInfoGroup.TabIndex = 3;
			exceptionInfoGroup.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_Exp_ExpInfo");
			stackTraceList.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			stackTraceList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[1]
			{
				stackTraceMethodHeader
			});
			stackTraceList.HideSelection = false;
			stackTraceList.Location = new System.Drawing.Point(110, 190);
			stackTraceList.Name = "stackTraceList";
			stackTraceList.ShowItemToolTips = true;
			stackTraceList.Size = new System.Drawing.Size(300, 125);
			stackTraceList.TabIndex = 11;
			stackTraceList.UseCompatibleStateImageBehavior = false;
			stackTraceList.View = System.Windows.Forms.View.Details;
			stackTraceMethodHeader.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_List_MethodCol");
			stackTraceMethodHeader.Width = 295;
			nativeErrorCode.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			nativeErrorCode.Location = new System.Drawing.Point(110, 160);
			nativeErrorCode.Name = "nativeErrorCode";
			nativeErrorCode.ReadOnly = true;
			nativeErrorCode.Size = new System.Drawing.Size(300, 20);
			nativeErrorCode.TabIndex = 9;
			exceptionMessage.AcceptsReturn = true;
			exceptionMessage.AcceptsTab = false;
			exceptionMessage.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			exceptionMessage.Location = new System.Drawing.Point(110, 70);
			exceptionMessage.Multiline = true;
			exceptionMessage.Name = "exceptionMessage";
			exceptionMessage.ReadOnly = true;
			exceptionMessage.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			exceptionMessage.Size = new System.Drawing.Size(300, 80);
			exceptionMessage.TabIndex = 7;
			exceptionType.AcceptsReturn = true;
			exceptionType.AcceptsTab = false;
			exceptionType.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			exceptionType.Location = new System.Drawing.Point(110, 20);
			exceptionType.Multiline = true;
			exceptionType.Name = "exceptionType";
			exceptionType.ReadOnly = true;
			exceptionType.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			exceptionType.Size = new System.Drawing.Size(300, 40);
			exceptionType.TabIndex = 5;
			lblStackTrace.Location = new System.Drawing.Point(5, 190);
			lblStackTrace.Name = "lblStackTrace";
			lblStackTrace.Size = new System.Drawing.Size(100, 20);
			lblStackTrace.TabIndex = 10;
			lblStackTrace.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_Exp_StackTrace");
			lblNativeErrorCode.Location = new System.Drawing.Point(5, 160);
			lblNativeErrorCode.Name = "lblNativeErrorCode";
			lblNativeErrorCode.Size = new System.Drawing.Size(100, 20);
			lblNativeErrorCode.TabIndex = 8;
			lblNativeErrorCode.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_Exp_NativeCode");
			lblMessage.Location = new System.Drawing.Point(5, 70);
			lblMessage.Name = "lblMessage";
			lblMessage.Size = new System.Drawing.Size(100, 20);
			lblMessage.TabIndex = 6;
			lblMessage.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_Exp_Message");
			lblExceptionType.Location = new System.Drawing.Point(5, 20);
			lblExceptionType.Name = "lblExceptionType";
			lblExceptionType.Size = new System.Drawing.Size(100, 20);
			lblExceptionType.TabIndex = 4;
			lblExceptionType.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_Exp_ExpType");
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			BackColor = System.Drawing.SystemColors.Window;
			base.Controls.Add(mainPanel);
			base.Name = "ExceptionTraceDetailControl";
			base.Size = new System.Drawing.Size(430, 425);
			mainPanel.ResumeLayout(performLayout: false);
			exceptionInfoGroup.ResumeLayout(performLayout: false);
			exceptionInfoGroup.PerformLayout();
			ResumeLayout(performLayout: false);
		}
	}
}
