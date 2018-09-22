using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class RichTextXmlFormatter
	{
		private int currentPosition;

		private int indent;

		private int rtfHeaderLength;

		private bool isStackTrace;

		private bool isSurpressEndElement;

		private bool isTextInCData;

		private XmlNodeType prevNode;

		private IList<XmlNodeRecord> attributeValueRecords;

		private IList<XmlNodeRecord> textRecords;

		private StringBuilder rtfBuilder = new StringBuilder();

		private char[] stackTraceSeparator = new char[2]
		{
			'\n',
			'\r'
		};

		private XmlReader xmlReader;

		public RichTextXmlFormatter()
		{
			InitializeRtfBuilder();
		}

		private void InitializeFormatWork()
		{
			rtfBuilder.Remove(rtfHeaderLength, rtfBuilder.Length - rtfHeaderLength);
			currentPosition = 0;
			indent = Xml2RtfConfig.IndentIncrement;
		}

		internal string Xml2Rtf(string xml, IList<XmlNodeRecord> attributeValueRecords, IList<XmlNodeRecord> textRecords)
		{
			this.attributeValueRecords = attributeValueRecords;
			this.textRecords = textRecords;
			InitializeFormatWork();
			xmlReader = new XmlTextReader(xml, XmlNodeType.Element, new XmlParserContext(null, null, null, XmlSpace.None));
			try
			{
				while (xmlReader.Read())
				{
					switch (xmlReader.NodeType)
					{
					case XmlNodeType.Element:
						CreateElement();
						break;
					case XmlNodeType.EndElement:
						CreateEndElement();
						break;
					case XmlNodeType.CDATA:
						CreateCData();
						break;
					case XmlNodeType.Text:
						if (isStackTrace)
						{
							CreateStackTrace();
						}
						else
						{
							CreateText();
						}
						break;
					case XmlNodeType.Comment:
						CreateComment();
						break;
					}
				}
				rtfBuilder.Append("\\par}");
			}
			catch (XmlException)
			{
			}
			finally
			{
				xmlReader.Close();
				xmlReader = null;
			}
			return rtfBuilder.ToString();
		}

		private void InitializeRtfBuilder()
		{
			rtfBuilder.Append("{\\rtf1\\ansi\\deff0");
			rtfBuilder.Append("{\\fonttbl{\\f0\\fnil\\fcharset0 Microsoft Sans Serif;}{\\f1\\fnil\\fcharset0 Verdana;}}");
			rtfBuilder.Append("{\\colortbl ;");
			rtfBuilder.Append("\\red0\\green0\\blue255;");
			rtfBuilder.Append("\\red153\\green0\\blue0;");
			rtfBuilder.Append("\\red255\\green0\\blue0;");
			rtfBuilder.Append("\\red0\\green0\\blue0;");
			rtfBuilder.Append("\\red136\\green136\\blue136;");
			rtfBuilder.Append("}\\viewkind4\\uc0\\pard\\f0\\fs");
			rtfBuilder.Append(Xml2RtfConfig.FontSizeFormat);
			rtfHeaderLength = rtfBuilder.Length;
		}

		private void CreateUnicodeString(string s)
		{
			for (int i = 0; i < s.Length; i++)
			{
				rtfBuilder.Append("\\u");
				rtfBuilder.Append(Convert.ToUInt16(s[i]).ToString(CultureInfo.InvariantCulture));
			}
		}

		private void CreateAttributeValue()
		{
			rtfBuilder.Append("\\cf0\\f1\\b ");
			CreateUnicodeString(xmlReader.Value);
			rtfBuilder.Append("\\b0");
			attributeValueRecords.Add(new XmlNodeRecord(xmlReader.Value, currentPosition));
			currentPosition += xmlReader.Value.Length;
		}

		private void CreateElementValue(string s)
		{
			rtfBuilder.Append("\\cf0\\f1\\b ");
			CreateUnicodeString(s);
			rtfBuilder.Append("\\b0");
			textRecords.Add(new XmlNodeRecord(s, currentPosition));
			currentPosition += s.Length;
		}

		private void CreateFormmatedString(string onControl, string source, string offControl, bool isUnicode)
		{
			rtfBuilder.Append(onControl);
			if (isUnicode)
			{
				CreateUnicodeString(source);
			}
			else
			{
				rtfBuilder.Append(source);
			}
			rtfBuilder.Append(offControl);
			currentPosition += source.Length;
		}

		private void CreateWhiteSpace()
		{
			rtfBuilder.Append(" ");
			currentPosition++;
		}

		private void CreateCData()
		{
			CreateNewLine();
			CreateFormmatedString("\\cf1\\f1\\", "<![CDATA[", string.Empty, isUnicode: false);
			isTextInCData = true;
			indent += Xml2RtfConfig.IndentIncrement;
			CreateText();
			indent -= Xml2RtfConfig.IndentIncrement;
			isTextInCData = false;
		}

		private void CreateComment()
		{
			CreateNewLine();
			CreateFormmatedString("\\cf1\\f1", "<!-- ", string.Empty, isUnicode: false);
			CreateFormmatedString("\\cf5\\f1", xmlReader.Value, "\\b0", isUnicode: true);
			CreateFormmatedString("\\cf1\\f1", " -->", string.Empty, isUnicode: false);
			prevNode = XmlNodeType.Comment;
			isSurpressEndElement = false;
		}

		private void CreateElement()
		{
			CreateNewLine();
			prevNode = XmlNodeType.Element;
			bool hasAttributes = xmlReader.HasAttributes;
			bool isEmptyElement = xmlReader.IsEmptyElement;
			isStackTrace = (xmlReader.Name.Equals("Callstack", StringComparison.Ordinal) || xmlReader.Name.Equals("StackTrace", StringComparison.Ordinal));
			CreateFormmatedString("\\cf1\\f1", "<", string.Empty, isUnicode: false);
			CreateFormmatedString("\\cf2\\f1", xmlReader.Name, string.Empty, isUnicode: false);
			if (hasAttributes)
			{
				while (xmlReader.MoveToNextAttribute())
				{
					CreateWhiteSpace();
					bool num = xmlReader.Name.Equals("xmlns", StringComparison.CurrentCultureIgnoreCase) || xmlReader.Name.StartsWith("xmlns:", StringComparison.CurrentCultureIgnoreCase);
					if (num)
					{
						CreateFormmatedString("\\cf3\\f1", xmlReader.Name, string.Empty, isUnicode: false);
					}
					else
					{
						CreateFormmatedString("\\cf2\\f1", xmlReader.Name, string.Empty, isUnicode: false);
					}
					CreateFormmatedString("\\cf1\\f1", "=", string.Empty, isUnicode: false);
					CreateFormmatedString("\\cf1\\f1", "\"", string.Empty, isUnicode: false);
					if (num)
					{
						CreateFormmatedString("\\cf3\\f1\\b ", xmlReader.Value, "\\b0", isUnicode: true);
						attributeValueRecords.Add(new XmlNodeRecord(xmlReader.Value, currentPosition - xmlReader.Value.Length));
					}
					else
					{
						CreateAttributeValue();
					}
					CreateFormmatedString("\\cf1\\f1", "\"", string.Empty, isUnicode: false);
				}
			}
			if (isEmptyElement)
			{
				CreateWhiteSpace();
				CreateFormmatedString("\\cf1\\f1", "/>", string.Empty, isUnicode: false);
			}
			else
			{
				CreateFormmatedString("\\cf1\\f1", ">", string.Empty, isUnicode: false);
				indent += Xml2RtfConfig.IndentIncrement;
			}
		}

		private void CreateEndElement()
		{
			isStackTrace = false;
			indent -= Xml2RtfConfig.IndentIncrement;
			if (prevNode == XmlNodeType.Element)
			{
				isSurpressEndElement = true;
			}
			if (!isSurpressEndElement)
			{
				CreateNewLine();
			}
			prevNode = XmlNodeType.EndElement;
			CreateFormmatedString("\\cf1\\f1", "</", string.Empty, isUnicode: false);
			CreateFormmatedString("\\cf2\\f1", xmlReader.Name, string.Empty, isUnicode: false);
			CreateFormmatedString("\\cf1\\f1", ">", string.Empty, isUnicode: false);
			isSurpressEndElement = false;
		}

		private void CreateStackTrace()
		{
			isSurpressEndElement = false;
			prevNode = XmlNodeType.Text;
			string[] array = xmlReader.Value.Split(stackTraceSeparator, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				if (!string.IsNullOrEmpty(text))
				{
					CreateNewLine();
					string s = text.Trim();
					CreateElementValue(s);
				}
			}
		}

		private void CreateText()
		{
			prevNode = XmlNodeType.Text;
			if (isTextInCData)
			{
				CreateNewLine();
			}
			else
			{
				isSurpressEndElement = true;
			}
			CreateElementValue(xmlReader.Value);
		}

		private void CreateNewLine()
		{
			rtfBuilder.Append("\\par\\pard");
			rtfBuilder.Append("\\li");
			rtfBuilder.Append(indent.ToString(CultureInfo.InvariantCulture));
			currentPosition++;
		}
	}
}
