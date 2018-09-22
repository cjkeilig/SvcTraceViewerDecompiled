using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class DiagnosticsTraceDetailControl : UserControl
	{
		private enum TabFocusIndex
		{
			PrpoertyLabel,
			PropertyList,
			CallstackLabel,
			CallstackList
		}

		private IContainer components;

		private ListView listProperty;

		private Label lblCallstack;

		private ListView listCallstack;

		private ColumnHeader methodColumn;

		private ColumnHeader nameColumn;

		private ColumnHeader valueColumn;

		private Label lblPrpoerty;

		public DiagnosticsTraceDetailControl()
		{
			InitializeComponent();
		}

		public void CleanUp()
		{
			listProperty.Items.Clear();
			listCallstack.Items.Clear();
		}

		private void EnlistElements(XmlNode node, int depth)
		{
			if (depth < 10 && node != null)
			{
				if (string.Compare(node.Name, "Callstack", true, CultureInfo.CurrentUICulture) == 0 && !string.IsNullOrEmpty(node.InnerText))
				{
					DisplayCallstack(node.InnerText);
				}
				else
				{
					if (node.Attributes != null)
					{
						foreach (XmlAttribute attribute in node.Attributes)
						{
							if (!string.IsNullOrEmpty(attribute.Name) && !TraceDetailedProcessParameter.ExcludedXmlAttributes.Contains(attribute.Name))
							{
								listProperty.Items.Add(new ListViewItem(new string[2]
								{
									attribute.Name,
									attribute.Value
								}));
							}
						}
					}
					if (node.HasChildNodes)
					{
						if (node.ChildNodes.Count == 1 && string.Compare(node.ChildNodes[0].Name, "#text",  true, CultureInfo.CurrentUICulture) == 0 && !string.IsNullOrEmpty(node.ChildNodes[0].InnerText))
						{
							listProperty.Items.Add(new ListViewItem(new string[2]
							{
								node.Name,
								node.InnerText
							}));
						}
						else
						{
							foreach (XmlNode childNode in node.ChildNodes)
							{
								EnlistElements(childNode, depth + 1);
							}
						}
					}
					else if (!string.IsNullOrEmpty(node.Value))
					{
						listProperty.Items.Add(new ListViewItem(new string[2]
						{
							node.Name,
							node.Value
						}));
					}
				}
			}
		}

		private void DisplayCallstack(string callstack)
		{
			listCallstack.Items.Clear();
			if (!string.IsNullOrEmpty(callstack))
			{
				callstack = callstack.Trim();
				if (callstack.StartsWith("at ", StringComparison.OrdinalIgnoreCase))
				{
					callstack = callstack.Substring("at ".Length);
				}
				string[] array = callstack.Split(new string[1]
				{
					" at "
				}, StringSplitOptions.RemoveEmptyEntries);
				if (array != null)
				{
					string[] array2 = array;
					for (int i = 0; i < array2.Length; i++)
					{
						string text = array2[i].Trim();
						if (!string.IsNullOrEmpty(text))
						{
							ListViewItem listViewItem = new ListViewItem(new string[1]
							{
								text
							});
							if (!text.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
							{
								ListViewItem listViewItem2 = listViewItem;
								listViewItem2.Font = new Font(listViewItem2.Font, FontStyle.Bold);
							}
							listCallstack.Items.Add(listViewItem);
						}
					}
				}
			}
		}

		private void DisplayDiagnosticsTree(string diagnosticsXml)
		{
			if (!string.IsNullOrEmpty(diagnosticsXml))
			{
				CleanUp();
				try
				{
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.LoadXml(diagnosticsXml);
					XmlElement documentElement = xmlDocument.DocumentElement;
					if (documentElement != null && string.Compare(documentElement.Name, "System.Diagnostics", true, CultureInfo.CurrentUICulture) == 0)
					{
						foreach (XmlElement item in documentElement)
						{
							EnlistElements(item, 0);
						}
					}
				}
				catch (XmlException e)
				{
					throw new TraceViewerException(SR.GetString("FV_ERROR"), e);
				}
			}
		}

		public void ReloadExceptions(string diagXml)
		{
			CleanUp();
			if (!string.IsNullOrEmpty(diagXml))
			{
				DisplayDiagnosticsTree(diagXml);
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
			listProperty = new System.Windows.Forms.ListView();
			nameColumn = new System.Windows.Forms.ColumnHeader();
			valueColumn = new System.Windows.Forms.ColumnHeader();
			lblCallstack = new System.Windows.Forms.Label();
			listCallstack = new System.Windows.Forms.ListView();
			methodColumn = new System.Windows.Forms.ColumnHeader();
			lblPrpoerty = new System.Windows.Forms.Label();
			SuspendLayout();
			listProperty.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			listProperty.Columns.AddRange(new System.Windows.Forms.ColumnHeader[2]
			{
				nameColumn,
				valueColumn
			});
			listProperty.FullRowSelect = true;
			listProperty.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			listProperty.HideSelection = false;
			listProperty.Location = new System.Drawing.Point(5, 20);
			listProperty.Name = "listView";
			listProperty.ShowItemToolTips = true;
			listProperty.Size = new System.Drawing.Size(420, 65);
			listProperty.TabIndex = 1;
			listProperty.UseCompatibleStateImageBehavior = false;
			listProperty.View = System.Windows.Forms.View.Details;
			nameColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_List_NameCol");
			nameColumn.Width = 118;
			valueColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_List_ValueCol");
			valueColumn.Width = 192;
			lblCallstack.Location = new System.Drawing.Point(5, 95);
			lblCallstack.Name = "lblCallstack";
			lblCallstack.Size = new System.Drawing.Size(130, 20);
			lblCallstack.TabIndex = 2;
			lblCallstack.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_Diag_Callstack");
			listCallstack.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			listCallstack.Columns.AddRange(new System.Windows.Forms.ColumnHeader[1]
			{
				methodColumn
			});
			listCallstack.FullRowSelect = true;
			listCallstack.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			listCallstack.HideSelection = false;
			listCallstack.Location = new System.Drawing.Point(5, 115);
			listCallstack.Name = "listCallstack";
			listCallstack.ShowItemToolTips = true;
			listCallstack.Size = new System.Drawing.Size(420, 125);
			listCallstack.TabIndex = 3;
			listCallstack.UseCompatibleStateImageBehavior = false;
			listCallstack.View = System.Windows.Forms.View.Details;
			methodColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_Diag_Method");
			methodColumn.Width = 314;
			lblPrpoerty.Location = new System.Drawing.Point(5, 0);
			lblPrpoerty.Name = "lblPrpoerty";
			lblPrpoerty.Size = new System.Drawing.Size(100, 20);
			lblPrpoerty.TabIndex = 2;
			lblPrpoerty.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_Diag_Properties");
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			BackColor = System.Drawing.SystemColors.Window;
			base.Controls.Add(lblPrpoerty);
			base.Controls.Add(listCallstack);
			base.Controls.Add(lblCallstack);
			base.Controls.Add(listProperty);
			base.Name = "DiagnosticsTraceDetailControl";
			base.Size = new System.Drawing.Size(430, 245);
			ResumeLayout(performLayout: false);
		}
	}
}
