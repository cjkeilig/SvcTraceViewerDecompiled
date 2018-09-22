using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class CustomFilter
	{
		public class CustomFilterParameter
		{
			public CustomFilterParameterValueType type;

			public string description;
		}

		public const int FILTERNAME_AND_NAMESPACE_PREFIX_MAX_LENGTH = 255;

		public const int XPATH_EXPRESSION_MAX_LENGTH = 5120;

		public const int FILTER_DESCRIPTION_AND_NAMESPACE_MAX_LENGTH = 512;

		private bool isErrorOccurs;

		private string filterName = string.Empty;

		private string filterDescription = string.Empty;

		private string expression = string.Empty;

		private Dictionary<string, string> namespaces = new Dictionary<string, string>();

		private List<CustomFilterParameter> parameters = new List<CustomFilterParameter>();

		internal List<CustomFilterParameter> Parameters => parameters;

		public bool ContainsParameters => Parameters.Count != 0;

		public string FilterName => filterName;

		public string FilterDescription => filterDescription;

		public string Expression => expression;

		public Dictionary<string, string> Namespaces => namespaces;

		public CustomFilter(string name, string description, string xpath, Dictionary<string, string> namespaces, List<CustomFilterParameter> parameters)
		{
			if (ValidateInputCustom(name, description, xpath, namespaces, parameters))
			{
				filterName = name;
				if (!string.IsNullOrEmpty(description))
				{
					filterDescription = description;
				}
				expression = xpath;
				if (namespaces != null && namespaces.Count > 0)
				{
					this.namespaces = namespaces;
				}
				if (parameters != null && parameters.Count > 0)
				{
					this.parameters = parameters;
				}
				return;
			}
			throw new AppSettingsException(SR.GetString("CF_Err13"), null);
		}

		public void ChangeFilterName(string name)
		{
			filterName = name;
		}

		public CustomFilter(string xml)
		{
			if (!ValidateXmlData(xml))
			{
				throw new AppSettingsException(SR.GetString("CF_Err14"), null);
			}
			ExtractCustomFilter(xml);
		}

		public string ParseXPathExpression(string parameters)
		{
			if (!ContainsParameters)
			{
				return Expression;
			}
			if (!string.IsNullOrEmpty(parameters))
			{
				string[] array = parameters.Split(';');
				if (array != null)
				{
					if (array.Length == Parameters.Count)
					{
						int num = 0;
						using (List<CustomFilterParameter>.Enumerator enumerator = Parameters.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
                                DateTime t;
								switch (enumerator.Current.type)
								{
								case CustomFilterParameterValueType.DateTime:
									if (!DateTime.TryParse(array[num], CultureInfo.CurrentUICulture, DateTimeStyles.None, out t))
									{
										return null;
									}
									break;
								case CustomFilterParameterValueType.Numeric:
								{
									int result = 0;
									if (!int.TryParse(array[num], NumberStyles.None, CultureInfo.CurrentUICulture, out result))
									{
										return null;
									}
									break;
								}
								}
								num++;
							}
						}
						try
						{
							return string.Format(CultureInfo.CurrentUICulture, Expression, array);
						}
						catch (FormatException)
						{
							return null;
						}
					}
					return null;
				}
				return null;
			}
			return null;
		}

		public void OutputToStream(XmlTextWriter writer)
		{
			if (writer != null)
			{
				ValidateInputCustom(FilterName, FilterDescription, Expression, Namespaces, Parameters);
				try
				{
					writer.WriteStartElement("filter");
					writer.WriteStartElement("name");
					writer.WriteString(FilterName);
					writer.WriteEndElement();
					if (!string.IsNullOrEmpty(FilterDescription))
					{
						writer.WriteStartElement("description");
						writer.WriteString(FilterDescription);
						writer.WriteEndElement();
					}
					writer.WriteStartElement("xpath");
					writer.WriteString(Expression);
					writer.WriteEndElement();
					if (namespaces != null && namespaces.Count != 0)
					{
						writer.WriteStartElement("namespaces");
						foreach (string key in namespaces.Keys)
						{
							writer.WriteStartElement("ns");
							writer.WriteAttributeString("prefix", key);
							writer.WriteString(namespaces[key]);
							writer.WriteEndElement();
						}
						writer.WriteEndElement();
					}
					if (parameters != null && parameters.Count != 0)
					{
						writer.WriteStartElement("parameters");
						foreach (CustomFilterParameter parameter in parameters)
						{
							writer.WriteStartElement("param");
							int type = (int)parameter.type;
							writer.WriteAttributeString("type", type.ToString(CultureInfo.InvariantCulture));
							if (!string.IsNullOrEmpty(parameter.description))
							{
								writer.WriteString(parameter.description);
							}
							writer.WriteEndElement();
						}
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}
				catch (XmlException)
				{
					throw new AppSettingsException(SR.GetString("CF_Err15"), null);
				}
			}
		}

		public static int ExtractXPathParameters(string xpath)
		{
			if (string.IsNullOrEmpty(xpath))
			{
				return 0;
			}
			int num = 0;
			int num2 = 0;
			while (num < xpath.Length)
			{
				if (xpath[num++] == '{')
				{
					num2++;
				}
			}
			return num2;
		}

		public static bool ValidateXPath(string xpath)
		{
			if (string.IsNullOrEmpty(xpath) || xpath.Length > 5120)
			{
				return false;
			}
			if (xpath.StartsWith("|", StringComparison.CurrentCulture) || xpath.StartsWith("&", StringComparison.CurrentCulture))
			{
				return false;
			}
			if (xpath.Contains("<") || xpath.Contains(">"))
			{
				return false;
			}
			Stack<char> stack = new Stack<char>();
			int i = 0;
			int num = 0;
			int num2 = 0;
			bool flag = false;
			for (; i < xpath.Length; i++)
			{
				if (xpath[i] == '{')
				{
					num++;
					if (stack.Count != 0)
					{
						return false;
					}
					stack.Clear();
					flag = true;
				}
				else if (xpath[i] == '}')
				{
					num2++;
					if (stack.Count == 0)
					{
						return false;
					}
					if (stack.Count > 1)
					{
						return false;
					}
					char c = stack.Pop();
					try
					{
						if (int.Parse(new string(new char[1]
						{
							c
						}), CultureInfo.InvariantCulture) > 3)
						{
							return false;
						}
					}
					catch (FormatException)
					{
						return false;
					}
					flag = false;
					stack.Clear();
				}
				else if (flag)
				{
					stack.Push(xpath[i]);
				}
			}
			if (num != num2)
			{
				return false;
			}
			return true;
		}

		public bool ValidateInputCustom(string name, string description, string xpath, Dictionary<string, string> namespaces, List<CustomFilterParameter> parameters)
		{
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(xpath))
			{
				return false;
			}
			if (name.Length > 255 || (!string.IsNullOrEmpty(description) && description.Length > 512) || xpath.Length > 5120)
			{
				return false;
			}
			if (namespaces != null && namespaces.Count != 0)
			{
				foreach (string key in namespaces.Keys)
				{
					if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(namespaces[key]))
					{
						return false;
					}
					if (key.Length > 255 || namespaces[key].Length > 5120)
					{
						return false;
					}
				}
			}
			if (!ValidateXPath(xpath))
			{
				return false;
			}
			if (ExtractXPathParameters(xpath) != 0)
			{
				if (parameters == null || parameters.Count == 0)
				{
					return false;
				}
				if (parameters.Count != ExtractXPathParameters(xpath))
				{
					return false;
				}
				foreach (CustomFilterParameter parameter in parameters)
				{
					if (!string.IsNullOrEmpty(parameter.description) && parameter.description.Length > 5120)
					{
						return false;
					}
				}
			}
			return true;
		}

		private void ExtractCustomFilter(string xml)
		{
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(xml);
				XmlElement documentElement = xmlDocument.DocumentElement;
				if (documentElement != null)
				{
					if (documentElement["name"] == null)
					{
						throw new AppSettingsException(SR.GetString("CF_Err16"), null);
					}
					filterName = documentElement["name"].InnerText;
					if (documentElement["description"] != null)
					{
						filterDescription = documentElement["description"].InnerText;
					}
					if (documentElement["xpath"] == null)
					{
						throw new AppSettingsException(SR.GetString("CF_Err16"), null);
					}
					expression = documentElement["xpath"].InnerText;
					if (documentElement["namespaces"] != null)
					{
						foreach (XmlNode childNode in documentElement["namespaces"].ChildNodes)
						{
							if (childNode.Name == "ns" && !string.IsNullOrEmpty(childNode.InnerText) && childNode.Attributes["prefix"] != null && !string.IsNullOrEmpty(childNode.Attributes["prefix"].Value) && !namespaces.ContainsKey(childNode.Attributes["prefix"].Value))
							{
								namespaces.Add(childNode.Attributes["prefix"].Value, childNode.InnerText);
							}
						}
					}
					if (documentElement["parameters"] != null)
					{
						foreach (XmlNode childNode2 in documentElement["parameters"].ChildNodes)
						{
							if (childNode2.Name == "param" && childNode2.Attributes["type"] != null && !string.IsNullOrEmpty(childNode2.Attributes["type"].Value))
							{
								CustomFilterParameter customFilterParameter = new CustomFilterParameter();
								customFilterParameter.type = (CustomFilterParameterValueType)int.Parse(childNode2.Attributes["type"].Value, CultureInfo.InvariantCulture);
								if (!string.IsNullOrEmpty(childNode2.InnerText))
								{
									customFilterParameter.description = childNode2.InnerText;
								}
								parameters.Add(customFilterParameter);
							}
						}
					}
				}
			}
			catch (XmlException)
			{
				throw new AppSettingsException(SR.GetString("CF_Err16"), null);
			}
		}

		private bool ValidateXmlData(string xml)
		{
			Stream stream = null;
			try
			{
				isErrorOccurs = false;
				XmlDocument xmlDocument = new XmlDocument();
				stream = TempFileManager.GetResourceFileStreamByName("CustomFilterValidator.xsd");
				if (stream != null)
				{
					XmlReader schemaDocument = XmlReader.Create(stream);
					xmlDocument.LoadXml(xml);
					xmlDocument.Schemas.Add(string.Empty, schemaDocument);
					xmlDocument.Validate(InternalValidationHandler);
					if (isErrorOccurs)
					{
						return false;
					}
					return true;
				}
				return false;
			}
			catch (XmlException)
			{
				return false;
			}
			finally
			{
				isErrorOccurs = false;
				Utilities.CloseStreamWithoutException(stream, isFlushStream: false);
			}
		}

		private void InternalValidationHandler(object sender, ValidationEventArgs args)
		{
			isErrorOccurs = true;
		}
	}
}
