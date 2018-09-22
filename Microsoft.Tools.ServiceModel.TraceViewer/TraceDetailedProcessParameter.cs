using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceDetailedProcessParameter : IEnumerator
	{
		public class TraceProperty
		{
			private string propertyName;

			private string propertyValue;

			private bool isXmlAttribute;

			private bool isXmlFormat;

			private object additionalData;

			public object AdditionalData
			{
				get
				{
					return additionalData;
				}
				set
				{
					additionalData = value;
				}
			}

			public bool IsXmlFormat => isXmlFormat;

			public bool IsXmlAttribute => isXmlAttribute;

			public string PropertyName => propertyName;

			public string PropertyValue => propertyValue;

			public TraceProperty(string name, string value, bool isAttribute, bool isXmlFormat)
			{
				propertyName = name;
				propertyValue = value;
				isXmlAttribute = isAttribute;
				this.isXmlFormat = isXmlFormat;
			}
		}

		public const int MAX_E2E_TREE_DEPTH = 10;

		public const int INIT_E2E_TREE_DEPTH = 0;

		private static List<string> excludedXmlAttributes;

		private static List<string> excludedXmlNodes;

		private static List<string> subTreeRootXmlNodes;

		private object thisLock = new object();

		private TraceRecord trace;

		private List<TraceProperty> listTraceProperty = new List<TraceProperty>();

		private TraceProperty[] currentEnumItems;

		private int currentEnumIndex;

		private object ThisLock => thisLock;

		public static List<string> ExcludedXmlAttributes => excludedXmlAttributes;

		public static List<string> ExcludedXmlNodes => excludedXmlNodes;

		public static List<string> SubTreeRootXmlNodes => subTreeRootXmlNodes;

		internal TraceRecord RelatedTraceRecord => trace;

		public int PropertyCount
		{
			get
			{
				int num = 0;
				foreach (TraceProperty item in listTraceProperty)
				{
					if (!item.IsXmlFormat)
					{
						num++;
					}
				}
				return num;
			}
		}

		object IEnumerator.Current
		{
			get
			{
				return GetCurrentItem();
			}
		}

		static TraceDetailedProcessParameter()
		{
			excludedXmlAttributes = new List<string>();
			excludedXmlNodes = new List<string>();
			subTreeRootXmlNodes = new List<string>();
			excludedXmlAttributes.Add("xmlns");
			excludedXmlNodes.Add("#text");
			excludedXmlNodes.Add("#comment");
			subTreeRootXmlNodes.Add("Exception");
			subTreeRootXmlNodes.Add("System.Diagnostics");
			subTreeRootXmlNodes.Add("MessageProperties");
			subTreeRootXmlNodes.Add("MessageHeaders");
			subTreeRootXmlNodes.Add("MessageLogTraceRecord");
		}

		public TraceDetailedProcessParameter(TraceRecord trace)
		{
			if (trace == null || string.IsNullOrEmpty(trace.Xml))
			{
				throw new TraceViewerException(SR.GetString("FV_Error_Init"));
			}
			this.trace = trace;
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(trace.Xml);
				XmlElement documentElement = xmlDocument.DocumentElement;
				if (documentElement != null && documentElement["ApplicationData"] != null)
				{
					StringBuilder stringBuilder = new StringBuilder();
					foreach (XmlNode childNode in documentElement["ApplicationData"].ChildNodes)
					{
						if (childNode.NodeType == XmlNodeType.Text || childNode.NodeType == XmlNodeType.Comment || childNode.NodeType == XmlNodeType.CDATA)
						{
							stringBuilder.Append(childNode.InnerText);
						}
					}
					if (stringBuilder.Length != 0)
					{
						listTraceProperty.Add(new TraceProperty(SR.GetString("FV_AppDataText"), stringBuilder.ToString(), isAttribute: false, isXmlFormat: false));
					}
					foreach (XmlNode childNode2 in documentElement["ApplicationData"].ChildNodes)
					{
						EnlistRecognizedElements(childNode2, 0);
					}
				}
			}
			catch (XmlException e)
			{
				throw new TraceViewerException(SR.GetString("FV_Error_Init"), e);
			}
		}

		private static bool SubTreeRootXmlNodeValidator(XmlNode node, string elementNameTradeOff)
		{
			if (!string.IsNullOrEmpty(elementNameTradeOff) && node != null && elementNameTradeOff == "Exception")
			{
				if (node.HasChildNodes && node["ExceptionType"] != null && node["Message"] != null && node["StackTrace"] != null)
				{
					return true;
				}
				return false;
			}
			return true;
		}

		public static void EnlistRecognizedElements(XmlNode node, List<TraceProperty> listProperty, bool ignoreSubTreeNodes, int depth)
		{
			if (depth < 10 && node != null && !ExcludedXmlNodes.Contains(node.Name))
			{
				string text = Utilities.TradeOffXmlPrefixForName(node.Name);
				if (ignoreSubTreeNodes && SubTreeRootXmlNodes.Contains(text) && SubTreeRootXmlNodeValidator(node, text))
				{
					listProperty.Add(new TraceProperty(node.Name, node.OuterXml, isAttribute: false, isXmlFormat: true));
				}
				else
				{
					if (node.Attributes != null)
					{
						foreach (XmlAttribute attribute in node.Attributes)
						{
							if (!ExcludedXmlAttributes.Contains(Utilities.TradeOffXmlPrefixForName(attribute.Name)))
							{
								listProperty.Add(new TraceProperty(SR.GetString("FV_MSG2_LeftQ") + node.Name + SR.GetString("FV_MSG2_RightQ") + attribute.Name, attribute.Value, isAttribute: false, isXmlFormat: false));
							}
						}
					}
					if (node.HasChildNodes)
					{
						foreach (XmlNode childNode in node.ChildNodes)
						{
							if (string.Compare(childNode.Name, "#text", true, CultureInfo.CurrentUICulture) == 0 && !string.IsNullOrEmpty(childNode.Value))
							{
								if (string.Compare(Utilities.TradeOffXmlPrefixForName(node.Name), "ActivityId", true, CultureInfo.CurrentUICulture) == 0)
								{
									string text2 = TraceRecord.NormalizeActivityId(childNode.InnerText);
									if (!string.IsNullOrEmpty(text2) && TraceViewerForm.IsActivityDisplayNameInCache(text2))
									{
										listProperty.Add(new TraceProperty(SR.GetString("FV_Basic_ActivityName"), TraceViewerForm.GetActivityDisplayName(text2), isAttribute: false, isXmlFormat: false));
									}
									else
									{
										listProperty.Add(new TraceProperty(SR.GetString("FV_Basic_ActivityID"), text2, isAttribute: false, isXmlFormat: false));
									}
								}
								else
								{
									listProperty.Add(new TraceProperty(node.Name, childNode.InnerText, isAttribute: false, isXmlFormat: false));
								}
							}
							else
							{
								EnlistRecognizedElements(childNode, listProperty, ignoreSubTreeNodes, depth + 1);
							}
						}
					}
					else if (!string.IsNullOrEmpty(node.Value) && !ExcludedXmlNodes.Contains(Utilities.TradeOffXmlPrefixForName(node.Name)))
					{
						if (string.Compare(Utilities.TradeOffXmlPrefixForName(node.Name), "ActivityId", true, CultureInfo.CurrentUICulture) == 0)
						{
							listProperty.Add(new TraceProperty(node.Name, TraceViewerForm.GetActivityDisplayName(TraceRecord.NormalizeActivityId(node.InnerText)), isAttribute: false, isXmlFormat: false));
						}
						else
						{
							listProperty.Add(new TraceProperty(node.Name, node.Value, isAttribute: false, isXmlFormat: false));
						}
					}
				}
			}
		}

		private void EnlistRecognizedElements(XmlNode node, int depth)
		{
			EnlistRecognizedElements(node, listTraceProperty, true, depth);
		}

		public void RemoveProperty(TraceProperty property)
		{
			if (property != null && listTraceProperty.Contains(property))
			{
				listTraceProperty.Remove(property);
			}
		}

		private TraceProperty GetCurrentItem()
		{
			lock (ThisLock)
			{
				if (currentEnumItems != null && currentEnumItems.Length != 0 && currentEnumIndex >= 0 && currentEnumIndex < currentEnumItems.Length)
				{
					return currentEnumItems[currentEnumIndex];
				}
				return null;
			}
		}

		public IEnumerator GetEnumerator()
		{
			((IEnumerator)this).Reset();
			return this;
		}

		bool IEnumerator.MoveNext()
		{
			lock (ThisLock)
			{
				if (currentEnumItems != null && currentEnumItems.Length != 0)
				{
					return ++currentEnumIndex < currentEnumItems.Length;
				}
				return false;
			}
		}

		void IEnumerator.Reset()
		{
			lock (ThisLock)
			{
				currentEnumItems = new TraceProperty[listTraceProperty.Count];
				listTraceProperty.CopyTo(currentEnumItems);
				currentEnumIndex = -1;
			}
		}
	}
}
