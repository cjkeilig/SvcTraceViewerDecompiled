using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class MessageTraceInfoControl : UserControl
	{
		private class InternalMessageTraceInfo
		{
			public DateTime time;

			public string source;

			public string type;

			public string method;

			public List<TraceDetailedProcessParameter.TraceProperty> properties = new List<TraceDetailedProcessParameter.TraceProperty>();

			public List<TraceDetailedProcessParameter.TraceProperty> headers = new List<TraceDetailedProcessParameter.TraceProperty>();

			public List<TraceDetailedProcessParameter.TraceProperty> parameters = new List<TraceDetailedProcessParameter.TraceProperty>();
		}

		private enum TabFocusIndex
		{
			GeneralGroup,
			TimeLabel,
			Time,
			SourceLabel,
			Source,
			TypeLabel,
			Type,
			PropertyLabel,
			Property,
			EnvelopeGroup,
			HeaderLabel,
			Header,
			MethodLabel,
			Method,
			ParameterLabel,
			Parameter
		}

		private readonly string dateTimeFormat = SR.GetString("FV_DateTimeFormat");

		private IContainer components;

		private GroupBox generalGroup;

		private Label lblTime;

		private TextBox txtTime;

		private TextBox txtSource;

		private Label lblSource;

		private TextBox txtType;

		private Label lblType;

		private Label lblProperties;

		private ListView listProperties;

		private ColumnHeader nameColumn;

		private ColumnHeader valueColumn;

		private GroupBox envelopeGroup;

		private ListView listHeaders;

		private ColumnHeader columnHeader1;

		private ColumnHeader columnHeader2;

		private Label lblHeaders;

		private Label lblMethod;

		private TextBox txtMethod;

		private Label lblParameters;

		private ListView listParameters;

		private ColumnHeader columnHeader3;

		private ColumnHeader columnHeader4;

		private ColumnHeader columnHeader5;

		public void CleanUp()
		{
			txtMethod.Clear();
			txtSource.Clear();
			txtTime.Clear();
			txtType.Clear();
			listHeaders.Items.Clear();
			listParameters.Items.Clear();
			listProperties.Items.Clear();
		}

		private void DisplayInfo(InternalMessageTraceInfo info)
		{
			if (info != null)
			{
				txtMethod.Text = info.method;
				txtSource.Text = info.source;
				txtTime.Text = info.time.ToString(dateTimeFormat, CultureInfo.CurrentUICulture);
				txtType.Text = info.type;
				foreach (TraceDetailedProcessParameter.TraceProperty property in info.properties)
				{
					listProperties.Items.Add(new ListViewItem(new string[2]
					{
						property.PropertyName,
						property.PropertyValue
					}));
				}
				foreach (TraceDetailedProcessParameter.TraceProperty parameter in info.parameters)
				{
					listParameters.Items.Add(new ListViewItem(new string[3]
					{
						parameter.PropertyName,
						parameter.PropertyValue,
						(parameter.AdditionalData == null) ? string.Empty : parameter.AdditionalData.ToString()
					}));
				}
				foreach (TraceDetailedProcessParameter.TraceProperty header in info.headers)
				{
					listHeaders.Items.Add(new ListViewItem(new string[2]
					{
						header.PropertyName,
						header.PropertyValue
					}));
				}
			}
		}

		public void ReloadMessageInfo(string messageXml)
		{
			if (!string.IsNullOrEmpty(messageXml))
			{
				try
				{
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.LoadXml(messageXml);
					XmlElement documentElement = xmlDocument.DocumentElement;
					InternalMessageTraceInfo info = new InternalMessageTraceInfo();
					ExtractKnownInfo(documentElement, info);
					if (documentElement.HasChildNodes)
					{
						foreach (XmlNode childNode in documentElement.ChildNodes)
						{
							string a = Utilities.TradeOffXmlPrefixForName(childNode.Name);
							if (a == "Envelope")
							{
								ExtractSoapEnvelop(childNode, info);
							}
							else
							{
								EnlistProperties(childNode, info);
							}
						}
					}
					DisplayInfo(info);
				}
				catch (XmlException e)
				{
					throw new TraceViewerException(SR.GetString("FV_ERROR"), e);
				}
			}
		}

		private void ExtractKnownInfo(XmlElement element, InternalMessageTraceInfo info)
		{
			if (element != null && element.NodeType == XmlNodeType.Element && string.Compare(element.Name, "MessageLogTraceRecord", true, CultureInfo.CurrentUICulture) == 0 && info != null)
			{
				if (element.HasAttribute("Time"))
				{
					try
					{
						info.time = DateTime.Parse(element.Attributes["Time"].Value, CultureInfo.CurrentUICulture);
					}
					catch (FormatException)
					{
					}
				}
				if (element.HasAttribute("Type"))
				{
					info.type = element.Attributes["Type"].Value;
				}
				if (element.HasAttribute("Source"))
				{
					info.source = element.Attributes["Source"].Value;
				}
			}
		}

		private void EnlistProperties(XmlNode node, InternalMessageTraceInfo info)
		{
			if (node != null && info != null && node.HasChildNodes)
			{
				List<TraceDetailedProcessParameter.TraceProperty> list = new List<TraceDetailedProcessParameter.TraceProperty>();
				TraceDetailedProcessParameter.EnlistRecognizedElements(node, list,  false, 0);
				if (node.Name != "Properties")
				{
					foreach (TraceDetailedProcessParameter.TraceProperty item in list)
					{
						if (string.Compare(Utilities.TradeOffXmlPrefixForName(item.PropertyName), "ActivityId",  true, CultureInfo.CurrentUICulture) == 0)
						{
							string text = TraceRecord.NormalizeActivityId(item.PropertyValue);
							if (!string.IsNullOrEmpty(text) && TraceViewerForm.IsActivityDisplayNameInCache(text))
							{
								info.properties.Add(new TraceDetailedProcessParameter.TraceProperty(SR.GetString("FV_MSG2_LeftQ") + SR.GetString("FV_MSG2_ActivityName2") + SR.GetString("FV_MSG2_RightQ") + item.PropertyName, TraceViewerForm.GetActivityDisplayName(text), item.IsXmlAttribute, item.IsXmlFormat));
							}
							else
							{
								info.properties.Add(new TraceDetailedProcessParameter.TraceProperty(SR.GetString("FV_MSG2_LeftQ") + "ActivityId" + SR.GetString("FV_MSG2_RightQ") + item.PropertyName, text, item.IsXmlAttribute, item.IsXmlFormat));
							}
						}
						else
						{
							info.properties.Add(new TraceDetailedProcessParameter.TraceProperty(SR.GetString("FV_MSG2_LeftQ") + node.Name + SR.GetString("FV_MSG2_RightQ") + item.PropertyName, item.PropertyValue, item.IsXmlAttribute, item.IsXmlFormat));
						}
					}
				}
				else
				{
					info.properties.AddRange(list);
				}
			}
		}

		private void ExtractSoapEnvelop(XmlNode node, InternalMessageTraceInfo info)
		{
			if (node != null && info != null && node.HasChildNodes)
			{
				foreach (XmlNode item in node)
				{
					string a = Utilities.TradeOffXmlPrefixForName(item.Name);
					if (!(a == "Header"))
					{
						if (a == "Body")
						{
							ExtractSoapBody(item, info);
						}
					}
					else
					{
						EnlistSoapHeaders(item, info);
					}
				}
			}
		}

		private void ExtractSoapBody(XmlNode node, InternalMessageTraceInfo info)
		{
			if (node != null && info != null && node.HasChildNodes)
			{
				info.method = node.ChildNodes[0].Name;
				EnlistParameters(node.ChildNodes[0], info);
			}
		}

		private void EnlistSoapHeaders(XmlNode node, InternalMessageTraceInfo info)
		{
			if (node != null && info != null && node.HasChildNodes)
			{
				TraceDetailedProcessParameter.EnlistRecognizedElements(node, info.headers,  false, 0);
			}
		}

		private void EnlistParameters(XmlNode node, InternalMessageTraceInfo info)
		{
			if (node != null && info != null && node.HasChildNodes)
			{
				foreach (XmlNode childNode in node.ChildNodes)
				{
					TraceDetailedProcessParameter.TraceProperty traceProperty = new TraceDetailedProcessParameter.TraceProperty(childNode.Name, childNode.InnerText, isAttribute: false, isXmlFormat: false);
					if (childNode.Attributes != null && childNode.Attributes["ValueType"] != null)
					{
						traceProperty.AdditionalData = childNode.Attributes["ValueType"].Value;
					}
					info.parameters.Add(traceProperty);
				}
			}
		}

		public MessageTraceInfoControl()
		{
			InitializeComponent();
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
			generalGroup = new System.Windows.Forms.GroupBox();
			listProperties = new System.Windows.Forms.ListView();
			nameColumn = new System.Windows.Forms.ColumnHeader();
			valueColumn = new System.Windows.Forms.ColumnHeader();
			lblProperties = new System.Windows.Forms.Label();
			txtType = new System.Windows.Forms.TextBox();
			lblType = new System.Windows.Forms.Label();
			txtSource = new System.Windows.Forms.TextBox();
			lblSource = new System.Windows.Forms.Label();
			txtTime = new System.Windows.Forms.TextBox();
			lblTime = new System.Windows.Forms.Label();
			envelopeGroup = new System.Windows.Forms.GroupBox();
			listParameters = new System.Windows.Forms.ListView();
			columnHeader3 = new System.Windows.Forms.ColumnHeader();
			columnHeader4 = new System.Windows.Forms.ColumnHeader();
			columnHeader5 = new System.Windows.Forms.ColumnHeader();
			lblParameters = new System.Windows.Forms.Label();
			txtMethod = new System.Windows.Forms.TextBox();
			lblMethod = new System.Windows.Forms.Label();
			listHeaders = new System.Windows.Forms.ListView();
			columnHeader1 = new System.Windows.Forms.ColumnHeader();
			columnHeader2 = new System.Windows.Forms.ColumnHeader();
			lblHeaders = new System.Windows.Forms.Label();
			generalGroup.SuspendLayout();
			envelopeGroup.SuspendLayout();
			SuspendLayout();
			generalGroup.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			generalGroup.Controls.Add(listProperties);
			generalGroup.Controls.Add(lblProperties);
			generalGroup.Controls.Add(txtType);
			generalGroup.Controls.Add(lblType);
			generalGroup.Controls.Add(txtSource);
			generalGroup.Controls.Add(lblSource);
			generalGroup.Controls.Add(txtTime);
			generalGroup.Controls.Add(lblTime);
			generalGroup.Location = new System.Drawing.Point(5, 0);
			generalGroup.Name = "generalGroup";
			generalGroup.Size = new System.Drawing.Size(420, 225);
			generalGroup.TabIndex = 0;
			generalGroup.TabStop = false;
			generalGroup.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_GMsgInfo");
			listProperties.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			listProperties.Columns.AddRange(new System.Windows.Forms.ColumnHeader[2]
			{
				nameColumn,
				valueColumn
			});
			listProperties.FullRowSelect = true;
			listProperties.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			listProperties.HideSelection = false;
			listProperties.Location = new System.Drawing.Point(110, 110);
			listProperties.Name = "listProperties";
			listProperties.ShowItemToolTips = true;
			listProperties.Size = new System.Drawing.Size(300, 110);
			listProperties.TabIndex = 8;
			listProperties.UseCompatibleStateImageBehavior = false;
			listProperties.View = System.Windows.Forms.View.Details;
			nameColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_List_NameCol");
			nameColumn.Width = 109;
			valueColumn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_List_ValueCol");
			valueColumn.Width = 183;
			lblProperties.Location = new System.Drawing.Point(5, 110);
			lblProperties.Name = "lblProperties";
			lblProperties.Size = new System.Drawing.Size(100, 20);
			lblProperties.TabIndex = 7;
			lblProperties.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_Properties");
			txtType.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			txtType.Location = new System.Drawing.Point(110, 80);
			txtType.Name = "txtType";
			txtType.ReadOnly = true;
			txtType.Size = new System.Drawing.Size(300, 20);
			txtType.TabIndex = 6;
			lblType.Location = new System.Drawing.Point(5, 80);
			lblType.Name = "lblType";
			lblType.Size = new System.Drawing.Size(100, 20);
			lblType.TabIndex = 5;
			lblType.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_MsgType");
			txtSource.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			txtSource.Location = new System.Drawing.Point(110, 50);
			txtSource.Name = "txtSource";
			txtSource.ReadOnly = true;
			txtSource.Size = new System.Drawing.Size(300, 20);
			txtSource.TabIndex = 4;
			lblSource.Location = new System.Drawing.Point(5, 50);
			lblSource.Name = "lblSource";
			lblSource.Size = new System.Drawing.Size(200, 20);
			lblSource.TabIndex = 3;
			lblSource.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_MsgSource");
			txtTime.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			txtTime.Location = new System.Drawing.Point(110, 20);
			txtTime.Name = "txtTime";
			txtTime.ReadOnly = true;
			txtTime.Size = new System.Drawing.Size(300, 20);
			txtTime.TabIndex = 2;
			lblTime.Location = new System.Drawing.Point(5, 20);
			lblTime.Name = "lblTime";
			lblTime.Size = new System.Drawing.Size(200, 20);
			lblTime.TabIndex = 1;
			lblTime.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_MsgTime");
			envelopeGroup.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			envelopeGroup.Controls.Add(listParameters);
			envelopeGroup.Controls.Add(lblParameters);
			envelopeGroup.Controls.Add(txtMethod);
			envelopeGroup.Controls.Add(lblMethod);
			envelopeGroup.Controls.Add(listHeaders);
			envelopeGroup.Controls.Add(lblHeaders);
			envelopeGroup.Location = new System.Drawing.Point(5, 231);
			envelopeGroup.Name = "envelopeGroup";
			envelopeGroup.Size = new System.Drawing.Size(420, 285);
			envelopeGroup.TabIndex = 9;
			envelopeGroup.TabStop = false;
			envelopeGroup.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_EnvelopeInfo");
			listParameters.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			listParameters.Columns.AddRange(new System.Windows.Forms.ColumnHeader[3]
			{
				columnHeader3,
				columnHeader4,
				columnHeader5
			});
			listParameters.FullRowSelect = true;
			listParameters.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			listParameters.HideSelection = false;
			listParameters.Location = new System.Drawing.Point(110, 170);
			listParameters.Name = "listParameters";
			listParameters.ShowItemToolTips = true;
			listParameters.Size = new System.Drawing.Size(300, 110);
			listParameters.TabIndex = 15;
			listParameters.UseCompatibleStateImageBehavior = false;
			listParameters.View = System.Windows.Forms.View.Details;
			columnHeader3.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_List_NameCol");
			columnHeader3.Width = 70;
			columnHeader4.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_List_ValueCol");
			columnHeader4.Width = 100;
			columnHeader5.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_List_TypeCol");
			columnHeader5.Width = 100;
			lblParameters.Location = new System.Drawing.Point(5, 170);
			lblParameters.Name = "lblParameters";
			lblParameters.Size = new System.Drawing.Size(100, 20);
			lblParameters.TabIndex = 14;
			lblParameters.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_Parameters");
			txtMethod.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			txtMethod.Location = new System.Drawing.Point(110, 140);
			txtMethod.Name = "txtMethod";
			txtMethod.ReadOnly = true;
			txtMethod.Size = new System.Drawing.Size(300, 20);
			txtMethod.TabIndex = 13;
			lblMethod.Location = new System.Drawing.Point(5, 140);
			lblMethod.Name = "lblMethod";
			lblMethod.Size = new System.Drawing.Size(100, 20);
			lblMethod.TabIndex = 12;
			lblMethod.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_Method");
			listHeaders.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
			listHeaders.Columns.AddRange(new System.Windows.Forms.ColumnHeader[2]
			{
				columnHeader1,
				columnHeader2
			});
			listHeaders.FullRowSelect = true;
			listHeaders.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			listHeaders.HideSelection = false;
			listHeaders.Location = new System.Drawing.Point(110, 20);
			listHeaders.Name = "listHeaders";
			listHeaders.ShowItemToolTips = true;
			listHeaders.Size = new System.Drawing.Size(300, 110);
			listHeaders.TabIndex = 11;
			listHeaders.UseCompatibleStateImageBehavior = false;
			listHeaders.View = System.Windows.Forms.View.Details;
			columnHeader1.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_List_NameCol");
			columnHeader1.Width = 109;
			columnHeader2.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_List_ValueCol");
			columnHeader2.Width = 183;
			lblHeaders.Location = new System.Drawing.Point(5, 20);
			lblHeaders.Name = "lblHeaders";
			lblHeaders.Size = new System.Drawing.Size(100, 20);
			lblHeaders.TabIndex = 10;
			lblHeaders.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("FV_MSG2_Headers2");
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(envelopeGroup);
			base.Controls.Add(generalGroup);
			base.Name = "MessageTraceInfoControl";
			base.Size = new System.Drawing.Size(430, 520);
			generalGroup.ResumeLayout(performLayout: false);
			generalGroup.PerformLayout();
			envelopeGroup.ResumeLayout(performLayout: false);
			envelopeGroup.PerformLayout();
			ResumeLayout(performLayout: false);
		}
	}
}
