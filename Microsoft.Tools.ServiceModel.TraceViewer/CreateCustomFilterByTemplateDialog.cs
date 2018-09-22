using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class CreateCustomFilterByTemplateDialog : Form
	{
		private class TreeNodeTagBase
		{
			private string xpathExpression0;

			private string xpathExpression1;

			private string name;

			private XmlNode xmlNode;

			public string XpathExpression0
			{
				get
				{
					return xpathExpression0;
				}
				set
				{
					xpathExpression0 = value;
				}
			}

			public string XpathExpression1
			{
				get
				{
					return xpathExpression1;
				}
				set
				{
					xpathExpression1 = value;
				}
			}

			public string Name
			{
				set
				{
					name = value;
				}
			}

			public XmlNode XmlNode
			{
				get
				{
					return xmlNode;
				}
				set
				{
					xmlNode = value;
				}
			}
		}

		private class TreeAttributeNodeTag : TreeNodeTagBase
		{
			private string value;

			public string Value
			{
				get
				{
					return value;
				}
				set
				{
					this.value = value;
				}
			}
		}

		private class TreeXmlNodeNodeTag : TreeNodeTagBase
		{
		}

		private enum TabFocusIndex
		{
			OkButton,
			CancelButton,
			TreeView,
			CriteriaGrid,
			FilterNameLabel,
			FilterNameText,
			FilterDescriptionLabel,
			FilterDescriptionText
		}

		private CustomFilter currentFilter;

		private CustomFilterDialog parent;

		private IErrorReport errorReport;

		private XmlDocument xmlDoc;

		private Dictionary<string, string> nsMap = new Dictionary<string, string>();

		private int prefixIndex;

		private const int EXPRESSION_COLUMN_INDEX = 0;

		private const int OPERATION_COLUMN_INDEX = 1;

		private const int VALUE_COLUMN_INDEX = 2;

		private const int PARAMETER_COLUMN_INDEX = 3;

		private readonly int sumOfColumnWidth;

		private IContainer components;

		private Panel leftPanel;

		private Splitter splitter;

		private Panel rightPanel;

		private TreeView treeView;

		private Splitter splitter1;

		private Panel xpathPanel;

		private TextBox xpathExpression;

		private DataGridView logicGrid;

		private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;

		private DataGridViewComboBoxColumn dataGridViewComboBoxColumn2;

		private DataGridViewTextBoxColumn dataGridViewCheckBoxColumn2;

		private DataGridViewComboBoxColumn dataGridViewComboBoxColumn3;

		private Label lblXPath;

		private Button btnOk;

		private Button btnCancel;

		private Label lblName;

		private TextBox filterName;

		private Label lblDescription;

		private TextBox filterDescription;

		private ToolStrip toolStrip1;

		private ToolStripButton btnAddCriteria;

		internal CustomFilter CurrentFilter => currentFilter;

		public CreateCustomFilterByTemplateDialog(CustomFilterDialog parent, IErrorReport errorReport)
		{
			InitializeComponent();
			this.parent = parent;
			this.errorReport = errorReport;
			sumOfColumnWidth = 0;
			foreach (DataGridViewColumn column in logicGrid.Columns)
			{
				sumOfColumnWidth += column.Width;
			}
		}

		public void Initialize(string templateXml)
		{
			try
			{
				xmlDoc = new XmlDocument();
				xmlDoc.LoadXml(templateXml);
				InitializeXmlTree();
				ExpandXmlTree();
				InitializeGrid();
			}
			catch (XmlException innerException)
			{
				throw new AppSettingsException(SR.GetString("CF_Err18"), innerException);
			}
		}

		private void InitializeXmlTree()
		{
			CreateNodeByXmlNode(xmlDoc.ChildNodes[0], null, string.Empty);
		}

		private void InitializeGrid()
		{
			string[] dataSource = new string[2]
			{
				SR.GetString("CF_Ext"),
				SR.GetString("CF_Equal")
			};
			string[] dataSource2 = new string[5]
			{
				SR.GetString("CF_None"),
				"{0}",
				"{1}",
				"{2}",
				"{3}"
			};
			((DataGridViewComboBoxColumn)logicGrid.Columns[1]).DataSource = dataSource;
			((DataGridViewComboBoxColumn)logicGrid.Columns[3]).DataSource = dataSource2;
		}

		private string GetNamespacePrefix(XmlAttribute attr)
		{
			if (attr != null && !string.IsNullOrEmpty(attr.NamespaceURI))
			{
				if (!nsMap.ContainsKey(attr.NamespaceURI))
				{
					nsMap.Add(attr.NamespaceURI, SR.GetString("CF_Prefix") + prefixIndex.ToString(CultureInfo.InvariantCulture));
					prefixIndex++;
				}
				return nsMap[attr.NamespaceURI] + SR.GetString("CF_Sep");
			}
			return string.Empty;
		}

		private string GetNamespacePrefix(XmlNode node)
		{
			if (node != null && !string.IsNullOrEmpty(node.NamespaceURI))
			{
				if (!nsMap.ContainsKey(node.NamespaceURI))
				{
					nsMap.Add(node.NamespaceURI, SR.GetString("CF_Prefix") + prefixIndex.ToString(CultureInfo.InvariantCulture));
					prefixIndex++;
				}
				return nsMap[node.NamespaceURI] + SR.GetString("CF_Sep");
			}
			return string.Empty;
		}

		private string NormalizeXPathElementName(string s)
		{
			if (!string.IsNullOrEmpty(s))
			{
				if (!s.Contains(":"))
				{
					return s;
				}
				string[] array = s.Split(':');
				if (array != null)
				{
					string[] array2 = array;
					return array2[array2.Length - 1];
				}
			}
			return SR.GetString("CF_NoNodeName");
		}

		private void ExpandXmlTree()
		{
			if (treeView.Nodes.Count != 0)
			{
				foreach (TreeNode node in treeView.Nodes)
				{
					node.Expand();
					if (node.Nodes.Count != 0)
					{
						foreach (TreeNode node2 in node.Nodes)
						{
							node2.Expand();
						}
					}
				}
			}
		}

		private void CreateNodeByXmlNode(XmlNode node, TreeNode treeNode, string xpathRoot)
		{
			try
			{
				TreeNode treeNode2 = new TreeNode(node.Name);
				TreeXmlNodeNodeTag treeXmlNodeNodeTag = new TreeXmlNodeNodeTag();
				treeXmlNodeNodeTag.XpathExpression0 = xpathRoot + "/" + GetNamespacePrefix(node) + NormalizeXPathElementName(node.Name);
				treeXmlNodeNodeTag.XpathExpression1 = xpathRoot + "/" + GetNamespacePrefix(node) + NormalizeXPathElementName(node.Name) + "[text()='{0}']";
				treeXmlNodeNodeTag.XmlNode = node;
				treeNode2.Tag = treeXmlNodeNodeTag;
				if (node.Attributes != null && node.Attributes.Count != 0)
				{
					TreeNode treeNode3 = new TreeNode(SR.GetString("CF_Attr2"));
					treeNode3.Tag = null;
					foreach (XmlAttribute attribute in node.Attributes)
					{
						if (!attribute.Name.Contains("xmlns"))
						{
							TreeNode treeNode4 = new TreeNode(attribute.Name + SR.GetString("CF_Equal") + attribute.Value);
							TreeAttributeNodeTag treeAttributeNodeTag = new TreeAttributeNodeTag();
							treeAttributeNodeTag.XpathExpression0 = xpathRoot + "/" + GetNamespacePrefix(node) + NormalizeXPathElementName(node.Name) + "[@" + GetNamespacePrefix(attribute) + NormalizeXPathElementName(attribute.Name) + "]";
							treeAttributeNodeTag.XpathExpression1 = xpathRoot + "/" + GetNamespacePrefix(node) + NormalizeXPathElementName(node.Name) + "[@" + GetNamespacePrefix(attribute) + NormalizeXPathElementName(attribute.Name) + "='{0}']";
							treeAttributeNodeTag.Name = attribute.Name;
							treeAttributeNodeTag.Value = attribute.Value;
							treeAttributeNodeTag.XmlNode = node;
							treeNode4.Tag = treeAttributeNodeTag;
							treeNode3.Nodes.Add(treeNode4);
						}
					}
					if (treeNode3.Nodes.Count != 0)
					{
						treeNode2.Nodes.Add(treeNode3);
					}
				}
				if (node.HasChildNodes)
				{
					foreach (XmlNode childNode in node.ChildNodes)
					{
						if (!(childNode.Name == "#text"))
						{
							CreateNodeByXmlNode(childNode, treeNode2, xpathRoot + "/" + GetNamespacePrefix(node) + NormalizeXPathElementName(node.Name));
						}
					}
				}
				if (treeNode == null)
				{
					treeView.Nodes.Add(treeNode2);
				}
				else
				{
					treeNode.Nodes.Add(treeNode2);
				}
			}
			catch (Exception ex)
			{
				ExceptionManager.GeneralExceptionFilter(ex);
				throw new AppSettingsException(SR.GetString("CF_Err17"), ex);
			}
		}

		private bool UpdateXPathExpression()
		{
			bool flag = true;
			string text = string.Empty;
			foreach (DataGridViewRow item in (IEnumerable)logicGrid.Rows)
			{
				text += " ";
				if (!flag)
				{
					text += "| ";
				}
				text = ((!(item.Cells[1].Value.ToString() == SR.GetString("CF_Ext"))) ? (text + string.Format(CultureInfo.CurrentUICulture, item.Cells[0].Value.ToString(), new object[1]
				{
					(!(item.Cells[3].Value.ToString() == SR.GetString("CF_None"))) ? item.Cells[3].Value.ToString() : ((item.Cells[2].Value != null) ? item.Cells[2].Value.ToString() : string.Empty)
				})) : (text + item.Cells[0].Value.ToString() + " "));
				flag = false;
			}
			xpathExpression.Text = text.Trim();
			return true;
		}

		private void AppendTreeNode(TreeNode node)
		{
			DataGridViewRow dataGridViewRow = new DataGridViewRow();
			if (node != null && node.Tag is TreeAttributeNodeTag)
			{
				TreeAttributeNodeTag treeAttributeNodeTag = (TreeAttributeNodeTag)node.Tag;
				dataGridViewRow.CreateCells(logicGrid);
				dataGridViewRow.Cells[0].Value = treeAttributeNodeTag.XpathExpression1;
				dataGridViewRow.Cells[1].Value = SR.GetString("CF_Equal");
				dataGridViewRow.Cells[2].Value = ((!string.IsNullOrEmpty(treeAttributeNodeTag.Value)) ? treeAttributeNodeTag.Value : string.Empty);
				dataGridViewRow.Cells[3].Value = SR.GetString("CF_None");
				dataGridViewRow.Tag = treeAttributeNodeTag;
				logicGrid.Rows.Add(dataGridViewRow);
			}
			else if (node != null && node.Tag is TreeXmlNodeNodeTag)
			{
				TreeXmlNodeNodeTag treeXmlNodeNodeTag = (TreeXmlNodeNodeTag)node.Tag;
				dataGridViewRow.CreateCells(logicGrid);
				dataGridViewRow.Cells[0].Value = treeXmlNodeNodeTag.XpathExpression0;
				dataGridViewRow.Cells[1].Value = SR.GetString("CF_Ext");
				dataGridViewRow.Cells[2].Value = treeXmlNodeNodeTag.XmlNode.InnerText;
				dataGridViewRow.Cells[3].Value = SR.GetString("CF_None");
				dataGridViewRow.Tag = treeXmlNodeNodeTag;
				logicGrid.Rows.Add(dataGridViewRow);
			}
			if (dataGridViewRow.Cells[1].Value.ToString() == SR.GetString("CF_Ext"))
			{
				dataGridViewRow.Cells[2].ReadOnly = true;
				dataGridViewRow.Cells[2].Style = new DataGridViewCellStyle();
				dataGridViewRow.Cells[2].Style.ForeColor = Color.Gray;
				dataGridViewRow.Cells[3].ReadOnly = true;
				dataGridViewRow.Cells[3].Style = new DataGridViewCellStyle();
				dataGridViewRow.Cells[3].Style.ForeColor = Color.Gray;
			}
		}

		private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (e.Action != TreeViewAction.Collapse && e.Action != TreeViewAction.Expand)
			{
				if (treeView.SelectedNode != null && treeView.SelectedNode != treeView.Nodes[0] && treeView.SelectedNode.Tag != null)
				{
					btnAddCriteria.Enabled = true;
				}
				else
				{
					btnAddCriteria.Enabled = false;
				}
			}
		}

		private bool UpdateGridCellOnChanged(int rowIndex, int colIndex)
		{
			if (rowIndex >= 0 && colIndex >= 0 && rowIndex < logicGrid.RowCount && colIndex < logicGrid.ColumnCount)
			{
				if (logicGrid.Rows[rowIndex].Cells[0].Value == null)
				{
					errorReport.ReportErrorToUser(SR.GetString("CF_InvalidLogic"));
					return false;
				}
				DataGridViewRow dataGridViewRow = logicGrid.Rows[rowIndex];
				TreeNodeTagBase treeNodeTagBase = (TreeNodeTagBase)dataGridViewRow.Tag;
				if (dataGridViewRow.Cells[1].Value.ToString() == SR.GetString("CF_Ext"))
				{
					dataGridViewRow.Cells[0].Value = treeNodeTagBase.XpathExpression0;
				}
				else if (dataGridViewRow.Cells[1].Value.ToString() == SR.GetString("CF_Equal"))
				{
					dataGridViewRow.Cells[0].Value = treeNodeTagBase.XpathExpression1;
				}
				return UpdateXPathExpression();
			}
			errorReport.ReportErrorToUser(SR.GetString("CF_InvalidLogic"));
			return false;
		}

		private void logicGrid_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
		{
			if (e.ColumnIndex == 3 && !logicGrid.Rows[e.RowIndex].Cells[3].ReadOnly)
			{
				if (e.FormattedValue.ToString() != SR.GetString("CF_None"))
				{
					logicGrid.Rows[e.RowIndex].Cells[2].ReadOnly = true;
					logicGrid.Rows[e.RowIndex].Cells[2].Style = new DataGridViewCellStyle();
					logicGrid.Rows[e.RowIndex].Cells[2].Style.ForeColor = Color.Gray;
				}
				else
				{
					logicGrid.Rows[e.RowIndex].Cells[2].ReadOnly = false;
					logicGrid.Rows[e.RowIndex].Cells[2].Style = logicGrid.Columns[2].DefaultCellStyle;
				}
			}
			else if (e.ColumnIndex == 1)
			{
				if (e.FormattedValue.ToString() == SR.GetString("CF_Ext"))
				{
					logicGrid.Rows[e.RowIndex].Cells[2].ReadOnly = true;
					logicGrid.Rows[e.RowIndex].Cells[2].Style = new DataGridViewCellStyle();
					logicGrid.Rows[e.RowIndex].Cells[2].Style.ForeColor = Color.Gray;
					logicGrid.Rows[e.RowIndex].Cells[3].ReadOnly = true;
					logicGrid.Rows[e.RowIndex].Cells[3].Style = new DataGridViewCellStyle();
					logicGrid.Rows[e.RowIndex].Cells[3].Style.ForeColor = Color.Gray;
				}
				else
				{
					logicGrid.Rows[e.RowIndex].Cells[2].ReadOnly = false;
					logicGrid.Rows[e.RowIndex].Cells[2].Style = logicGrid.Columns[2].DefaultCellStyle;
					logicGrid.Rows[e.RowIndex].Cells[3].ReadOnly = false;
					logicGrid.Rows[e.RowIndex].Cells[3].Style = logicGrid.Columns[3].DefaultCellStyle;
				}
			}
		}

		private void logicGrid_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
		{
			if (!UpdateGridCellOnChanged(e.RowIndex, e.ColumnIndex))
			{
				e.Cancel = true;
			}
			else
			{
				e.Cancel = false;
			}
		}

		private void logicGrid_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
		{
			UpdateXPathExpression();
		}

		private void logicGrid_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
		{
			UpdateXPathExpression();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (!UpdateXPathExpression())
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_InvalidLogic"));
			}
			if (string.IsNullOrEmpty(filterName.Text))
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_Err1"));
			}
			else if (string.IsNullOrEmpty(xpathExpression.Text))
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_Err2"));
			}
			else if (!string.IsNullOrEmpty(xpathExpression.Text) && xpathExpression.Text.Length > 5120)
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_Err19"));
			}
			else if (!CustomFilter.ValidateXPath(xpathExpression.Text.Trim()))
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_Err3"));
			}
			else if (parent.IsDuplicateFilterName(filterName.Text, string.Empty))
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_Err4"));
			}
			else
			{
				try
				{
					Dictionary<string, string> dictionary = new Dictionary<string, string>();
					foreach (string key in nsMap.Keys)
					{
						dictionary.Add(nsMap[key], key);
					}
					List<CustomFilter.CustomFilterParameter> list = new List<CustomFilter.CustomFilterParameter>();
					int num = -1;
					foreach (DataGridViewRow item in (IEnumerable)logicGrid.Rows)
					{
						if (item.Cells[3].Value.ToString() != SR.GetString("CF_None"))
						{
							int num2 = int.Parse(item.Cells[3].Value.ToString().Substring(1, 1), CultureInfo.InvariantCulture);
							if (num2 > num)
							{
								num = num2;
							}
						}
					}
					if (num != -1)
					{
						for (int i = 0; i <= num; i++)
						{
							CustomFilter.CustomFilterParameter customFilterParameter = new CustomFilter.CustomFilterParameter();
							customFilterParameter.type = CustomFilterParameterValueType.AnyText;
							list.Add(customFilterParameter);
						}
					}
					currentFilter = new CustomFilter(filterName.Text, filterDescription.Text, xpathExpression.Text.Trim(), dictionary, list);
				}
				catch (AppSettingsException)
				{
					errorReport.ReportErrorToUser(SR.GetString("CF_Err5"));
					return;
				}
				base.DialogResult = DialogResult.OK;
				Close();
			}
		}

		private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			AppendFilterCriteria();
		}

		private void AppendFilterCriteria()
		{
			if (treeView.SelectedNode != null && (treeView.SelectedNode.Nodes == null || treeView.SelectedNode.Nodes.Count == 0 || !treeView.SelectedNode.IsExpanded) && treeView.SelectedNode != treeView.Nodes[0] && treeView.SelectedNode.Tag != null)
			{
				AppendTreeNode(treeView.SelectedNode);
			}
		}

		private void btnAddCriteria_Click(object sender, EventArgs e)
		{
			AppendFilterCriteria();
		}

		private void treeView_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyData == Keys.Return)
			{
				AppendFilterCriteria();
			}
		}

		private void logicGrid_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
		{
			logicGrid.SuspendLayout();
			int num = 0;
			foreach (DataGridViewColumn column in logicGrid.Columns)
			{
				num += column.Width;
			}
			if (num < sumOfColumnWidth)
			{
				dataGridViewComboBoxColumn3.Width += sumOfColumnWidth - num;
			}
			logicGrid.ResumeLayout();
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
			new System.ComponentModel.ComponentResourceManager(typeof(Microsoft.Tools.ServiceModel.TraceViewer.CreateCustomFilterByTemplateDialog));
			leftPanel = new System.Windows.Forms.Panel();
			treeView = new System.Windows.Forms.TreeView();
			toolStrip1 = new System.Windows.Forms.ToolStrip();
			btnAddCriteria = new System.Windows.Forms.ToolStripButton();
			splitter = new System.Windows.Forms.Splitter();
			rightPanel = new System.Windows.Forms.Panel();
			logicGrid = new System.Windows.Forms.DataGridView();
			dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			dataGridViewComboBoxColumn2 = new System.Windows.Forms.DataGridViewComboBoxColumn();
			dataGridViewCheckBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			dataGridViewComboBoxColumn3 = new System.Windows.Forms.DataGridViewComboBoxColumn();
			splitter1 = new System.Windows.Forms.Splitter();
			xpathPanel = new System.Windows.Forms.Panel();
			filterDescription = new System.Windows.Forms.TextBox();
			lblDescription = new System.Windows.Forms.Label();
			filterName = new System.Windows.Forms.TextBox();
			lblName = new System.Windows.Forms.Label();
			btnOk = new System.Windows.Forms.Button();
			btnCancel = new System.Windows.Forms.Button();
			lblXPath = new System.Windows.Forms.Label();
			xpathExpression = new System.Windows.Forms.TextBox();
			leftPanel.SuspendLayout();
			toolStrip1.SuspendLayout();
			rightPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)logicGrid).BeginInit();
			xpathPanel.SuspendLayout();
			SuspendLayout();
			leftPanel.Controls.Add(treeView);
			leftPanel.Controls.Add(toolStrip1);
			leftPanel.Dock = System.Windows.Forms.DockStyle.Left;
			leftPanel.Location = new System.Drawing.Point(0, 0);
			leftPanel.Name = "leftPanel";
			leftPanel.Size = new System.Drawing.Size(275, 541);
			leftPanel.TabStop = false;
			leftPanel.MinimumSize = new System.Drawing.Size(275, 0);
			leftPanel.MaximumSize = new System.Drawing.Size(350, 0);
			treeView.Dock = System.Windows.Forms.DockStyle.Fill;
			treeView.FullRowSelect = true;
			treeView.Location = new System.Drawing.Point(0, 25);
			treeView.Name = "treeView";
			treeView.HideSelection = false;
			treeView.ShowNodeToolTips = true;
			treeView.Size = new System.Drawing.Size(284, 516);
			treeView.TabIndex = 0;
			treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(treeView_AfterSelect);
			treeView.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(treeView_NodeMouseDoubleClick);
			treeView.KeyUp += new System.Windows.Forms.KeyEventHandler(treeView_KeyUp);
			treeView.TabIndex = 2;
			toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[1]
			{
				btnAddCriteria
			});
			toolStrip1.Location = new System.Drawing.Point(0, 0);
			toolStrip1.Name = "toolStrip1";
			toolStrip1.Size = new System.Drawing.Size(284, 25);
			toolStrip1.TabIndex = 1;
			btnAddCriteria.Enabled = false;
			btnAddCriteria.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			btnAddCriteria.Name = "btnAddCriteria";
			btnAddCriteria.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_AFC");
			btnAddCriteria.Click += new System.EventHandler(btnAddCriteria_Click);
			splitter.Location = new System.Drawing.Point(284, 0);
			splitter.Name = "splitter";
			splitter.Size = new System.Drawing.Size(3, 541);
			splitter.TabStop = false;
			splitter.Enabled = false;
			rightPanel.Controls.Add(logicGrid);
			rightPanel.Dock = System.Windows.Forms.DockStyle.Top;
			rightPanel.Location = new System.Drawing.Point(287, 0);
			rightPanel.Name = "rightPanel";
			rightPanel.Size = new System.Drawing.Size(435, 283);
			rightPanel.TabStop = false;
			logicGrid.AllowUserToAddRows = false;
			logicGrid.Columns.Add(dataGridViewTextBoxColumn1);
			logicGrid.Columns.Add(dataGridViewComboBoxColumn2);
			logicGrid.Columns.Add(dataGridViewCheckBoxColumn2);
			logicGrid.Columns.Add(dataGridViewComboBoxColumn3);
			logicGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			logicGrid.Location = new System.Drawing.Point(0, 0);
			logicGrid.MultiSelect = false;
			logicGrid.Name = "logicGrid";
			logicGrid.ShowEditingIcon = false;
			logicGrid.Size = new System.Drawing.Size(435, 283);
			logicGrid.TabIndex = 3;
			logicGrid.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(logicGrid_CellValidating);
			logicGrid.RowValidating += new System.Windows.Forms.DataGridViewCellCancelEventHandler(logicGrid_RowValidating);
			logicGrid.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(logicGrid_RowsAdded);
			logicGrid.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(logicGrid_RowsRemoved);
			logicGrid.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(logicGrid_ColumnWidthChanged);
			dataGridViewTextBoxColumn1.HeaderText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_ExpressionHeader");
			dataGridViewTextBoxColumn1.Name = "xpathHeader";
			dataGridViewTextBoxColumn1.ReadOnly = true;
			dataGridViewTextBoxColumn1.Width = 100;
			dataGridViewComboBoxColumn2.HeaderText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_OperationHeader");
			dataGridViewComboBoxColumn2.Name = "operationHeader";
			dataGridViewComboBoxColumn2.Width = 100;
			dataGridViewCheckBoxColumn2.HeaderText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_ValueHeader");
			dataGridViewCheckBoxColumn2.Name = "defaultValueHeader";
			dataGridViewCheckBoxColumn2.Width = 100;
			dataGridViewComboBoxColumn3.HeaderText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_ParameterHeader");
			dataGridViewComboBoxColumn3.Name = "paramHeader";
			dataGridViewComboBoxColumn3.Width = 100;
			splitter1.Dock = System.Windows.Forms.DockStyle.Top;
			splitter1.Location = new System.Drawing.Point(287, 283);
			splitter1.Name = "splitter1";
			splitter1.Size = new System.Drawing.Size(435, 3);
			splitter1.TabStop = false;
			xpathPanel.Controls.Add(filterDescription);
			xpathPanel.Controls.Add(lblDescription);
			xpathPanel.Controls.Add(filterName);
			xpathPanel.Controls.Add(lblName);
			xpathPanel.Controls.Add(btnOk);
			xpathPanel.Controls.Add(btnCancel);
			xpathPanel.Controls.Add(lblXPath);
			xpathPanel.Controls.Add(xpathExpression);
			xpathPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			xpathPanel.Location = new System.Drawing.Point(287, 286);
			xpathPanel.Name = "xpathPanel";
			xpathPanel.Size = new System.Drawing.Size(435, 255);
			xpathPanel.TabStop = false;
			filterDescription.Location = new System.Drawing.Point(127, 59);
			filterDescription.MaxLength = 512;
			filterDescription.Multiline = true;
			filterDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			filterDescription.Name = "filterDescription";
			filterDescription.Size = new System.Drawing.Size(306, 50);
			filterDescription.TabIndex = 7;
			lblDescription.AutoSize = true;
			lblDescription.Location = new System.Drawing.Point(8, 59);
			lblDescription.Name = "lblDescription";
			lblDescription.Size = new System.Drawing.Size(0, 0);
			lblDescription.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_FD");
			lblDescription.TabIndex = 6;
			filterName.Location = new System.Drawing.Point(127, 16);
			filterName.MaxLength = 255;
			filterName.Name = "filterName";
			filterName.Size = new System.Drawing.Size(306, 20);
			filterName.TabIndex = 5;
			lblName.AutoSize = true;
			lblName.Location = new System.Drawing.Point(8, 16);
			lblName.Name = "lblName";
			lblName.Size = new System.Drawing.Size(0, 0);
			lblName.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_FN");
			lblName.TabIndex = 4;
			btnOk.Location = new System.Drawing.Point(278, 225);
			btnOk.Name = "btnOk";
			btnOk.Size = new System.Drawing.Size(75, 23);
			btnOk.TabIndex = 0;
			btnOk.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Btn_OK");
			btnOk.Click += new System.EventHandler(btnOk_Click);
			btnCancel.CausesValidation = false;
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Location = new System.Drawing.Point(359, 225);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new System.Drawing.Size(75, 23);
			btnCancel.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Btn_Cancel");
			btnCancel.TabIndex = 1;
			lblXPath.AutoSize = true;
			lblXPath.Location = new System.Drawing.Point(8, 131);
			lblXPath.Name = "lblXPath";
			lblXPath.Size = new System.Drawing.Size(0, 0);
			lblXPath.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_XPathExpression2");
			lblXPath.TabStop = false;
			xpathExpression.ReadOnly = true;
			xpathExpression.Location = new System.Drawing.Point(127, 131);
			xpathExpression.Multiline = true;
			xpathExpression.MaxLength = 5120;
			xpathExpression.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			xpathExpression.Name = "xpathExpression";
			xpathExpression.Size = new System.Drawing.Size(306, 83);
			xpathExpression.TabStop = false;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.CancelButton = btnCancel;
			base.ClientSize = new System.Drawing.Size(722, 541);
			base.Controls.Add(xpathPanel);
			base.Controls.Add(splitter1);
			base.Controls.Add(rightPanel);
			base.Controls.Add(splitter);
			base.Controls.Add(leftPanel);
			Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_NewCustomFilterDlgName");
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "CreateCustomFilterByTemplateDialog";
			base.ShowInTaskbar = false;
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			leftPanel.ResumeLayout(performLayout: false);
			leftPanel.PerformLayout();
			toolStrip1.ResumeLayout(performLayout: false);
			rightPanel.ResumeLayout(performLayout: false);
			((System.ComponentModel.ISupportInitialize)logicGrid).EndInit();
			xpathPanel.ResumeLayout(performLayout: false);
			xpathPanel.PerformLayout();
			ResumeLayout(performLayout: false);
		}
	}
}
