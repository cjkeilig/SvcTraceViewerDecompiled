using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class MessageHeaderTraceInfoControl : UserControl
	{
		private class InternalMessageHeaders
		{
			public string messageId;

			public string relatesTo;

			public string activityId;

			public string action;

			public string from;

			public string to;

			public string replyTo;
		}

		private enum TabFocusIndex
		{
			PropertyLabel,
			PropertyList,
			HeaderGroup,
			ActionLabel,
			ActionList,
			MessageIDLabel,
			MessageID,
			ActivityIDLabel,
			ActivityID,
			FromLabel,
			From,
			ToLabel,
			To,
			ReplyToLabel,
			ReplyTo,
			HeaderTreeLabel,
			HeaderTree
		}

		private static Dictionary<string, string> generalPropertyNodeValuePair;

		private const string THIS_NODETAG = "this";

		private IContainer components;

		private Label lblMessageProperties;

		private ListView listProperties;

		private ColumnHeader nameColumn;

		private ColumnHeader valueColumn;

		private GroupBox headerGroup;

		private Label lblHeaderTree;

		private TreeView headersTree;

		private Label lblMessageID;

		private TextBox txtAction;

		private Label lblAction;

		private TextBox txtMessageID;

		private TextBox txtActivityID;

		private Label lblActivityID;

		private TextBox txtReplyTo;

		private Label lblReplyTo;

		private TextBox txtTo;

		private Label lblTo;

		private TextBox txtFrom;

		private Label lblFrom;

		static MessageHeaderTraceInfoControl()
		{
			generalPropertyNodeValuePair = new Dictionary<string, string>();
			generalPropertyNodeValuePair.Add("MessageID", "this");
			generalPropertyNodeValuePair.Add("RelatesTo", "this");
			generalPropertyNodeValuePair.Add("ActivityId", "this");
			generalPropertyNodeValuePair.Add("Action", "this");
			generalPropertyNodeValuePair.Add("ReplyTo", "Address");
			generalPropertyNodeValuePair.Add("To", "this");
			generalPropertyNodeValuePair.Add("From", "Address");
		}

		public MessageHeaderTraceInfoControl()
		{
			InitializeComponent();
		}

		public void CleanUp()
		{
			listProperties.Items.Clear();
			txtAction.Clear();
			txtMessageID.Clear();
			txtActivityID.Clear();
			txtFrom.Clear();
			txtTo.Clear();
			txtReplyTo.Clear();
			headersTree.Nodes.Clear();
		}

		private void AssignHeaderInfo(InternalMessageHeaders headers, string headerName, string value)
		{
			if (headers != null && !string.IsNullOrEmpty(headerName) && !string.IsNullOrEmpty(value))
			{
				switch (headerName)
				{
				case "MessageID":
					headers.messageId = value;
					break;
				case "RelatesTo":
					headers.relatesTo = value;
					break;
				case "ActivityId":
					headers.activityId = TraceRecord.NormalizeActivityId(value);
					break;
				case "Action":
					headers.action = value;
					break;
				case "ReplyTo":
					headers.replyTo = value;
					break;
				case "To":
					headers.to = value;
					break;
				case "From":
					headers.from = value;
					break;
				}
			}
		}

		private void EnlistMessageHeadersTree(XmlNode xmlNode, TreeNode treeNode, InternalMessageHeaders headers, int depth)
		{
			if (depth < 10 && xmlNode != null && treeNode != null && !TraceDetailedProcessParameter.ExcludedXmlNodes.Contains(Utilities.TradeOffXmlPrefixForName(xmlNode.Name)))
			{
				if (xmlNode.Attributes != null)
				{
					foreach (XmlAttribute attribute in xmlNode.Attributes)
					{
						if (!TraceDetailedProcessParameter.ExcludedXmlAttributes.Contains(Utilities.TradeOffXmlPrefixForName(attribute.Name)))
						{
							treeNode.Nodes.Add(SR.GetString("FV_PROPERTY_HEADER") + attribute.Name + SR.GetString("FV_EQUAL") + attribute.Value);
						}
					}
				}
				string text = Utilities.TradeOffXmlPrefixForName(xmlNode.Name);
				bool flag = false;
				bool flag2 = false;
				string b = null;
				if (generalPropertyNodeValuePair.ContainsKey(text))
				{
					flag = true;
					if (generalPropertyNodeValuePair[text] == "this")
					{
						flag2 = true;
					}
					else
					{
						b = generalPropertyNodeValuePair[text];
					}
				}
				if (xmlNode.HasChildNodes)
				{
					foreach (XmlNode childNode in xmlNode.ChildNodes)
					{
						if (string.Compare(childNode.Name, "#text",  true, CultureInfo.CurrentUICulture) == 0 && !string.IsNullOrEmpty(childNode.Value))
						{
							if (string.Compare(Utilities.TradeOffXmlPrefixForName(xmlNode.Name), "ActivityId",  true, CultureInfo.CurrentUICulture) == 0)
							{
								string text2 = TraceRecord.NormalizeActivityId(xmlNode.ChildNodes[0].Value);
								if (!string.IsNullOrEmpty(text2) && TraceViewerForm.IsActivityDisplayNameInCache(text2))
								{
									treeNode.Text = SR.GetString("FV_MSG2_ActivityName2") + SR.GetString("FV_EQUAL") + TraceViewerForm.GetActivityDisplayName(text2);
								}
								else
								{
									treeNode.Text = "ActivityId" + SR.GetString("FV_EQUAL") + text2;
								}
							}
							else
							{
								treeNode.Text = xmlNode.Name + SR.GetString("FV_EQUAL") + xmlNode.ChildNodes[0].Value;
							}
							if (flag && flag2)
							{
								AssignHeaderInfo(headers, text, childNode.Value);
							}
						}
						else
						{
							TreeNode treeNode2 = new TreeNode(childNode.Name);
							treeNode.Nodes.Add(treeNode2);
							EnlistMessageHeadersTree(childNode, treeNode2, headers, depth + 1);
							if (flag && Utilities.TradeOffXmlPrefixForName(childNode.Name) == b && childNode.HasChildNodes)
							{
								foreach (XmlNode childNode2 in childNode.ChildNodes)
								{
									if (string.Compare(childNode2.Name, "#text",  true, CultureInfo.CurrentUICulture) == 0 && !string.IsNullOrEmpty(childNode2.Value))
									{
										AssignHeaderInfo(headers, text, childNode2.Value);
									}
								}
							}
						}
					}
				}
				else if (!string.IsNullOrEmpty(xmlNode.Value) && !TraceDetailedProcessParameter.ExcludedXmlNodes.Contains(Utilities.TradeOffXmlPrefixForName(xmlNode.Name)))
				{
					if (string.Compare(Utilities.TradeOffXmlPrefixForName(xmlNode.Name), "ActivityId",  true, CultureInfo.CurrentUICulture) == 0)
					{
						string text3 = TraceRecord.NormalizeActivityId(xmlNode.ChildNodes[0].Value);
						if (!string.IsNullOrEmpty(text3) && TraceViewerForm.IsActivityDisplayNameInCache(text3))
						{
							treeNode.Text = SR.GetString("FV_MSG2_ActivityName2") + SR.GetString("FV_EQUAL") + TraceViewerForm.GetActivityDisplayName(text3);
						}
						else
						{
							treeNode.Text = "ActivityId" + SR.GetString("FV_EQUAL") + text3;
						}
					}
					else
					{
						treeNode.Text = xmlNode.Name + SR.GetString("FV_EQUAL") + xmlNode.ChildNodes[0].Value;
					}
				}
			}
		}

		public void ReloadMessageRelatedInfo(string messagePropertiesInfoXml, string messageHeadersInfoXml)
		{
			CleanUp();
			try
			{
				ReloadMessagePropertiesInfo(messagePropertiesInfoXml);
				ReloadMessageHeadersInfo(messageHeadersInfoXml);
			}
			catch (XmlException e)
			{
				throw new TraceViewerException(SR.GetString("FV_ERROR"), e);
			}
		}

		private void ReloadMessagePropertiesInfo(string messagePropertiesInfoXml)
		{
			if (!string.IsNullOrEmpty(messagePropertiesInfoXml))
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(messagePropertiesInfoXml);
				XmlElement documentElement = xmlDocument.DocumentElement;
				List<TraceDetailedProcessParameter.TraceProperty> list = new List<TraceDetailedProcessParameter.TraceProperty>();
				if (documentElement.HasChildNodes)
				{
					foreach (XmlNode childNode in documentElement.ChildNodes)
					{
						TraceDetailedProcessParameter.EnlistRecognizedElements(childNode, list, false, 0);
					}
				}
				foreach (TraceDetailedProcessParameter.TraceProperty item in list)
				{
					listProperties.Items.Add(new ListViewItem(new string[2]
					{
						item.PropertyName,
						item.PropertyValue
					}));
				}
			}
		}

		private void ReloadMessageHeadersInfo(string messageHeadersInfoXml)
		{
			if (!string.IsNullOrEmpty(messageHeadersInfoXml))
			{
				InternalMessageHeaders headers = new InternalMessageHeaders();
				TreeNode treeNode = new TreeNode(SR.GetString("FV_MSG_MSGHEADER"));
				headersTree.Nodes.Add(treeNode);
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(messageHeadersInfoXml);
				XmlElement documentElement = xmlDocument.DocumentElement;
				if (documentElement.HasChildNodes)
				{
					foreach (XmlNode childNode in documentElement.ChildNodes)
					{
						TreeNode treeNode2 = new TreeNode(childNode.Name);
						treeNode.Nodes.Add(treeNode2);
						EnlistMessageHeadersTree(childNode, treeNode2, headers, 0);
					}
				}
				treeNode.Expand();
				ReloadKnownMessageHeaders(headers);
			}
		}

		private void ReloadKnownMessageHeaders(InternalMessageHeaders headers)
		{
			if (headers != null)
			{
				if (!string.IsNullOrEmpty(headers.messageId))
				{
					lblMessageID.Text = SR.GetString("FV_MSG_MSGID");
					txtMessageID.Text = headers.messageId;
				}
				else if (!string.IsNullOrEmpty(headers.relatesTo))
				{
					lblMessageID.Text = SR.GetString("FV_MSG_RelatesTo");
					txtMessageID.Text = headers.relatesTo;
				}
				txtAction.Text = headers.action;
				if (!string.IsNullOrEmpty(headers.activityId))
				{
					if (TraceViewerForm.IsActivityDisplayNameInCache(headers.activityId))
					{
						lblActivityID.Text = SR.GetString("FV_MSG2_ActivityName");
						txtActivityID.Text = TraceViewerForm.GetActivityDisplayName(headers.activityId);
					}
					else
					{
						lblActivityID.Text = SR.GetString("FV_MSG2_ActivityId");
						txtActivityID.Text = headers.activityId;
					}
				}
				else
				{
					lblActivityID.Text = SR.GetString("FV_MSG2_ActivityId");
					txtActivityID.Text = string.Empty;
				}
				txtFrom.Text = headers.from;
				txtTo.Text = headers.to;
				txtReplyTo.Text = headers.replyTo;
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
			lblMessageProperties = new System.Windows.Forms.Label();
			listProperties = new System.Windows.Forms.ListView();
			nameColumn = new System.Windows.Forms.ColumnHeader();
			valueColumn = new System.Windows.Forms.ColumnHeader();
			headerGroup = new System.Windows.Forms.GroupBox();
			txtReplyTo = new System.Windows.Forms.TextBox();
			lblReplyTo = new System.Windows.Forms.Label();
			txtTo = new System.Windows.Forms.TextBox();
			headersTree = new System.Windows.Forms.TreeView();
			lblTo = new System.Windows.Forms.Label();
			txtFrom = new System.Windows.Forms.TextBox();
			lblFrom = new System.Windows.Forms.Label();
			txtActivityID = new System.Windows.Forms.TextBox();
			lblActivityID = new System.Windows.Forms.Label();
			txtMessageID = new System.Windows.Forms.TextBox();
			txtAction = new System.Windows.Forms.TextBox();
			lblAction = new System.Windows.Forms.Label();
			lblMessageID = new System.Windows.Forms.Label();
			lblHeaderTree = new System.Windows.Forms.Label();
			headerGroup.SuspendLayout();
			SuspendLayout();
			lblMessageProperties.Location = new System.Drawing.Point(5, 0);
			lblMessageProperties.Name = "lblMessageProperties";
			lblMessageProperties.Size = new System.Drawing.Size(100, 20);
			lblMessageProperties.TabIndex = 0;
			lblMessageProperties.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_PROPERTY");
			listProperties.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			listProperties.Columns.AddRange(new System.Windows.Forms.ColumnHeader[2]
			{
				nameColumn,
				valueColumn
			});
			listProperties.FullRowSelect = true;
			listProperties.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			listProperties.HideSelection = false;
			listProperties.Location = new System.Drawing.Point(5, 20);
			listProperties.Name = "listProperties";
			listProperties.ShowItemToolTips = true;
			listProperties.Size = new System.Drawing.Size(420, 80);
			listProperties.TabIndex = 1;
			listProperties.UseCompatibleStateImageBehavior = false;
			listProperties.View = System.Windows.Forms.View.Details;
			nameColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_List_NameCol");
			nameColumn.Width = 152;
			valueColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_List_ValueCol");
			valueColumn.Width = 243;
			headerGroup.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			headerGroup.Controls.Add(txtReplyTo);
			headerGroup.Controls.Add(lblReplyTo);
			headerGroup.Controls.Add(txtTo);
			headerGroup.Controls.Add(headersTree);
			headerGroup.Controls.Add(lblTo);
			headerGroup.Controls.Add(txtFrom);
			headerGroup.Controls.Add(lblFrom);
			headerGroup.Controls.Add(txtActivityID);
			headerGroup.Controls.Add(lblActivityID);
			headerGroup.Controls.Add(txtMessageID);
			headerGroup.Controls.Add(txtAction);
			headerGroup.Controls.Add(lblAction);
			headerGroup.Controls.Add(lblMessageID);
			headerGroup.Controls.Add(lblHeaderTree);
			headerGroup.Location = new System.Drawing.Point(5, 110);
			headerGroup.Name = "headerGroup";
			headerGroup.Size = new System.Drawing.Size(420, 369);
			headerGroup.TabIndex = 2;
			headerGroup.TabStop = false;
			headerGroup.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_HEADERS");
			txtReplyTo.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			txtReplyTo.Location = new System.Drawing.Point(110, 210);
			txtReplyTo.Name = "txtReplyTo";
			txtReplyTo.ReadOnly = true;
			txtReplyTo.Size = new System.Drawing.Size(300, 20);
			txtReplyTo.TabIndex = 14;
			lblReplyTo.Location = new System.Drawing.Point(5, 210);
			lblReplyTo.Name = "lblReplyTo";
			lblReplyTo.Size = new System.Drawing.Size(100, 20);
			lblReplyTo.TabIndex = 13;
			lblReplyTo.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_ReplyTo");
			txtTo.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			txtTo.Location = new System.Drawing.Point(110, 180);
			txtTo.Name = "txtTo";
			txtTo.ReadOnly = true;
			txtTo.Size = new System.Drawing.Size(300, 20);
			txtTo.TabIndex = 12;
			headersTree.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			headersTree.Location = new System.Drawing.Point(110, 240);
			headersTree.Name = "headersTree";
			headersTree.Size = new System.Drawing.Size(300, 120);
			headersTree.TabIndex = 16;
			lblTo.Location = new System.Drawing.Point(5, 180);
			lblTo.Name = "lblTo";
			lblTo.Size = new System.Drawing.Size(100, 20);
			lblTo.TabIndex = 11;
			lblTo.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_TO");
			txtFrom.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			txtFrom.Location = new System.Drawing.Point(110, 150);
			txtFrom.Name = "txtFrom";
			txtFrom.ReadOnly = true;
			txtFrom.Size = new System.Drawing.Size(300, 20);
			txtFrom.TabIndex = 10;
			lblFrom.Location = new System.Drawing.Point(5, 150);
			lblFrom.Name = "lblFrom";
			lblFrom.Size = new System.Drawing.Size(100, 20);
			lblFrom.TabIndex = 9;
			lblFrom.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_FROM");
			txtActivityID.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			txtActivityID.Location = new System.Drawing.Point(110, 100);
			txtActivityID.Multiline = true;
			txtActivityID.Name = "txtActivityID";
			txtActivityID.ReadOnly = true;
			txtActivityID.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			txtActivityID.Size = new System.Drawing.Size(300, 40);
			txtActivityID.TabIndex = 8;
			lblActivityID.Location = new System.Drawing.Point(5, 100);
			lblActivityID.Name = "lblActivityID";
			lblActivityID.Size = new System.Drawing.Size(100, 20);
			lblActivityID.TabIndex = 7;
			txtMessageID.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			txtMessageID.Location = new System.Drawing.Point(110, 50);
			txtMessageID.Multiline = true;
			txtMessageID.Name = "txtMessageID";
			txtMessageID.ReadOnly = true;
			txtMessageID.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			txtMessageID.Size = new System.Drawing.Size(300, 40);
			txtMessageID.TabIndex = 6;
			txtAction.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			txtAction.Location = new System.Drawing.Point(110, 20);
			txtAction.Name = "txtAction";
			txtAction.ReadOnly = true;
			txtAction.Size = new System.Drawing.Size(300, 20);
			txtAction.TabIndex = 4;
			lblAction.Location = new System.Drawing.Point(5, 20);
			lblAction.Name = "lblAction";
			lblAction.Size = new System.Drawing.Size(100, 20);
			lblAction.TabIndex = 3;
			lblAction.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_ACTION");
			lblMessageID.Location = new System.Drawing.Point(5, 50);
			lblMessageID.Name = "lblMessageID";
			lblMessageID.Size = new System.Drawing.Size(100, 20);
			lblMessageID.TabIndex = 5;
			lblMessageID.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG_MSGID");
			lblHeaderTree.Location = new System.Drawing.Point(5, 240);
			lblHeaderTree.Name = "lblHeaderTree";
			lblHeaderTree.Size = new System.Drawing.Size(100, 20);
			lblHeaderTree.TabIndex = 15;
			lblHeaderTree.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_HeaderTree");
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			BackColor = System.Drawing.SystemColors.Window;
			base.Controls.Add(headerGroup);
			base.Controls.Add(listProperties);
			base.Controls.Add(lblMessageProperties);
			base.Name = "MessageHeaderTraceInfoControl";
			base.Size = new System.Drawing.Size(430, 485);
			headerGroup.ResumeLayout(performLayout: false);
			headerGroup.PerformLayout();
			ResumeLayout(performLayout: false);
		}
	}
}
