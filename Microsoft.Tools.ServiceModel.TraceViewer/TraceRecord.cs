using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceRecord
	{
		private bool isActivityName;

		private bool isData = true;

		private DateTime dateTime;

		private string xml;

		private string endpointAddress;

		private string eventID;

		private string sourceName;

		private TraceEventType level;

		private string description;

		private int threadId;

		private string processName;

		private int processId = -1;

		private string time;

		private string traceCode;

		private string activityID;

		private string activityName;

		private string funcName;

		private string computerName;

		private string messageActivityID;

		private bool actionParsed;

		private bool toParsed;

		private string action;

		private string to;

		private ActivityType activityType = ActivityType.UnknownActivity;

		private string relatedActivityID;

		private bool isMessageLogged;

		private bool isFunctionRelated;

		private CallingDirection callingDirection;

		private long traceID;

		private TraceRecordPosition tracePos;

		private string messageID;

		private string relatesToMessageID;

		private string messageCorrelationID;

		private MessageProperty messageProperties = MessageProperty.Unknown;

		private ExecutionInfo executionInfo;

		private TraceDataSource dataSource;

		private const int MAX_TRACE_RECORD_SIZE = 40960;

		private const int MAX_DESCRIPTION_STRING_LENGTH = 255;

		internal ActivityType ActivityType
		{
			get
			{
				return activityType;
			}
			set
			{
				activityType = value;
			}
		}

		internal TraceDataSource DataSource => dataSource;

		public ExecutionInfo Execution
		{
			get
			{
				if (executionInfo != null)
				{
					return executionInfo;
				}
				executionInfo = new ExecutionInfo(computerName, ProcessName, ThreadId.ToString(CultureInfo.InvariantCulture), processId);
				return executionInfo;
			}
		}

		public bool IsMessageSentRecord
		{
			get
			{
				if (!string.IsNullOrEmpty(TraceCode))
				{
					if (TraceCode.EndsWith("MessageSent.aspx", StringComparison.Ordinal))
					{
						return true;
					}
					if (TraceCode.EndsWith("Sent", StringComparison.Ordinal))
					{
						return true;
					}
				}
				return false;
			}
		}

		public bool IsMessageReceivedRecord
		{
			get
			{
				if (!string.IsNullOrEmpty(TraceCode))
				{
					if (TraceCode.EndsWith("MessageReceived.aspx", StringComparison.Ordinal))
					{
						return true;
					}
					if (TraceCode.EndsWith("Received", StringComparison.Ordinal))
					{
						return true;
					}
				}
				return false;
			}
		}

		public TraceEventType Level => level;

		public int ThreadId => threadId;

		public bool IsActivityBoundary
		{
			get
			{
				if (Level != TraceEventType.Start)
				{
					return Level == TraceEventType.Stop;
				}
				return true;
			}
		}

		public FileDescriptor FileDescriptor => TraceRecordPos.RelatedFileDescriptor;

		public CallingDirection CallingDirection
		{
			get
			{
				return callingDirection;
			}
			set
			{
				callingDirection = value;
			}
		}

		public long TraceID
		{
			get
			{
				return traceID;
			}
			set
			{
				traceID = value;
			}
		}

		public string FunctionName
		{
			set
			{
				funcName = value;
				isFunctionRelated = true;
			}
		}

		public bool IsFunctionRelated => isFunctionRelated;

		public TraceRecordPosition TraceRecordPos
		{
			get
			{
				return tracePos;
			}
			set
			{
				tracePos = value;
			}
		}

		public string ActivityID
		{
			get
			{
				return activityID;
			}
			set
			{
				activityID = NormalizeActivityId(value);
			}
		}

		public string Description
		{
			get
			{
				bool isTransfer = IsTransfer;
				return description;
			}
			set
			{
				description = value;
			}
		}

		public string ProcessName
		{
			get
			{
				return processName;
			}
			set
			{
				processName = value;
			}
		}

		public string TimeString
		{
			get
			{
				return time;
			}
			set
			{
				time = value;
			}
		}

		public string TraceCode
		{
			get
			{
				return traceCode;
			}
			set
			{
				traceCode = value.Trim();
			}
		}

		public string To
		{
			get
			{
				if (IsMessageLogged)
				{
					if (toParsed)
					{
						return to;
					}
					XmlElement xmlElement = null;
					xmlElement = GetInternalXmlElementInMessageHeader(Xml, "To");
					if (xmlElement != null && !string.IsNullOrEmpty(xmlElement.InnerText))
					{
						to = xmlElement.InnerText;
						toParsed = true;
						return to;
					}
					xmlElement = GetInternalXmlElementInMessageHeader(Xml, "a:To");
					if (xmlElement != null && !string.IsNullOrEmpty(xmlElement.InnerText))
					{
						to = xmlElement.InnerText;
						toParsed = true;
						return to;
					}
					xmlElement = GetInternalXmlElementInMessageHeader(Xml, "ReplyTo");
					if (xmlElement != null && !string.IsNullOrEmpty(xmlElement.InnerText))
					{
						to = xmlElement.InnerText;
						toParsed = true;
						return to;
					}
					xmlElement = GetInternalXmlElementInMessageHeader(Xml, "a:ReplyTo");
					if (xmlElement != null && !string.IsNullOrEmpty(xmlElement.InnerText))
					{
						to = xmlElement.InnerText;
						toParsed = true;
						return to;
					}
					xmlElement = GetInternalXmlElementInApplicationData(Xml, "MessageLogTraceRecord");
					if (xmlElement != null && xmlElement["Addressing"] != null && xmlElement["Addressing"]["To"] != null && !string.IsNullOrEmpty(xmlElement["Addressing"]["To"].InnerText))
					{
						to = xmlElement["Addressing"]["To"].InnerText;
						toParsed = true;
						return to;
					}
					if (xmlElement != null && xmlElement["Addressing"] != null && xmlElement["Addressing"]["ReplyTo"] != null && !string.IsNullOrEmpty(xmlElement["Addressing"]["ReplyTo"].InnerText))
					{
						to = xmlElement["Addressing"]["ReplyTo"].InnerText;
						toParsed = true;
						return to;
					}
				}
				toParsed = true;
				return null;
			}
		}

		public string Action
		{
			get
			{
				if (IsMessageLogged || IsMessageReceivedRecord || IsMessageSentRecord)
				{
					if (actionParsed)
					{
						return action;
					}
					XmlElement xmlElement = null;
					xmlElement = GetInternalXmlElementInMessageHeader(Xml, "Action");
					if (xmlElement == null)
					{
						xmlElement = GetInternalXmlElementInMessageHeader(Xml, "a:Action");
					}
					if (xmlElement != null && !string.IsNullOrEmpty(xmlElement.InnerText))
					{
						action = xmlElement.InnerText;
						actionParsed = true;
						return action;
					}
					xmlElement = GetInternalXmlElementInApplicationData(Xml, "MessageLogTraceRecord");
					if (xmlElement != null && xmlElement["Addressing"] != null && xmlElement["Addressing"]["Action"] != null && !string.IsNullOrEmpty(xmlElement["Addressing"]["Action"].InnerText))
					{
						action = xmlElement["Addressing"]["Action"].InnerText;
						actionParsed = true;
						return action;
					}
				}
				actionParsed = true;
				return null;
			}
		}

		public string MessageID
		{
			get
			{
				if (!string.IsNullOrEmpty(messageID))
				{
					return messageID;
				}
				string value = GetMessageCorrelationID();
				if (!string.IsNullOrEmpty(value))
				{
					messageID = value;
					return messageID;
				}
				string text = Xml;
				if (!string.IsNullOrEmpty(text))
				{
					XmlElement xmlElement = null;
					if (IsMessageSentRecord)
					{
						xmlElement = GetInternalXmlElementInMessageHeader(text, "MessageID");
					}
					else if (IsMessageLogged || IsMessageReceivedRecord)
					{
						xmlElement = GetInternalXmlElementInMessageHeader(text, "a:MessageID");
					}
					if (xmlElement != null && !string.IsNullOrEmpty(xmlElement.InnerText))
					{
						messageID = xmlElement.InnerText;
						return messageID;
					}
				}
				return null;
			}
		}

		public string RelatesToMessageID
		{
			get
			{
				if (!string.IsNullOrEmpty(relatesToMessageID))
				{
					return relatesToMessageID;
				}
				string text = Xml;
				if (!string.IsNullOrEmpty(text))
				{
					XmlElement xmlElement = null;
					if (IsMessageSentRecord)
					{
						xmlElement = GetInternalXmlElementInMessageHeader(text, "RelatesTo");
					}
					else if (IsMessageLogged || IsMessageReceivedRecord)
					{
						xmlElement = GetInternalXmlElementInMessageHeader(text, "a:RelatesTo");
					}
					if (xmlElement != null && !string.IsNullOrEmpty(xmlElement.InnerText))
					{
						relatesToMessageID = xmlElement.InnerText;
						return relatesToMessageID;
					}
				}
				return null;
			}
		}

		public string MessageActivityID
		{
			get
			{
				if (!string.IsNullOrEmpty(relatesToMessageID))
				{
					return relatesToMessageID;
				}
				string text = Xml;
				if (!string.IsNullOrEmpty(text))
				{
					XmlElement internalXmlElementInMessageHeader = GetInternalXmlElementInMessageHeader(text, "ActivityId");
					if (internalXmlElementInMessageHeader != null && !string.IsNullOrEmpty(internalXmlElementInMessageHeader.InnerText))
					{
						messageActivityID = internalXmlElementInMessageHeader.InnerText;
						return messageActivityID;
					}
				}
				messageActivityID = NormalizeActivityId(ActivityID);
				return messageActivityID;
			}
		}

		public string ActivityName
		{
			get
			{
				return activityName;
			}
			set
			{
				isActivityName = true;
				activityName = value;
			}
		}

		public bool IsActivityName => isActivityName;

		public bool IsData
		{
			get
			{
				if (!IsTransfer)
				{
					return isData;
				}
				return false;
			}
		}

		public bool IsTransfer
		{
			get
			{
				if (RelatedActivityID != null)
				{
					return RelatedActivityID != string.Empty;
				}
				return false;
			}
		}

		public string RelatedActivityID
		{
			get
			{
				return relatedActivityID;
			}
			set
			{
				relatedActivityID = NormalizeActivityId(value);
			}
		}

		public DateTime Time => dateTime;

		public string Xml
		{
			get
			{
				if (string.IsNullOrEmpty(xml) && !string.IsNullOrEmpty(TraceRecordPos.RelatedFileDescriptor.FilePath))
				{
					FileStream fileStream = null;
					try
					{
						fileStream = Utilities.CreateFileStreamHelper(TraceRecordPos.RelatedFileDescriptor.FilePath);
						Utilities.SeekFileStreamHelper(fileStream, TraceRecordPos.FileOffset, SeekOrigin.Begin);
						XmlReader xmlReader = new XmlTextReader(fileStream, XmlNodeType.Element, null);
						xmlReader.MoveToContent();
						if (xmlReader.Name == "E2ETraceEvent" && xmlReader.NodeType == XmlNodeType.Element)
						{
							xml = xmlReader.ReadOuterXml();
						}
						else
						{
							xml = null;
						}
					}
					catch (XmlException)
					{
						xml = null;
					}
					catch (LogFileException)
					{
						xml = null;
					}
					finally
					{
						Utilities.CloseStreamWithoutException(fileStream, isFlushStream: false);
					}
				}
				return xml;
			}
		}

		public string EndpointAddress => endpointAddress;

		public string EventID => eventID;

		public string SourceName => sourceName;

		public bool IsMessageLogged => isMessageLogged;

		internal MessageProperty MessageProperties => messageProperties;

		public bool FindTraceRecord(FindCriteria findCriteria)
		{
			if (findCriteria != null && !string.IsNullOrEmpty(findCriteria.FindingText))
			{
				try
				{
					if (findCriteria.Target == FindingTarget.RawData)
					{
						string text = Xml;
						if (!string.IsNullOrEmpty(text))
						{
							if ((findCriteria.Options & FindingOptions.MatchWholeWord) > FindingOptions.None)
							{
								return findCriteria.WholeWordRegex.Match(text).Success;
							}
							if ((findCriteria.Options & FindingOptions.MatchCase) > FindingOptions.None)
							{
								return text.Contains(findCriteria.FindingText);
							}
							return text.ToLower(CultureInfo.CurrentCulture).Contains(findCriteria.FindingText.ToLower(CultureInfo.CurrentCulture));
						}
						return false;
					}
					if (findCriteria.Target == FindingTarget.XmlTagAttribute)
					{
						return InternalFindXmlTag(findCriteria);
					}
					if (findCriteria.Target == FindingTarget.XmlTagValue)
					{
						return InternalFindXmlTag(findCriteria);
					}
					if (findCriteria.Target == FindingTarget.LoggedMessage)
					{
						return InternalFindLoggedMessage(findCriteria);
					}
				}
				catch (XmlException)
				{
					return false;
				}
			}
			return false;
		}

		internal TraceRecord(TraceDataSource dataSource)
		{
			xml = null;
			this.dataSource = dataSource;
		}

		public static string NormalizeActivityId(string activityId)
		{
			string result = activityId;
			if (!string.IsNullOrEmpty(activityId))
			{
				result = activityId.Trim(' ', '"', '{', '}').ToLowerInvariant();
				result = "{" + result + "}";
			}
			return result;
		}

		private bool InternalFindXmlTag(FindCriteria findCriteria)
		{
			if (findCriteria != null && !string.IsNullOrEmpty(findCriteria.FindingText))
			{
				XmlTextReader xmlTextReader = null;
				string text = Xml;
				if (!string.IsNullOrEmpty(text))
				{
					try
					{
						xmlTextReader = new XmlTextReader(text, XmlNodeType.Element, null);
						while (xmlTextReader.Read())
						{
							if (xmlTextReader.NodeType == XmlNodeType.Text && findCriteria.Target == FindingTarget.XmlTagValue)
							{
								string value = xmlTextReader.Value;
								if ((findCriteria.Options & FindingOptions.MatchWholeWord) > FindingOptions.None)
								{
									if (findCriteria.WholeWordRegex.Match(value).Success)
									{
										return true;
									}
								}
								else if ((findCriteria.Options & FindingOptions.MatchCase) > FindingOptions.None)
								{
									if (value.Contains(findCriteria.FindingText))
									{
										return true;
									}
								}
								else if (value.ToLower(CultureInfo.CurrentCulture).Contains(findCriteria.FindingText.ToLower(CultureInfo.CurrentCulture)))
								{
									return true;
								}
							}
							if (xmlTextReader.NodeType == XmlNodeType.Element && findCriteria.Target == FindingTarget.XmlTagAttribute)
							{
								for (int i = 0; i < xmlTextReader.AttributeCount; i++)
								{
									string text2 = xmlTextReader[i];
									if ((findCriteria.Options & FindingOptions.MatchWholeWord) > FindingOptions.None)
									{
										if (findCriteria.WholeWordRegex.Match(text2).Success)
										{
											return true;
										}
									}
									else if ((findCriteria.Options & FindingOptions.MatchCase) > FindingOptions.None)
									{
										if (text2.Contains(findCriteria.FindingText))
										{
											return true;
										}
									}
									else if (text2.ToLower(CultureInfo.CurrentCulture).Contains(findCriteria.FindingText.ToLower(CultureInfo.CurrentCulture)))
									{
										return true;
									}
								}
							}
						}
					}
					catch (XmlException)
					{
						return false;
					}
					finally
					{
						xmlTextReader?.Close();
					}
				}
			}
			return false;
		}

		private bool InternalFindLoggedMessage(FindCriteria findCriteria)
		{
			if (findCriteria != null && findCriteria.Target == FindingTarget.LoggedMessage && !string.IsNullOrEmpty(findCriteria.FindingText))
			{
				if (!IsMessageLogged)
				{
					return false;
				}
				bool flag = false;
				string loggedMessageString = GetLoggedMessageString(out flag);
				if (string.IsNullOrEmpty(loggedMessageString))
				{
					return false;
				}
				if ((findCriteria.Options & FindingOptions.MatchWholeWord) > FindingOptions.None)
				{
					return findCriteria.WholeWordRegex.Match(loggedMessageString).Success;
				}
				if ((findCriteria.Options & FindingOptions.MatchCase) > FindingOptions.None)
				{
					return loggedMessageString.Contains(findCriteria.FindingText);
				}
				return loggedMessageString.ToLower(CultureInfo.CurrentCulture).Contains(findCriteria.FindingText.ToLower(CultureInfo.CurrentCulture));
			}
			return false;
		}

		internal static TraceRecord GetTraceRecordFromPosition(TraceRecordPosition pos)
		{
			if (pos != null)
			{
				FileStream cachedFileStream = pos.RelatedFileDescriptor.GetCachedFileStream();
				long foundTraceFileOffset = 0L;
				cachedFileStream.Seek(pos.FileOffset, SeekOrigin.Begin);
				return pos.RelatedFileDescriptor.GetFirstValidTrace(cachedFileStream, pos.FileOffset, out foundTraceFileOffset);
			}
			return null;
		}

		private XmlElement GetInternalXmlElementInApplicationData(string rawXml, string nodeName)
		{
			if (!string.IsNullOrEmpty(rawXml) && !string.IsNullOrEmpty(nodeName))
			{
				try
				{
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.LoadXml(rawXml);
					XmlElement xmlElement = xmlDocument.DocumentElement["ApplicationData"];
					if (xmlElement != null)
					{
						if (xmlElement[nodeName] != null)
						{
							return xmlElement[nodeName];
						}
						XmlElement xmlElement2 = xmlElement["TraceRecord"];
						if (xmlElement2 == null && xmlElement["TraceData"] != null)
						{
							XmlElement xmlElement3 = xmlElement["TraceData"]["DataItem"];
							if (xmlElement3 != null)
							{
								if (xmlElement3[nodeName] != null)
								{
									return xmlElement3[nodeName];
								}
								xmlElement2 = xmlElement3["TraceRecord"];
							}
						}
						if (xmlElement2 != null && xmlElement2[nodeName] != null)
						{
							return xmlElement2[nodeName];
						}
					}
				}
				catch (XmlException)
				{
				}
			}
			return null;
		}

		private XmlElement GetInternalXmlElementInMessageHeader(string rawXml, string nodeName)
		{
			if (IsMessageSentRecord || IsMessageReceivedRecord)
			{
				XmlElement internalXmlElementInApplicationData = GetInternalXmlElementInApplicationData(rawXml, "ExtendedData");
				if (internalXmlElementInApplicationData != null)
				{
					XmlElement xmlElement = internalXmlElementInApplicationData["MessageHeaders"];
					if (xmlElement != null && xmlElement[nodeName] != null)
					{
						return xmlElement[nodeName];
					}
				}
			}
			else if (IsMessageLogged)
			{
				XmlElement internalXmlElementInApplicationData2 = GetInternalXmlElementInApplicationData(rawXml, "MessageLogTraceRecord");
				if (internalXmlElementInApplicationData2 != null)
				{
					XmlElement xmlElement2 = internalXmlElementInApplicationData2["s:Envelope"];
					if (xmlElement2 != null && xmlElement2["s:Header"] != null && xmlElement2["s:Header"][nodeName] != null)
					{
						return xmlElement2["s:Header"][nodeName];
					}
				}
			}
			return null;
		}

		private string GetMessageCorrelationID()
		{
			if (!string.IsNullOrEmpty(messageCorrelationID))
			{
				return messageCorrelationID;
			}
			string text = Xml;
			if (!string.IsNullOrEmpty(text))
			{
				XmlElement xmlElement = null;
				xmlElement = GetInternalXmlElementInMessageHeader(text, "ActivityId");
				if (xmlElement != null && xmlElement.HasAttributes)
				{
					if (xmlElement.HasAttribute("CorrelationId"))
					{
						messageCorrelationID = xmlElement.Attributes["CorrelationId"].Value;
					}
					else if (xmlElement.HasAttribute("HeaderId"))
					{
						messageCorrelationID = xmlElement.Attributes["HeaderId"].Value;
					}
					return messageCorrelationID;
				}
			}
			return null;
		}

		public string TryAndGetXmlString()
		{
			FileStream fileStream = null;
			try
			{
				fileStream = Utilities.CreateFileStreamHelper(TraceRecordPos.RelatedFileDescriptor.FilePath);
				Utilities.SeekFileStreamHelper(fileStream, TraceRecordPos.FileOffset, SeekOrigin.Begin);
				StreamReader streamReader = Utilities.CreateStreamReaderHelper(fileStream);
				char[] array = new char[40960];
				streamReader.Read(array, 0, 40960);
				string text = new string(array);
				if (!string.IsNullOrEmpty(text))
				{
					text = text.Trim('\0', ' ');
					if (text.StartsWith("", StringComparison.OrdinalIgnoreCase))
					{
						int num = text.IndexOf("</E2ETraceEvent>", StringComparison.OrdinalIgnoreCase);
						int num2 = text.IndexOf("<E2ETraceEvent", "<E2ETraceEvent".Length, StringComparison.OrdinalIgnoreCase);
						if (num == -1 && num2 == -1)
						{
							return text;
						}
						if (num == -1 && num2 != -1)
						{
							return text.Substring(0, num2);
						}
						if (num != -1 && num2 == -1)
						{
							return text.Substring(0, num + "</E2ETraceEvent>".Length);
						}
						if (num < num2)
						{
							return text.Substring(0, num + "</E2ETraceEvent>".Length);
						}
						return text.Substring(0, num2);
					}
				}
				return null;
			}
			catch (LogFileException)
			{
				return null;
			}
			finally
			{
				Utilities.CloseStreamWithoutException(fileStream, isFlushStream: false);
			}
		}

		public string GetLoggedMessageString(out bool xml)
		{
			xml = false;
			if (!IsMessageLogged)
			{
				return string.Empty;
			}
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				string value = Xml;
				if (!string.IsNullOrEmpty(value))
				{
					xmlDocument.LoadXml(value);
					XmlElement xmlElement = xmlDocument.DocumentElement["ApplicationData"];
					if (xmlElement != null)
					{
						XmlElement xmlElement2 = xmlElement["MessageLogTraceRecord"];
						if (xmlElement2 != null)
						{
							xml = true;
							return xmlElement2.InnerXml.Trim();
						}
						XmlElement xmlElement3 = xmlElement["TraceData"];
						if (xmlElement3 != null)
						{
							XmlElement xmlElement4 = xmlElement3["DataItem"];
							if (xmlElement4 != null)
							{
								XmlElement xmlElement5 = xmlElement4["MessageLogTraceRecord"];
								if (xmlElement5 != null)
								{
									xml = true;
									return xmlElement5.InnerXml.Trim();
								}
							}
						}
					}
				}
			}
			catch (XmlException)
			{
			}
			return null;
		}

		internal void ReadFrom(XmlTextReader reader, XmlUtils xmlUtils)
		{
			ReadFrom((XmlReader)reader, xmlUtils);
		}

		private MessageProperty ExtraceMessageProperties(string sourceName)
		{
			if (!string.IsNullOrEmpty(sourceName))
			{
				switch (sourceName)
				{
				case "ServiceLevelRequestIn":
					return MessageProperty.MessageIn;
				case "ServiceLevelRequestOut":
					return MessageProperty.MessageOut;
				case "ServiceLevelReplyIn":
					return MessageProperty.MessageIn;
				case "ServiceLevelReplyOut":
					return MessageProperty.MessageOut;
				case "TransportWrite":
					return MessageProperty.MessageOut;
				case "TransportRead":
					return MessageProperty.MessageIn;
				case "ServiceLevelDatagramOut":
					return MessageProperty.MessageIn;
				case "ServiceLevelDatagramIn":
					return MessageProperty.MessageIn;
				}
			}
			return MessageProperty.Unknown;
		}

		internal void ReadFrom(XmlReader reader, XmlUtils xmlUtils)
		{
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				reader.Read();
				xmlUtils.SkipTo(reader, "System");
				reader.Read();
				xmlUtils.SkipTo(reader, "EventID");
				reader.Read();
				eventID = reader.Value;
				xmlUtils.SkipTo(reader, "Type");
				reader.Skip();
				if (reader.Name == "SubType" && reader.HasAttributes)
				{
					string attribute = reader.GetAttribute("Name");
					if (!string.IsNullOrEmpty(attribute))
					{
						TraceEventType traceEventType = TraceEventTypeFromString(attribute);
						if (traceEventType != 0)
						{
							level = traceEventType;
						}
					}
				}
				if (reader.Name == "SubType")
				{
					reader.Skip();
				}
				if (reader.Name == "Level")
				{
					reader.Read();
					if (reader.Value.Length > 0)
					{
						try
						{
							TraceEventType traceEventType2 = (TraceEventType)int.Parse(reader.Value, CultureInfo.InvariantCulture);
							if (traceEventType2 != (TraceEventType)255)
							{
								level = traceEventType2;
							}
						}
						catch (FormatException e)
						{
							throw new E2EInvalidFileException(SR.GetString("MsgLevelFormatErr"), string.Empty, e, -1L);
						}
						catch (OverflowException e2)
						{
							throw new E2EInvalidFileException(SR.GetString("MsgLevelFormatErr"), string.Empty, e2, -1L);
						}
					}
				}
				xmlUtils.SkipTo(reader, "TimeCreated");
				TimeString = reader["SystemTime"];
				if (TimeString != null)
				{
					try
					{
						dateTime = DateTime.Parse(TimeString, CultureInfo.CurrentCulture);
					}
					catch (FormatException e3)
					{
						throw new E2EInvalidFileException(SR.GetString("MsgTimeFormatErr"), string.Empty, e3, -1L);
					}
				}
				xmlUtils.SkipTo(reader, "Source");
				string text = sourceName = reader["Name"];
				xmlUtils.SkipTo(reader, "Correlation");
				ActivityID = reader["ActivityID"];
				RelatedActivityID = reader["RelatedActivityID"];
				if (IsTransfer)
				{
					TraceCode = SR.GetString("TR_TraceTransfer");
					Description = Activity.ShortID(ActivityID) + SR.GetString("TR_Arrow") + Activity.ShortID(RelatedActivityID);
					level = TraceEventType.Information;
				}
				if (ActivityID == null)
				{
					ActivityID = Guid.Empty.ToString("B", CultureInfo.CurrentCulture);
				}
				xmlUtils.SkipTo(reader, "Execution");
				try
				{
					processId = int.Parse(reader["ProcessID"], CultureInfo.CurrentUICulture);
				}
				catch (FormatException)
				{
					processId = -1;
				}
				catch (OverflowException)
				{
					processId = -1;
				}
				ProcessName = reader["ProcessName"];
				if (ProcessName == null || ProcessName.Trim() == string.Empty)
				{
					ProcessName = processId.ToString(CultureInfo.CurrentUICulture);
				}
				if (!string.IsNullOrEmpty(reader["ThreadID"]))
				{
					try
					{
						threadId = int.Parse(reader["ThreadID"], CultureInfo.InvariantCulture);
					}
					catch (FormatException)
					{
						threadId = 0;
					}
					catch (OverflowException)
					{
						threadId = 0;
					}
				}
				else
				{
					threadId = 0;
				}
				xmlUtils.SkipTo(reader, "Computer");
				reader.Read();
				computerName = (string.IsNullOrEmpty(reader.Value) ? string.Empty : reader.Value);
				xmlUtils.SkipTo(reader, "ApplicationData");
				reader.Read();
				if (string.IsNullOrEmpty(reader.Name) && reader.NodeType == XmlNodeType.Text && !string.IsNullOrEmpty(reader.Value))
				{
					if (reader.Value.Length >= 255)
					{
						Description = reader.Value.Substring(0, 255) + SR.GetString("MoreDataTag");
					}
					else
					{
						Description = reader.Value;
					}
					reader.Read();
				}
				string text2;
				int num;
				switch (reader.Name)
				{
				case "MessageLogTraceRecord":
					isMessageLogged = true;
					messageProperties = ExtraceMessageProperties(reader.GetAttribute("Source"));
					ExtractMessageLogProperties(reader, xmlUtils);
					break;
				case "TraceData":
				case "TraceRecord":
				{
					if (reader.Name.Equals("TraceData", StringComparison.Ordinal))
					{
						xmlUtils.SkipTo(reader, "DataItem");
						reader.Read();
						if (reader.Name.Equals("MessageLogTraceRecord", StringComparison.Ordinal))
						{
							isMessageLogged = true;
							messageProperties = ExtraceMessageProperties(reader.GetAttribute("Source"));
							ExtractMessageLogProperties(reader, xmlUtils);
							break;
						}
						if (!xmlUtils.SkipToChild(reader, "TraceRecord") || !reader.Name.Equals("TraceRecord", StringComparison.Ordinal))
						{
							break;
						}
					}
					string value = reader["Severity"];
					if (!string.IsNullOrEmpty(value))
					{
						try
						{
							level = (TraceEventType)Enum.Parse(typeof(TraceEventType), value);
						}
						catch (ArgumentException e4)
						{
							throw new E2EInvalidFileException(SR.GetString("MsgLevelFormatErr"), string.Empty, e4, -1L);
						}
					}
					if (xmlUtils.SkipToEither(reader, "TraceCode", "TraceIdentifier"))
					{
						reader.Read();
						TraceCode = reader.Value;
					}
					if (xmlUtils.SkipTo(reader, "Description", throwExceptionOnEnd: false))
					{
						reader.Read();
						Description = reader.Value;
						reader.Skip();
						reader.ReadEndElement();
					}
					if (reader.Name == "EndpointReference" && xmlUtils.SkipToChild(reader, "Address"))
					{
						reader.Read();
						endpointAddress = reader.Value;
					}
					break;
				}
				case "ConnID":
					reader.Skip();
					if (!(reader.Name == "ContextId"))
					{
						break;
					}
					goto case "ContextId";
				case "ActivityID":
					if (!xmlUtils.SkipToSibling(reader, "TraceRecord"))
					{
						break;
					}
					goto case "TraceData";
				case "ContextId":
					reader.Read();
					ActivityID = reader.Value;
					break;
				case "RequestObj":
					reader.Skip();
					if (!(reader.Name == "RequestId"))
					{
						break;
					}
					goto case "RequestId";
				case "RequestId":
					reader.Read();
					text2 = reader.Value;
					if (!string.IsNullOrEmpty(text2))
					{
						text2 = NormalizeActivityId(text2);
						try
						{
							ActivityID = NormalizeActivityId(new Guid(text2).ToString("B", CultureInfo.CurrentCulture));
						}
						catch (FormatException)
						{
							goto IL_077a;
						}
						break;
					}
					goto IL_07bc;
				case "System.Diagnostics":
					{
						if (xmlUtils.SkipToChild(reader, "Message"))
						{
							reader.Read();
							if (reader.Name == "TraceRecord")
							{
								if (xmlUtils.SkipToChild(reader, "TraceCode"))
								{
									reader.Read();
									TraceCode = reader.Value;
								}
								if (xmlUtils.SkipTo(reader, "Description", throwExceptionOnEnd: false))
								{
									reader.Read();
									Description = reader.Value;
								}
							}
						}
						if (Level == (TraceEventType)0 && xmlUtils.SkipTo(reader, "Severity", throwExceptionOnEnd: false))
						{
							reader.Read();
							level = TraceEventTypeFromString(reader.Value);
						}
						break;
					}
					IL_077a:
					try
					{
						if (ulong.Parse(text2, CultureInfo.CurrentCulture) != 0L)
						{
							ActivityID = NormalizeActivityId(xmlUtils.GuidFromIntString(text2).ToString("B", CultureInfo.CurrentCulture));
							break;
						}
					}
					catch (FormatException)
					{
					}
					catch (OverflowException)
					{
					}
					goto IL_07bc;
					IL_07bc:
					num = reader.Depth - 1;
					while (reader.Read() && reader.Depth >= num)
					{
						if (reader.Name == "connID" && reader.Read())
						{
							string value2 = reader.Value;
							if (!string.IsNullOrEmpty(value2))
							{
								ActivityID = xmlUtils.GuidFromIntString(value2).ToString("B", CultureInfo.CurrentCulture);
							}
							break;
						}
					}
					break;
				}
				if (Description == null)
				{
					if (xmlUtils.SkipToChild(reader, "DisplayName"))
					{
						reader.Read();
						Description = reader.Value;
						if (Description == null || Description.Trim() == string.Empty)
						{
							Description = text;
						}
					}
					else
					{
						Description = text;
					}
				}
				if (TraceCode != null)
				{
					if (TraceCode.EndsWith("MethodEntered", StringComparison.Ordinal) || TraceCode.EndsWith("MethodExited", StringComparison.Ordinal))
					{
						if (xmlUtils.SkipTo(reader, "ExtendedData", throwExceptionOnEnd: false) && xmlUtils.SkipToChild(reader, "MethodName"))
						{
							reader.Read();
							Description = Description + SR.GetString("TR_LeftQu") + reader.Value + SR.GetString("TR_RightQu");
						}
					}
					else if (TraceCode.EndsWith("ActivityId/Set", StringComparison.Ordinal) || TraceCode.EndsWith("ActivityId/IssuedNew", StringComparison.Ordinal))
					{
						isData = false;
					}
					else
					{
						while (reader.Name != "ExtendedData" && (!(reader.Name == "E2ETraceEvent") || reader.NodeType != XmlNodeType.EndElement) && reader.Read())
						{
						}
						if (reader.Name == "ExtendedData")
						{
							string text3 = reader["xmlns"];
							if (!string.IsNullOrEmpty(text3))
							{
								int num2 = text3.LastIndexOf('/');
								if (num2 != -1)
								{
									string text4 = text3.Substring(num2 + 1);
									if (text4.Contains("DictionaryTraceRecord"))
									{
										if (reader.Read() && reader.Name == "ActivityName")
										{
											reader.Read();
											ActivityName = reader.Value;
											if (Level == TraceEventType.Start && reader.Read() && reader.Name == "ActivityName" && reader.NodeType == XmlNodeType.EndElement && reader.Read() && reader.Name == "ActivityType" && reader.Read())
											{
												ActivityType = ParseActivityTypeFromString(reader.Value);
											}
										}
									}
									else if (text4.Contains("StringTraceRecord") && reader.Read())
									{
										if (reader.Name == "ActivityName")
										{
											reader.Read();
											ActivityName = reader.Value;
											if (Level == TraceEventType.Start && reader.Read() && reader.Name == "ActivityName" && reader.NodeType == XmlNodeType.EndElement && reader.Read() && reader.Name == "ActivityType" && reader.Read())
											{
												ActivityType = ParseActivityTypeFromString(reader.Value);
											}
										}
										if (reader.Name == "BeginMethod")
										{
											reader.Read();
											FunctionName = reader.Value;
											CallingDirection = CallingDirection.CallIn;
										}
										else if (reader.Name == "EndMethod")
										{
											reader.Read();
											FunctionName = reader.Value;
											CallingDirection = CallingDirection.Return;
										}
									}
								}
							}
						}
					}
				}
				if (IsMessageLogged)
				{
					Description = SR.GetString("TR_MsgLogTrace");
					level = TraceEventType.Information;
					if (string.IsNullOrEmpty(traceCode))
					{
						traceCode = SR.GetString("TR_MsgLogTrace");
					}
				}
				if (IsTransfer)
				{
					level = TraceEventType.Transfer;
				}
				if (level != 0 && TraceEventTypeFromString(level.ToString()) == (TraceEventType)0)
				{
					throw new E2EInvalidFileException(SR.GetString("MsgLevelFormatErr"), string.Empty, null, -1L);
				}
				if (IsActivityBoundary && !isActivityName && !string.IsNullOrEmpty(Description))
				{
					ActivityName = Description;
				}
			}
			catch (XmlException ex8)
			{
				throw new E2EInvalidFileException(ex8.Message, string.Empty, ex8, -1L);
			}
			catch (Exception ex9)
			{
				ExceptionManager.GeneralExceptionFilter(ex9);
				throw new E2EInvalidFileException(ex9.Message, string.Empty, ex9, -1L);
			}
		}

		private void ExtractMessageLogProperties(XmlReader reader, XmlUtils utils)
		{
			if (utils.SkipToEither(reader, "s:Envelope", "Addressing"))
			{
				int num = 0;
				string name = reader.Name;
				if (!(name == "s:Envelope"))
				{
					if (name == "Addressing")
					{
						num = reader.Depth;
						while (!reader.EOF && reader.Read() && (!(reader.Name == "E2ETraceEvent") || reader.NodeType != XmlNodeType.EndElement) && reader.Depth > num)
						{
							if (reader.NodeType == XmlNodeType.Element)
							{
								switch (reader.Name)
								{
								case "Action":
									if (reader.Read() && !string.IsNullOrEmpty(reader.Value))
									{
										action = reader.Value;
									}
									break;
								case "To":
									if (reader.Read() && !string.IsNullOrEmpty(reader.Value))
									{
										to = reader.Value;
									}
									break;
								case "ReplyTo":
									if (reader.Read() && !string.IsNullOrEmpty(reader.Value))
									{
										to = reader.Value;
									}
									break;
								}
							}
						}
					}
				}
				else if (utils.SkipToChild(reader, "s:Header"))
				{
					num = reader.Depth;
					while (!reader.EOF && reader.Read() && (!(reader.Name == "E2ETraceEvent") || reader.NodeType != XmlNodeType.EndElement) && reader.Depth > num)
					{
						if (reader.NodeType == XmlNodeType.Element)
						{
							switch (reader.Name)
							{
							case "Action":
								if (reader.Read() && !string.IsNullOrEmpty(reader.Value))
								{
									action = reader.Value;
								}
								break;
							case "a:Action":
								if (reader.Read() && !string.IsNullOrEmpty(reader.Value))
								{
									action = reader.Value;
								}
								break;
							case "To":
								if (reader.Read() && !string.IsNullOrEmpty(reader.Value))
								{
									to = reader.Value;
								}
								break;
							case "a:To":
								if (reader.Read() && !string.IsNullOrEmpty(reader.Value))
								{
									to = reader.Value;
								}
								break;
							case "ReplyTo":
								if (reader.Read() && !string.IsNullOrEmpty(reader.Value))
								{
									to = reader.Value;
								}
								break;
							case "a:ReplyTo":
								if (reader.Read() && !string.IsNullOrEmpty(reader.Value))
								{
									to = reader.Value;
								}
								break;
							}
						}
					}
				}
			}
			actionParsed = true;
			toParsed = true;
		}

		private ActivityType ParseActivityTypeFromString(string typeName)
		{
			if (!string.IsNullOrEmpty(typeName))
			{
				switch (typeName)
				{
				case "Construct":
				case "Open":
				case "Close":
					return ActivityType.ServiceHostActivity;
				case "ListenAt":
					return ActivityType.ListenActivity;
				case "ReceiveBytes":
					return ActivityType.ConnectionActivity;
				case "ProcessMessage":
					return ActivityType.MessageActivity;
				case "ProcessAction":
				case "ExecuteUserCode":
					return ActivityType.UserCodeExecutionActivity;
				}
			}
			return ActivityType.NormalActivity;
		}

		private TraceEventType TraceEventTypeFromString(string value)
		{
			switch (value)
			{
			case "Verbose":
				return TraceEventType.Verbose;
			case "Critical":
				return TraceEventType.Critical;
			case "Error":
				return TraceEventType.Error;
			case "Information":
				return TraceEventType.Information;
			case "Warning":
				return TraceEventType.Warning;
			case "Start":
				return TraceEventType.Start;
			case "Stop":
				return TraceEventType.Stop;
			case "Transfer":
				return TraceEventType.Transfer;
			case "Suspend":
				return TraceEventType.Suspend;
			case "Resume":
				return TraceEventType.Resume;
			default:
				return (TraceEventType)0;
			}
		}
	}
}
