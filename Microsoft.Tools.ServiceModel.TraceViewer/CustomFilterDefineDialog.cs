using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class CustomFilterDefineDialog : Form
	{
		private enum TabFocusIndex
		{
			OkButton,
			CancelButton,
			FilterNameLabel,
			FilterNameText,
			FilterDescriptionLabel,
			FilterDescriptionText,
			FilterExpressionLabel,
			FilterExpressionText,
			ParametersLabel,
			ParametersList,
			NamespaceGroup,
			NamespaceList,
			AddNamespaceButton,
			RemoveNamespaceButton
		}

		private CustomFilter currentFilter;

		private CustomFilterDialog parent;

		private IErrorReport errorReport;

		private readonly int sumOfcolumnWidth;

		private IContainer components;

		private Label lblParams;

		private Label lblName;

		private Label lblDescription;

		private Label lblExpress;

		private GroupBox nsGroup;

		private ListView listNamespace;

		private TextBox filterName;

		private TextBox filterDescription;

		private TextBox filterExpression;

		private Button btnAddNs;

		private Button btnRemoveNs;

		private Button btnCancel;

		private Button btnOk;

		private ColumnHeader prefixHeader;

		private ColumnHeader valueHeader;

		private DataGridView paramGrid;

		private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;

		private DataGridViewComboBoxColumn dataGridViewComboBoxColumn1;

		private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;

		internal CustomFilter CurrentFilter => currentFilter;

		private string FilterName => filterName.Text.Trim();

		private string FilterDescription => filterDescription.Text;

		private string XPathExpression => filterExpression.Text.Trim();

		private Dictionary<string, string> Namespaces
		{
			get
			{
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				foreach (ListViewItem item in listNamespace.Items)
				{
					dictionary.Add(item.SubItems[0].Text, item.SubItems[1].Text);
				}
				return dictionary;
			}
		}

		private List<CustomFilter.CustomFilterParameter> Parameters
		{
			get
			{
				List<CustomFilter.CustomFilterParameter> list = new List<CustomFilter.CustomFilterParameter>();
				foreach (DataGridViewRow item in (IEnumerable)paramGrid.Rows)
				{
					CustomFilter.CustomFilterParameter customFilterParameter = new CustomFilter.CustomFilterParameter();
					customFilterParameter.type = ConvertToCustomFilterParameterValueType(((DataGridViewComboBoxCell)item.Cells[1]).Value.ToString());
					customFilterParameter.description = item.Cells[2].Value.ToString();
					list.Add(customFilterParameter);
				}
				return list;
			}
		}

		private CustomFilterParameterValueType ConvertToCustomFilterParameterValueType(string s)
		{
			if (s == SR.GetString("CF_Type1"))
			{
				return CustomFilterParameterValueType.AnyText;
			}
			if (s == SR.GetString("CF_Type2"))
			{
				return CustomFilterParameterValueType.Numeric;
			}
			if (s == SR.GetString("CF_Type3"))
			{
				return CustomFilterParameterValueType.DateTime;
			}
			return CustomFilterParameterValueType.AnyText;
		}

		private string ConvertToCustomFilterParameterValueType(CustomFilterParameterValueType type)
		{
			switch (type)
			{
			case CustomFilterParameterValueType.AnyText:
				return SR.GetString("CF_Type1");
			case CustomFilterParameterValueType.Numeric:
				return SR.GetString("CF_Type2");
			case CustomFilterParameterValueType.DateTime:
				return SR.GetString("CF_Type3");
			default:
				return SR.GetString("CF_Type1");
			}
		}

		public CustomFilterDefineDialog(CustomFilterDialog parent, IErrorReport errorReport)
		{
			InitializeComponent();
			this.parent = parent;
			this.errorReport = errorReport;
			sumOfcolumnWidth = 0;
			foreach (DataGridViewColumn column in paramGrid.Columns)
			{
				sumOfcolumnWidth += column.Width;
			}
		}

		public void Initialize(CustomFilter filter)
		{
			currentFilter = filter;
			filterName.Text = filter.FilterName;
			filterDescription.Text = filter.FilterDescription;
			filterExpression.Text = filter.Expression;
			foreach (string key in filter.Namespaces.Keys)
			{
				ListViewItem value = new ListViewItem(new string[2]
				{
					key,
					filter.Namespaces[key]
				});
				listNamespace.Items.Add(value);
			}
			UpdateParameterList(filter.Parameters);
		}

		private void UpdateParameterList()
		{
			int num = CustomFilter.ExtractXPathParameters(XPathExpression);
			if (num != 0)
			{
				for (int i = 0; i < num; i++)
				{
					DataGridViewRow dataGridViewRow = new DataGridViewRow();
					dataGridViewRow.CreateCells(paramGrid);
					dataGridViewRow.Cells[0].Value = SR.GetString("CF_LeftB") + i.ToString(CultureInfo.InvariantCulture) + SR.GetString("CF_RightB");
					dataGridViewRow.Cells[1].Value = SR.GetString("CF_Type1");
					dataGridViewRow.Cells[2].Value = string.Empty;
					paramGrid.Rows.Add(dataGridViewRow);
				}
			}
			UpdateNamespaceGroupStatus();
		}

		private void UpdateParameterList(List<CustomFilter.CustomFilterParameter> typeList)
		{
			if (typeList != null && typeList.Count != 0)
			{
				int num = 0;
				foreach (CustomFilter.CustomFilterParameter type in typeList)
				{
					DataGridViewRow dataGridViewRow = new DataGridViewRow();
					dataGridViewRow.CreateCells(paramGrid);
					dataGridViewRow.Cells[0].Value = SR.GetString("CF_LeftB") + num.ToString(CultureInfo.InvariantCulture) + SR.GetString("CF_RightB");
					dataGridViewRow.Cells[1].Value = ConvertToCustomFilterParameterValueType(type.type);
					dataGridViewRow.Cells[2].Value = ((!string.IsNullOrEmpty(type.description)) ? type.description : string.Empty);
					paramGrid.Rows.Add(dataGridViewRow);
					num++;
				}
			}
		}

		public bool IsDuplicatePrefix(string prefix, string originalPrefix)
		{
			foreach (ListViewItem item in listNamespace.Items)
			{
				if (!(originalPrefix == item.SubItems[0].Text) && item.SubItems[0].Text == prefix)
				{
					return true;
				}
			}
			return false;
		}

		private void listNamespace_DoubleClick(object sender, EventArgs e)
		{
			if (listNamespace.SelectedItems.Count != 0)
			{
				using (NSEditDlg nSEditDlg = new NSEditDlg(errorReport))
				{
					nSEditDlg.Initialize(listNamespace.SelectedItems[0].SubItems[0].Text, listNamespace.SelectedItems[0].SubItems[1].Text, this);
					if (nSEditDlg.ShowDialog(this) == DialogResult.OK)
					{
						listNamespace.SelectedItems[0].SubItems[0].Text = nSEditDlg.Prefix;
						listNamespace.SelectedItems[0].SubItems[1].Text = nSEditDlg.Namespace;
					}
				}
			}
		}

		private void btnAddNs_Click(object sender, EventArgs e)
		{
			using (NSEditDlg nSEditDlg = new NSEditDlg(errorReport))
			{
				nSEditDlg.Initialize(this);
				if (nSEditDlg.ShowDialog(this) == DialogResult.OK)
				{
					ListViewItem value = new ListViewItem(new string[2]
					{
						nSEditDlg.Prefix,
						nSEditDlg.Namespace
					});
					listNamespace.Items.Add(value);
				}
			}
			UpdateNamespaceGroupStatus();
		}

		private void btnRemoveNs_Click(object sender, EventArgs e)
		{
			if (listNamespace.SelectedItems.Count != 0)
			{
				listNamespace.Items.Remove(listNamespace.SelectedItems[0]);
				UpdateNamespaceGroupStatus();
			}
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(FilterName))
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_Err1"));
			}
			else if (string.IsNullOrEmpty(XPathExpression))
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_Err2"));
			}
			else if (!string.IsNullOrEmpty(XPathExpression) && XPathExpression.Length > 5120)
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_Err19"));
			}
			else if (!CustomFilter.ValidateXPath(XPathExpression))
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_Err3"));
			}
			else if (parent.IsDuplicateFilterName(FilterName, (currentFilter == null) ? string.Empty : currentFilter.FilterName))
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_Err4"));
			}
			else
			{
				try
				{
					currentFilter = new CustomFilter(FilterName, FilterDescription, XPathExpression, Namespaces, Parameters);
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

		private void btnCancel_Click(object sender, EventArgs e)
		{
			base.DialogResult = DialogResult.Cancel;
			Close();
		}

		private void filterExpression_Validating(object sender, CancelEventArgs e)
		{
			paramGrid.Rows.Clear();
			if (!string.IsNullOrEmpty(XPathExpression))
			{
				if (!CustomFilter.ValidateXPath(XPathExpression))
				{
					errorReport.ReportErrorToUser(SR.GetString("CF_Err3"));
					e.Cancel = true;
				}
				else
				{
					UpdateParameterList();
				}
			}
		}

		private void CustomFilterDefineDialog_Load(object sender, EventArgs e)
		{
			string[] dataSource = new string[3]
			{
				SR.GetString("CF_Type1"),
				SR.GetString("CF_Type2"),
				SR.GetString("CF_Type3")
			};
			((DataGridViewComboBoxColumn)paramGrid.Columns[1]).DataSource = dataSource;
			UpdateNamespaceGroupStatus();
		}

		private void listNamespace_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateNamespaceGroupStatus();
		}

		private void UpdateNamespaceGroupStatus()
		{
			if (listNamespace.SelectedItems.Count != 0)
			{
				btnRemoveNs.Enabled = true;
			}
			else
			{
				btnRemoveNs.Enabled = false;
			}
			listNamespace.Select();
		}

		private void paramGrid_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
		{
			paramGrid.SuspendLayout();
			int num = 0;
			foreach (DataGridViewColumn column in paramGrid.Columns)
			{
				num += column.Width;
			}
			if (num < sumOfcolumnWidth)
			{
				dataGridViewTextBoxColumn2.Width += sumOfcolumnWidth - num;
			}
			paramGrid.ResumeLayout();
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
			lblName = new System.Windows.Forms.Label();
			lblDescription = new System.Windows.Forms.Label();
			lblExpress = new System.Windows.Forms.Label();
			lblParams = new System.Windows.Forms.Label();
			nsGroup = new System.Windows.Forms.GroupBox();
			btnAddNs = new System.Windows.Forms.Button();
			btnRemoveNs = new System.Windows.Forms.Button();
			listNamespace = new System.Windows.Forms.ListView();
			prefixHeader = new System.Windows.Forms.ColumnHeader();
			valueHeader = new System.Windows.Forms.ColumnHeader();
			filterName = new System.Windows.Forms.TextBox();
			filterDescription = new System.Windows.Forms.TextBox();
			filterExpression = new System.Windows.Forms.TextBox();
			btnCancel = new System.Windows.Forms.Button();
			btnOk = new System.Windows.Forms.Button();
			paramGrid = new System.Windows.Forms.DataGridView();
			dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			dataGridViewComboBoxColumn1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
			dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			nsGroup.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)paramGrid).BeginInit();
			SuspendLayout();
			lblName.AutoSize = true;
			lblName.Location = new System.Drawing.Point(12, 18);
			lblName.Name = "lblName";
			lblName.Size = new System.Drawing.Size(90, 16);
			lblName.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_FN");
			lblName.TabIndex = 2;
			lblDescription.AutoSize = true;
			lblDescription.Location = new System.Drawing.Point(12, 45);
			lblDescription.Name = "lblDescription";
			lblDescription.Size = new System.Drawing.Size(90, 16);
			lblDescription.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_FD");
			lblDescription.TabIndex = 4;
			lblExpress.AutoSize = true;
			lblExpress.Location = new System.Drawing.Point(12, 110);
			lblExpress.Name = "lblExpress";
			lblExpress.Size = new System.Drawing.Size(90, 16);
			lblExpress.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_XPathExpression");
			lblExpress.TabIndex = 6;
			lblParams.Name = "lblParams";
			lblParams.Location = new System.Drawing.Point(12, 175);
			lblParams.Size = new System.Drawing.Size(90, 16);
			lblParams.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_ParametersGP");
			lblParams.TabIndex = 8;
			nsGroup.Controls.Add(btnAddNs);
			nsGroup.Controls.Add(btnRemoveNs);
			nsGroup.Controls.Add(listNamespace);
			nsGroup.Location = new System.Drawing.Point(12, 293);
			nsGroup.Name = "nsGroup";
			nsGroup.Size = new System.Drawing.Size(352, 179);
			nsGroup.TabIndex = 10;
			nsGroup.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_Namespaces");
			btnAddNs.Location = new System.Drawing.Point(190, 150);
			btnAddNs.Name = "btnAddNs";
			btnAddNs.Size = new System.Drawing.Size(75, 23);
			btnAddNs.TabIndex = 12;
			btnAddNs.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_Add");
			btnAddNs.Click += new System.EventHandler(btnAddNs_Click);
			btnRemoveNs.Location = new System.Drawing.Point(271, 150);
			btnRemoveNs.Name = "btnRemoveNs";
			btnRemoveNs.Size = new System.Drawing.Size(75, 23);
			btnRemoveNs.TabIndex = 13;
			btnRemoveNs.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_Remove");
			btnRemoveNs.Click += new System.EventHandler(btnRemoveNs_Click);
			listNamespace.Columns.AddRange(new System.Windows.Forms.ColumnHeader[2]
			{
				prefixHeader,
				valueHeader
			});
			listNamespace.FullRowSelect = true;
			listNamespace.Location = new System.Drawing.Point(6, 19);
			listNamespace.MultiSelect = false;
			listNamespace.Name = "listNamespace";
			listNamespace.HideSelection = false;
			listNamespace.ShowItemToolTips = true;
			listNamespace.Size = new System.Drawing.Size(340, 126);
			listNamespace.TabIndex = 11;
			listNamespace.View = System.Windows.Forms.View.Details;
			listNamespace.DoubleClick += new System.EventHandler(listNamespace_DoubleClick);
			listNamespace.SelectedIndexChanged += new System.EventHandler(listNamespace_SelectedIndexChanged);
			prefixHeader.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_PrefixHeader");
			valueHeader.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_NamespaceHeader");
			valueHeader.Width = 275;
			filterName.Location = new System.Drawing.Point(120, 18);
			filterName.MaxLength = 255;
			filterName.Name = "filterName";
			filterName.Size = new System.Drawing.Size(240, 20);
			filterName.TabIndex = 3;
			filterDescription.Location = new System.Drawing.Point(120, 45);
			filterDescription.MaxLength = 512;
			filterDescription.Multiline = true;
			filterDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			filterDescription.Name = "filterDescription";
			filterDescription.Size = new System.Drawing.Size(240, 58);
			filterDescription.TabIndex = 5;
			filterExpression.Location = new System.Drawing.Point(120, 110);
			filterExpression.MaxLength = 5120;
			filterExpression.Multiline = true;
			filterExpression.Name = "filterExpression";
			filterExpression.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			filterExpression.Size = new System.Drawing.Size(240, 58);
			filterExpression.TabIndex = 7;
			filterExpression.Validating += new System.ComponentModel.CancelEventHandler(filterExpression_Validating);
			btnCancel.CausesValidation = false;
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Location = new System.Drawing.Point(283, 482);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new System.Drawing.Size(75, 23);
			btnCancel.TabIndex = 1;
			btnCancel.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Btn_Cancel");
			btnCancel.Click += new System.EventHandler(btnCancel_Click);
			btnOk.Location = new System.Drawing.Point(202, 482);
			btnOk.Name = "btnOk";
			btnOk.Size = new System.Drawing.Size(75, 23);
			btnOk.TabIndex = 0;
			btnOk.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Btn_OK");
			btnOk.Click += new System.EventHandler(btnOk_Click);
			paramGrid.AllowUserToAddRows = false;
			paramGrid.AllowUserToDeleteRows = false;
			paramGrid.Columns.Add(dataGridViewTextBoxColumn1);
			paramGrid.Columns.Add(dataGridViewComboBoxColumn1);
			paramGrid.Columns.Add(dataGridViewTextBoxColumn2);
			paramGrid.Location = new System.Drawing.Point(120, 175);
			paramGrid.MultiSelect = false;
			paramGrid.Name = "paramGrid";
			paramGrid.RowHeadersVisible = false;
			paramGrid.ShowCellErrors = false;
			paramGrid.ShowEditingIcon = false;
			paramGrid.ShowRowErrors = false;
			paramGrid.Size = new System.Drawing.Size(240, 112);
			paramGrid.TabIndex = 9;
			paramGrid.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(paramGrid_ColumnWidthChanged);
			dataGridViewTextBoxColumn1.HeaderText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_ParameterHeader");
			dataGridViewTextBoxColumn1.Name = "Parameter";
			dataGridViewTextBoxColumn1.ReadOnly = true;
			dataGridViewTextBoxColumn1.Width = 50;
			dataGridViewComboBoxColumn1.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
			dataGridViewComboBoxColumn1.HeaderText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_PTHeader");
			dataGridViewComboBoxColumn1.MaxDropDownItems = 3;
			dataGridViewComboBoxColumn1.Name = "Type";
			dataGridViewComboBoxColumn1.Width = 95;
			dataGridViewTextBoxColumn2.HeaderText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_DescriptionHeader");
			dataGridViewTextBoxColumn2.Name = "paramDespHeader";
			dataGridViewTextBoxColumn2.Width = 90;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.CancelButton = btnCancel;
			base.AcceptButton = btnOk;
			base.ClientSize = new System.Drawing.Size(378, 511);
			base.Controls.Add(btnOk);
			base.Controls.Add(btnCancel);
			base.Controls.Add(filterExpression);
			base.Controls.Add(filterDescription);
			base.Controls.Add(filterName);
			base.Controls.Add(nsGroup);
			base.Controls.Add(lblExpress);
			base.Controls.Add(lblDescription);
			base.Controls.Add(lblName);
			base.Controls.Add(paramGrid);
			base.Controls.Add(lblParams);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "CustomFilterDefineDialog";
			base.ShowInTaskbar = false;
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_ACFTitle");
			base.Load += new System.EventHandler(CustomFilterDefineDialog_Load);
			nsGroup.ResumeLayout(performLayout: false);
			((System.ComponentModel.ISupportInitialize)paramGrid).EndInit();
			ResumeLayout(performLayout: false);
			PerformLayout();
		}
	}
}
