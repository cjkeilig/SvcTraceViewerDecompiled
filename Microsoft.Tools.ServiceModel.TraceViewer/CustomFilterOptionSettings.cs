using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class CustomFilterOptionSettings
	{
		private bool showWCFTraces = true;

		private bool showTransfer = true;

		private bool showMessageSentReceived = true;

		private bool showSecurityMessage = true;

		private bool showReliableMessage = true;

		private bool showTransactionMessage = true;

		public bool ShowWCFTraces
		{
			get
			{
				return showWCFTraces;
			}
			set
			{
				showWCFTraces = value;
			}
		}

		public bool ShowTransactionMessage
		{
			get
			{
				return showTransactionMessage;
			}
			set
			{
				showTransactionMessage = value;
			}
		}

		public bool ShowReliableMessage
		{
			get
			{
				return showReliableMessage;
			}
			set
			{
				showReliableMessage = value;
			}
		}

		public bool ShowSecurityMessage
		{
			get
			{
				return showSecurityMessage;
			}
			set
			{
				showSecurityMessage = value;
			}
		}

		public bool ShowTransfer
		{
			get
			{
				return showTransfer;
			}
			set
			{
				showTransfer = value;
			}
		}

		public bool ShowMessageSentReceived
		{
			get
			{
				return showMessageSentReceived;
			}
			set
			{
				showMessageSentReceived = value;
			}
		}

		public bool IsSet
		{
			get
			{
				if (!ShowWCFTraces)
				{
					return true;
				}
				if (ShowTransfer && ShowMessageSentReceived && ShowSecurityMessage && ShowReliableMessage)
				{
					return !ShowTransactionMessage;
				}
				return true;
			}
		}

		public void OutputToStream(XmlTextWriter writer)
		{
			if (writer != null)
			{
				writer.WriteStartElement("showWCFTraces");
				writer.WriteAttributeString("enabled", ShowWCFTraces ? "yes" : "no");
				writer.WriteEndElement();
				writer.WriteStartElement("showTransfer");
				writer.WriteAttributeString("enabled", ShowTransfer ? "yes" : "no");
				writer.WriteEndElement();
				writer.WriteStartElement("showMessageSentReceived");
				writer.WriteAttributeString("enabled", ShowMessageSentReceived ? "yes" : "no");
				writer.WriteEndElement();
				writer.WriteStartElement("showSecurityMessage");
				writer.WriteAttributeString("enabled", ShowSecurityMessage ? "yes" : "no");
				writer.WriteEndElement();
				writer.WriteStartElement("showReliableMessage");
				writer.WriteAttributeString("enabled", ShowReliableMessage ? "yes" : "no");
				writer.WriteEndElement();
				writer.WriteStartElement("showTransactionMessage");
				writer.WriteAttributeString("enabled", ShowTransactionMessage ? "yes" : "no");
				writer.WriteEndElement();
			}
		}

		public CustomFilterOptionSettings()
		{
		}

		public CustomFilterOptionSettings(XmlNode node)
		{
			if (node != null)
			{
				foreach (XmlNode childNode in node.ChildNodes)
				{
					switch (childNode.Name)
					{
					case "showWCFTraces":
						if (childNode.Attributes["enabled"] != null)
						{
							ShowWCFTraces = ((childNode.Attributes["enabled"].Value == "yes") ? true : false);
						}
						else
						{
							ShowWCFTraces = true;
						}
						break;
					case "showTransfer":
						if (childNode.Attributes["enabled"] != null)
						{
							ShowTransfer = ((childNode.Attributes["enabled"].Value == "yes") ? true : false);
						}
						else
						{
							ShowTransfer = true;
						}
						break;
					case "showMessageSentReceived":
						if (childNode.Attributes["enabled"] != null)
						{
							ShowMessageSentReceived = ((childNode.Attributes["enabled"].Value == "yes") ? true : false);
						}
						else
						{
							ShowMessageSentReceived = true;
						}
						break;
					case "showSecurityMessage":
						if (childNode.Attributes["enabled"] != null)
						{
							ShowSecurityMessage = ((childNode.Attributes["enabled"].Value == "yes") ? true : false);
						}
						else
						{
							ShowSecurityMessage = true;
						}
						break;
					case "showReliableMessage":
						if (childNode.Attributes["enabled"] != null)
						{
							ShowReliableMessage = ((childNode.Attributes["enabled"].Value == "yes") ? true : false);
						}
						else
						{
							ShowReliableMessage = true;
						}
						break;
					case "showTransactionMessage":
						if (childNode.Attributes["enabled"] != null)
						{
							ShowTransactionMessage = ((childNode.Attributes["enabled"].Value == "yes") ? true : false);
						}
						else
						{
							ShowTransactionMessage = true;
						}
						break;
					}
				}
			}
		}
	}
}
