using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class CustomFilterDialog : Form
	{
		private enum TabFocusIndex
		{
			OkButton,
			CancelButton,
			FilterList,
			NewButton,
			DeleteButton,
			ImportButton,
			ExportButton
		}

		private CustomFilterManager filterManager;

		private List<CustomFilter> resultFilters;

		private bool isCreatingFilterByTemplate;

		private string templateXmlString;

		private IErrorReport errorReport;

		private IUserInterfaceProvider userIP;

		private TraceViewerForm appForm;

		private IContainer components;

		private Label lblDescription;

		private Button btnCancel;

		private Button btnOk;

		private ListView listFilters;

		private ColumnHeader nameHeader;

		private ColumnHeader descriptionHeader;

		private ColumnHeader expressionHeader;

		private Button btnNew;

		private Button btnDelete;

		private Button btnImport;

		private Button btnExport;

		internal List<CustomFilter> ResultFilters => resultFilters;

		public CustomFilterDialog(IErrorReport errorReport)
		{
			InitializeComponent();
			this.errorReport = errorReport;
		}

		public bool IsDuplicateFilterName(string name, string originalName)
		{
			foreach (ListViewItem item in listFilters.Items)
			{
				if (!(((CustomFilter)item.Tag).FilterName == originalName) && ((CustomFilter)item.Tag).FilterName == name)
				{
					return true;
				}
			}
			return false;
		}

		public DialogResult CreateFilterByTemplate(string xml)
		{
			DialogResult result = DialogResult.Cancel;
			if (!string.IsNullOrEmpty(xml))
			{
				templateXmlString = xml;
				isCreatingFilterByTemplate = true;
				result = userIP.ShowDialog(this, appForm);
			}
			return result;
		}

		public void Initialize(CustomFilterManager filterManager, IUserInterfaceProvider userIP, TraceViewerForm appForm)
		{
			this.filterManager = filterManager;
			this.userIP = userIP;
			this.appForm = appForm;
		}

		private void CustomFilterDialog_Load(object sender, EventArgs e)
		{
			UpdateCustomFilterList();
		}

		private void UpdateCustomFilterList()
		{
			listFilters.Items.Clear();
			foreach (CustomFilter currentFilter in filterManager.CurrentFilters)
			{
				ListViewItem listViewItem = new ListViewItem(new string[3]
				{
					currentFilter.FilterName,
					(!string.IsNullOrEmpty(currentFilter.FilterDescription)) ? currentFilter.FilterDescription : string.Empty,
					currentFilter.Expression
				});
				listViewItem.Tag = currentFilter;
				listFilters.Items.Add(listViewItem);
			}
			UpdateButtonStatus();
		}

		private void listFilters_DoubleClick(object sender, EventArgs e)
		{
			if (listFilters.SelectedItems.Count != 0)
			{
				using (CustomFilterDefineDialog customFilterDefineDialog = new CustomFilterDefineDialog(this, errorReport))
				{
					customFilterDefineDialog.Initialize((CustomFilter)listFilters.SelectedItems[0].Tag);
					if (userIP.ShowDialog(customFilterDefineDialog, appForm) == DialogResult.OK)
					{
						listFilters.SelectedItems[0].Tag = customFilterDefineDialog.CurrentFilter;
						listFilters.SelectedItems[0].SubItems[0].Text = customFilterDefineDialog.CurrentFilter.FilterName;
						listFilters.SelectedItems[0].SubItems[1].Text = ((!string.IsNullOrEmpty(customFilterDefineDialog.CurrentFilter.FilterDescription)) ? customFilterDefineDialog.CurrentFilter.FilterDescription : string.Empty);
						listFilters.SelectedItems[0].SubItems[2].Text = customFilterDefineDialog.CurrentFilter.Expression;
					}
				}
			}
		}

		private void btnNew_Click(object sender, EventArgs e)
		{
			using (CustomFilterDefineDialog customFilterDefineDialog = new CustomFilterDefineDialog(this, errorReport))
			{
				if (userIP.ShowDialog(customFilterDefineDialog, appForm) == DialogResult.OK)
				{
					ListViewItem listViewItem = new ListViewItem(new string[3]
					{
						customFilterDefineDialog.CurrentFilter.FilterName,
						(!string.IsNullOrEmpty(customFilterDefineDialog.CurrentFilter.FilterDescription)) ? customFilterDefineDialog.CurrentFilter.FilterDescription : string.Empty,
						customFilterDefineDialog.CurrentFilter.Expression
					});
					listViewItem.Tag = customFilterDefineDialog.CurrentFilter;
					listFilters.Items.Add(listViewItem);
					UpdateButtonStatus();
				}
			}
		}

		private void CreateByTemplate(string xml)
		{
			using (CreateCustomFilterByTemplateDialog createCustomFilterByTemplateDialog = new CreateCustomFilterByTemplateDialog(this, errorReport))
			{
				try
				{
					createCustomFilterByTemplateDialog.Initialize(xml);
				}
				catch (AppSettingsException e)
				{
					ExceptionManager.ReportCommonErrorToUser(e);
				}
				if (userIP.ShowDialog(createCustomFilterByTemplateDialog, appForm) == DialogResult.OK)
				{
					ListViewItem listViewItem = new ListViewItem(new string[3]
					{
						createCustomFilterByTemplateDialog.CurrentFilter.FilterName,
						(!string.IsNullOrEmpty(createCustomFilterByTemplateDialog.CurrentFilter.FilterDescription)) ? createCustomFilterByTemplateDialog.CurrentFilter.FilterDescription : string.Empty,
						createCustomFilterByTemplateDialog.CurrentFilter.Expression
					});
					listViewItem.Tag = createCustomFilterByTemplateDialog.CurrentFilter;
					listFilters.Items.Add(listViewItem);
				}
			}
		}

		private void CustomFilterDialog_Shown(object sender, EventArgs e)
		{
			if (isCreatingFilterByTemplate && base.Visible && !string.IsNullOrEmpty(templateXmlString))
			{
				CreateByTemplate(templateXmlString);
			}
		}

		private void btnDelete_Click(object sender, EventArgs e)
		{
			if (listFilters.SelectedItems.Count != 0)
			{
				foreach (ListViewItem selectedItem in listFilters.SelectedItems)
				{
					listFilters.Items.Remove(selectedItem);
					filterManager.RemoveFilter((CustomFilter)selectedItem.Tag);
				}
				UpdateButtonStatus();
			}
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			List<CustomFilter> list = new List<CustomFilter>();
			foreach (ListViewItem item in listFilters.Items)
			{
				list.Add((CustomFilter)item.Tag);
			}
			filterManager.CurrentFilters = list;
			resultFilters = list;
			if (filterManager.Save())
			{
				base.DialogResult = DialogResult.OK;
				Close();
			}
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			base.DialogResult = DialogResult.Cancel;
			Close();
		}

		private void btnExport_Click(object sender, EventArgs e)
		{
			if (listFilters.SelectedItems.Count != 0)
			{
				List<CustomFilter> list = new List<CustomFilter>();
				foreach (ListViewItem selectedItem in listFilters.SelectedItems)
				{
					list.Add((CustomFilter)selectedItem.Tag);
				}
				filterManager.Export(list);
				UpdateButtonStatus();
			}
		}

		private void btnImport_Click(object sender, EventArgs e)
		{
			if (filterManager.Import())
			{
				UpdateCustomFilterList();
			}
		}

		private void listFilters_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateButtonStatus();
		}

		private void UpdateButtonStatus()
		{
			if (listFilters.SelectedItems.Count == 0)
			{
				btnExport.Enabled = false;
				btnDelete.Enabled = false;
			}
			else
			{
				btnExport.Enabled = true;
				btnDelete.Enabled = true;
			}
			listFilters.Select();
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
			lblDescription = new System.Windows.Forms.Label();
			btnImport = new System.Windows.Forms.Button();
			btnExport = new System.Windows.Forms.Button();
			btnDelete = new System.Windows.Forms.Button();
			btnNew = new System.Windows.Forms.Button();
			listFilters = new System.Windows.Forms.ListView();
			nameHeader = new System.Windows.Forms.ColumnHeader();
			descriptionHeader = new System.Windows.Forms.ColumnHeader();
			expressionHeader = new System.Windows.Forms.ColumnHeader();
			btnCancel = new System.Windows.Forms.Button();
			btnOk = new System.Windows.Forms.Button();
			SuspendLayout();
			lblDescription.Name = "lblDescription";
			lblDescription.Location = new System.Drawing.Point(10, 10);
			lblDescription.Size = new System.Drawing.Size(300, 20);
			lblDescription.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_DlgDescription");
			btnImport.Location = new System.Drawing.Point(380, 90);
			btnImport.Name = "btnImport";
			btnImport.Size = new System.Drawing.Size(100, 23);
			btnImport.TabIndex = 5;
			btnImport.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_Ibt");
			btnImport.Click += new System.EventHandler(btnImport_Click);
			btnExport.Location = new System.Drawing.Point(380, 120);
			btnExport.Name = "btnExport";
			btnExport.Size = new System.Drawing.Size(100, 23);
			btnExport.TabIndex = 6;
			btnExport.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_Ebt");
			btnExport.Click += new System.EventHandler(btnExport_Click);
			btnDelete.Location = new System.Drawing.Point(380, 60);
			btnDelete.Name = "btnDelete";
			btnDelete.Size = new System.Drawing.Size(100, 23);
			btnDelete.TabIndex = 4;
			btnDelete.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_Delete");
			btnDelete.Click += new System.EventHandler(btnDelete_Click);
			btnNew.Location = new System.Drawing.Point(380, 30);
			btnNew.Name = "btnNew";
			btnNew.Size = new System.Drawing.Size(100, 23);
			btnNew.TabIndex = 3;
			btnNew.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_New");
			btnNew.Click += new System.EventHandler(btnNew_Click);
			listFilters.Columns.AddRange(new System.Windows.Forms.ColumnHeader[3]
			{
				nameHeader,
				descriptionHeader,
				expressionHeader
			});
			listFilters.FullRowSelect = true;
			listFilters.Location = new System.Drawing.Point(10, 30);
			listFilters.Name = "listFilters";
			listFilters.ShowItemToolTips = true;
			listFilters.Size = new System.Drawing.Size(360, 265);
			listFilters.TabIndex = 2;
			listFilters.View = System.Windows.Forms.View.Details;
			listFilters.DoubleClick += new System.EventHandler(listFilters_DoubleClick);
			listFilters.SelectedIndexChanged += new System.EventHandler(listFilters_SelectedIndexChanged);
			nameHeader.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_NameH");
			nameHeader.Width = 90;
			descriptionHeader.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_DescriptionH");
			descriptionHeader.Width = 120;
			expressionHeader.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_XEH");
			expressionHeader.Width = 145;
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Location = new System.Drawing.Point(380, 308);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new System.Drawing.Size(75, 23);
			btnCancel.TabIndex = 1;
			btnCancel.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Btn_Cancel");
			btnCancel.Click += new System.EventHandler(btnCancel_Click);
			btnOk.Location = new System.Drawing.Point(295, 308);
			btnOk.Name = "btnOk";
			btnOk.Size = new System.Drawing.Size(75, 23);
			btnOk.TabIndex = 0;
			btnOk.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Btn_OK");
			btnOk.Click += new System.EventHandler(btnOk_Click);
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.CancelButton = btnCancel;
			base.AcceptButton = btnOk;
			base.ClientSize = new System.Drawing.Size(490, 337);
			base.Controls.Add(lblDescription);
			base.Controls.Add(btnOk);
			base.Controls.Add(btnCancel);
			base.Controls.Add(btnImport);
			base.Controls.Add(btnExport);
			base.Controls.Add(btnDelete);
			base.Controls.Add(btnNew);
			base.Controls.Add(listFilters);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "CustomFilterDialog";
			base.ShowInTaskbar = false;
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("CF_CFTitle");
			base.Load += new System.EventHandler(CustomFilterDialog_Load);
			base.Shown += new System.EventHandler(CustomFilterDialog_Shown);
		}
	}
}
