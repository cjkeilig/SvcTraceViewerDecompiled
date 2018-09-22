using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal static class FilterEngine
	{
		private static object filterEngineLock;

		private static FilterCriteria currentFilterCriteria;

		private static Dictionary<string, SearchOptions> searchOptionNames;

		private static Dictionary<string, SourceLevels> traceLevelNames;

		private static CustomFilterOptionSettings filterOptionSettings;

		private static Dictionary<string, int> securityMessageActionIndex;

		private static Dictionary<string, int> reliableMessageActionIndex;

		private static Dictionary<string, int> transactionMessageActionIndex;

		private static Dictionary<string, int> securityMessageTraceIdentifierIndex;

		private static Dictionary<string, int> reliableMessageTraceIdentifierIndex;

		private static Dictionary<string, int> transactionMessageTraceIdentifierIndex;

		private static object FilterEngineLock => filterEngineLock;

		public static FilterCriteria CurrentFilterCriteria
		{
			get
			{
				lock (FilterEngineLock)
				{
					return currentFilterCriteria;
				}
			}
			set
			{
				lock (FilterEngineLock)
				{
					currentFilterCriteria = value;
				}
			}
		}

		static FilterEngine()
		{
			filterEngineLock = new object();
			currentFilterCriteria = null;
			searchOptionNames = new Dictionary<string, SearchOptions>();
			traceLevelNames = new Dictionary<string, SourceLevels>();
			filterOptionSettings = null;
			securityMessageActionIndex = new Dictionary<string, int>();
			reliableMessageActionIndex = new Dictionary<string, int>();
			transactionMessageActionIndex = new Dictionary<string, int>();
			securityMessageTraceIdentifierIndex = new Dictionary<string, int>();
			reliableMessageTraceIdentifierIndex = new Dictionary<string, int>();
			transactionMessageTraceIdentifierIndex = new Dictionary<string, int>();
			searchOptionNames.Add(SR.GetString("AppFilterItem1"), SearchOptions.None);
			searchOptionNames.Add(SR.GetString("AppFilterItem2"), SearchOptions.EventID);
			searchOptionNames.Add(SR.GetString("AppFilterItem3"), SearchOptions.SourceName);
			searchOptionNames.Add(SR.GetString("AppFilterItem4"), SearchOptions.ProcessName);
			searchOptionNames.Add(SR.GetString("AppFilterItem5"), SearchOptions.TraceCode);
			searchOptionNames.Add(SR.GetString("AppFilterItem6"), SearchOptions.Description);
			searchOptionNames.Add(SR.GetString("AppFilterItem7"), SearchOptions.StartTime);
			searchOptionNames.Add(SR.GetString("AppFilterItem8"), SearchOptions.StopTime);
			searchOptionNames.Add(SR.GetString("AppFilterItem9"), SearchOptions.TimeRange);
			searchOptionNames.Add(SR.GetString("AppFilterItem10"), SearchOptions.EndpointAddress);
			searchOptionNames.Add(SR.GetString("AppFilterItem17"), SearchOptions.AppDataSection);
			searchOptionNames.Add(SR.GetString("AppFilterItem18"), SearchOptions.EntireRawData);
			searchOptionNames.Add(SR.GetString("AppFilterItem11"), SearchOptions.CustomFilter);
			traceLevelNames.Add(SR.GetString("AppFilterItem12"), SourceLevels.All);
			traceLevelNames.Add(SR.GetString("AppFilterItem13"), SourceLevels.Critical);
			traceLevelNames.Add(SR.GetString("AppFilterItem14"), SourceLevels.Error);
			traceLevelNames.Add(SR.GetString("AppFilterItem15"), SourceLevels.Warning);
			traceLevelNames.Add(SR.GetString("AppFilterItem16"), SourceLevels.Information);
			securityMessageActionIndex.Add("http://schemas.xmlsoap.org/ws/2005/02/trust", 0);
			securityMessageActionIndex.Add("http://schemas.xmlsoap.org/ws/2004/04/trust", 0);
			securityMessageActionIndex.Add("http://schemas.xmlsoap.org/ws/2005/02/sc", 0);
			securityMessageActionIndex.Add("http://schemas.xmlsoap.org/ws/2004/04/sc", 0);
			securityMessageActionIndex.Add("http://schemas.microsoft.com/ws/2004/04/addressingidentityextension", 0);
			securityMessageActionIndex.Add("http://schemas.microsoft.com/mb/2004/01/security", 0);
			reliableMessageActionIndex.Add("http://schemas.xmlsoap.org/ws/2005/02/rm", 0);
			reliableMessageActionIndex.Add("http://schemas.microsoft.com/net/2005/02/rm", 0);
			transactionMessageActionIndex.Add("http://schemas.microsoft.com/ws/2006/02/transactions", 0);
			transactionMessageActionIndex.Add("http://schemas.xmlsoap.org/ws/2004/10/wscoor", 0);
			transactionMessageActionIndex.Add("http://schemas.xmlsoap.org/ws/2004/10/wsat", 0);
			securityMessageTraceIdentifierIndex.Add("System.ServiceModel.Security", 0);
			securityMessageTraceIdentifierIndex.Add("System.IdentityModel", 0);
			reliableMessageTraceIdentifierIndex.Add("System.ServiceModel.Channels.RM", 0);
			transactionMessageTraceIdentifierIndex.Add("Microsoft.Transactions.TransactionBridge", 0);
			transactionMessageTraceIdentifierIndex.Add("http://msdn.microsoft.com/en-US/library/System.ServiceModel.Transactions.", 0);
		}

		public static void SetCustomFilterOptionSettingsReference(CustomFilterOptionSettings filterOptions)
		{
			filterOptionSettings = filterOptions;
		}

		public static SearchOptions ParseSearchOption(string name)
		{
            SearchOptions value;
			if (!searchOptionNames.TryGetValue(name, out value))
			{
				return SearchOptions.None;
			}
			return value;
		}

		public static SourceLevels ParseLevel(string name)
		{
            SourceLevels value;
			if (!traceLevelNames.TryGetValue(name, out value))
			{
				return SourceLevels.All;
			}
			return value;
		}

		private static bool CheckActionForTrace(string action, Dictionary<string, int> actionIndex)
		{
			if (!string.IsNullOrEmpty(action) && actionIndex != null)
			{
				foreach (string key in actionIndex.Keys)
				{
					if (action.StartsWith(key, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool CheckTraceIdentifierForTrace(string traceIdentifier, Dictionary<string, int> traceIdentifierIndex)
		{
			if (!string.IsNullOrEmpty(traceIdentifier) && traceIdentifierIndex != null)
			{
				foreach (string key in traceIdentifierIndex.Keys)
				{
					if (traceIdentifier.Contains(key))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool MatchFilter(SourceLevels level, SearchOptions searchOption, object searchCondition, TraceRecord trace)
		{
			if (filterOptionSettings != null && filterOptionSettings.IsSet)
			{
				if (!filterOptionSettings.ShowWCFTraces && IsWCFTraceRecord(trace))
				{
					return false;
				}
				if (IsWCFTraceRecord(trace))
				{
					if (!filterOptionSettings.ShowTransfer && IsTraceRecordTransfer(trace))
					{
						return false;
					}
					if (!filterOptionSettings.ShowMessageSentReceived && (trace.IsMessageSentRecord || trace.IsMessageReceivedRecord))
					{
						return false;
					}
					if (!filterOptionSettings.ShowSecurityMessage)
					{
						if (CheckActionForTrace(trace.Action, securityMessageActionIndex))
						{
							return false;
						}
						if (CheckTraceIdentifierForTrace(trace.TraceCode, securityMessageTraceIdentifierIndex))
						{
							return false;
						}
					}
					if (!filterOptionSettings.ShowReliableMessage)
					{
						if (CheckActionForTrace(trace.Action, reliableMessageActionIndex))
						{
							return false;
						}
						if (CheckTraceIdentifierForTrace(trace.TraceCode, reliableMessageTraceIdentifierIndex))
						{
							return false;
						}
					}
					if (!filterOptionSettings.ShowTransactionMessage)
					{
						if (CheckActionForTrace(trace.Action, transactionMessageActionIndex))
						{
							return false;
						}
						if (CheckTraceIdentifierForTrace(trace.TraceCode, transactionMessageTraceIdentifierIndex))
						{
							return false;
						}
						if (!string.IsNullOrEmpty(trace.Xml) && (trace.Xml.Contains("<CoordinationType>http://schemas.xmlsoap.org/ws/2004/10/wsat</CoordinationType>") || trace.Xml.Contains("<wscoor:Identifier xmlns:wscoor=\"http://schemas.xmlsoap.org/ws/2004/10/wscoor\">")))
						{
							return false;
						}
					}
				}
			}
			if (level == SourceLevels.All || trace.Level == (TraceEventType)0 || trace.IsMessageLogged || ((int)trace.Level & (int)(level + 65280)) > 0)
			{
				switch (searchOption)
				{
				case SearchOptions.None:
					return true;
				case SearchOptions.ProcessName:
				{
					string text = (string)searchCondition;
					CultureInfo currentCulture = CultureInfo.CurrentCulture;
					if (trace.ProcessName == null)
					{
						return string.IsNullOrEmpty(text);
					}
					return trace.ProcessName.ToUpper(currentCulture).Contains(text.ToUpper(currentCulture));
				}
				case SearchOptions.Description:
				{
					string text6 = (string)searchCondition;
					CultureInfo currentCulture5 = CultureInfo.CurrentCulture;
					if (trace.Description == null)
					{
						return string.IsNullOrEmpty(text6);
					}
					return trace.Description.ToUpper(currentCulture5).Contains(text6.ToUpper(currentCulture5));
				}
				case SearchOptions.EndpointAddress:
				{
					string text5 = (string)searchCondition;
					CultureInfo currentCulture4 = CultureInfo.CurrentCulture;
					if (trace.EndpointAddress == null)
					{
						return string.IsNullOrEmpty(text5);
					}
					return trace.EndpointAddress.ToUpper(currentCulture4).Contains(text5.ToUpper(currentCulture4));
				}
				case SearchOptions.EventID:
				{
					string text2 = (string)searchCondition;
					if (trace.EventID == null)
					{
						return string.IsNullOrEmpty(text2);
					}
					return trace.EventID == text2;
				}
				case SearchOptions.SourceName:
				{
					string text4 = (string)searchCondition;
					CultureInfo currentCulture3 = CultureInfo.CurrentCulture;
					if (trace.SourceName == null)
					{
						return string.IsNullOrEmpty(text4);
					}
					return trace.SourceName.ToUpper(currentCulture3) == text4.ToUpper(currentCulture3);
				}
				case SearchOptions.TraceCode:
				{
					string text3 = (string)searchCondition;
					CultureInfo currentCulture2 = CultureInfo.CurrentCulture;
					if (trace.TraceCode == null)
					{
						return string.IsNullOrEmpty(text3);
					}
					return trace.TraceCode.ToUpper(currentCulture2).Contains(text3.ToUpper(currentCulture2));
				}
				case SearchOptions.StartTime:
				{
					DateTime t2 = (DateTime)searchCondition;
					return trace.Time >= t2;
				}
				case SearchOptions.StopTime:
				{
					DateTime t = (DateTime)searchCondition;
					return trace.Time <= t;
				}
				case SearchOptions.TimeRange:
				{
					DateTimePair dateTimePair = (DateTimePair)searchCondition;
					if (trace.Time >= dateTimePair.StartTime)
					{
						return trace.Time <= dateTimePair.EndTime;
					}
					return false;
				}
				case SearchOptions.AppDataSection:
				{
					string xml3 = trace.Xml;
					string value2 = (string)searchCondition;
					if (!string.IsNullOrEmpty(xml3) && !string.IsNullOrEmpty(value2))
					{
						try
						{
							XmlDocument xmlDocument3 = new XmlDocument();
							xmlDocument3.LoadXml(xml3);
							if (xmlDocument3.DocumentElement != null && xmlDocument3.DocumentElement.HasChildNodes)
							{
								XmlElement xmlElement = xmlDocument3.DocumentElement["ApplicationData"];
								if (xmlElement != null)
								{
									string outerXml2 = xmlElement.OuterXml;
									if (!string.IsNullOrEmpty(outerXml2))
									{
										return outerXml2.IndexOf(value2, StringComparison.OrdinalIgnoreCase) != -1;
									}
								}
							}
						}
						catch (XmlException)
						{
						}
					}
					return false;
				}
				case SearchOptions.EntireRawData:
				{
					string xml2 = trace.Xml;
					string value = (string)searchCondition;
					if (!string.IsNullOrEmpty(xml2) && !string.IsNullOrEmpty(value))
					{
						try
						{
							XmlDocument xmlDocument2 = new XmlDocument();
							xmlDocument2.LoadXml(xml2);
							if (xmlDocument2.DocumentElement != null)
							{
								string outerXml = xmlDocument2.DocumentElement.OuterXml;
								if (!string.IsNullOrEmpty(outerXml))
								{
									return outerXml.IndexOf(value, StringComparison.OrdinalIgnoreCase) != -1;
								}
							}
						}
						catch (XmlException)
						{
						}
					}
					return false;
				}
				case SearchOptions.CustomFilter:
				{
					string xml = trace.Xml;
					if (string.IsNullOrEmpty(xml))
					{
						return false;
					}
					try
					{
						CustomFilterCondition customFilterCondition = (CustomFilterCondition)searchCondition;
						XmlDocument xmlDocument = new XmlDocument();
						xmlDocument.LoadXml(xml);
						if (customFilterCondition.customFilter.Namespaces.Count == 0)
						{
							try
							{
								if (xmlDocument.SelectSingleNode(customFilterCondition.customFilter.ContainsParameters ? customFilterCondition.parsedXPathExpress : customFilterCondition.customFilter.Expression) != null)
								{
									return true;
								}
							}
							catch (XPathException)
							{
								return false;
							}
							catch (XsltException)
							{
								return false;
							}
						}
						else
						{
							XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
							foreach (string key in customFilterCondition.customFilter.Namespaces.Keys)
							{
								xmlNamespaceManager.AddNamespace(key, customFilterCondition.customFilter.Namespaces[key]);
							}
							try
							{
								if (xmlDocument.SelectSingleNode(customFilterCondition.customFilter.ContainsParameters ? customFilterCondition.parsedXPathExpress : customFilterCondition.customFilter.Expression, xmlNamespaceManager) != null)
								{
									return true;
								}
							}
							catch (XPathException)
							{
								return false;
							}
							catch (XsltException)
							{
								return false;
							}
						}
					}
					catch (XmlException)
					{
						return false;
					}
					return false;
				}
				default:
					return false;
				}
			}
			return false;
		}

		private static bool IsWCFTraceRecord(TraceRecord trace)
		{
			return trace.SourceName.StartsWith("System.ServiceModel", StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsTraceRecordTransfer(TraceRecord trace)
		{
			if (!trace.IsTransfer && !trace.IsActivityBoundary && trace.Level != TraceEventType.Resume)
			{
				return trace.Level == TraceEventType.Suspend;
			}
			return true;
		}

		public static bool MatchFilter(FilterCriteria fc, TraceRecord trace)
		{
			if (fc != null)
			{
				return MatchFilter(fc.FilterSourceLevel, fc.SearchOption, fc.searchCondition, trace);
			}
			return MatchFilter(SourceLevels.All, SearchOptions.None, null, trace);
		}

		public static bool MatchFilter(TraceRecord trace)
		{
			FilterCriteria fc = null;
			lock (FilterEngineLock)
			{
				if (currentFilterCriteria == null && filterOptionSettings != null && !filterOptionSettings.IsSet)
				{
					return true;
				}
				fc = currentFilterCriteria;
			}
			return MatchFilter(fc, trace);
		}
	}
}
