using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class FileConverter
	{
		private class InternalCrimsonToE2eConvertingParameter : IDisposable
		{
			private string convertedFilename;

			private FileConverterException exception;

			private bool isDisposed;

			private bool isFinished;

			private int percentage;

			private string sourceFileName;

			private object thisLock = new object();

			private AutoResetEvent traceConvertedEvent = new AutoResetEvent(initialState: false);

			public string ConvertedFilename
			{
				get
				{
					return convertedFilename;
				}
				set
				{
					convertedFilename = value;
				}
			}

			public FileConverterException Exception
			{
				get
				{
					lock (ThisLock)
					{
						return exception;
					}
				}
				set
				{
					lock (ThisLock)
					{
						exception = value;
					}
				}
			}

			public bool IsFinished
			{
				get
				{
					lock (ThisLock)
					{
						return isFinished;
					}
				}
				set
				{
					lock (ThisLock)
					{
						isFinished = value;
					}
				}
			}

			public int Percentage
			{
				get
				{
					lock (ThisLock)
					{
						return percentage;
					}
				}
				set
				{
					lock (ThisLock)
					{
						percentage = value;
					}
				}
			}

			public string SourceFileName
			{
				get
				{
					return sourceFileName;
				}
				set
				{
					sourceFileName = value;
				}
			}

			public AutoResetEvent TraceConvertedEvent => traceConvertedEvent;

			private object ThisLock => thisLock;

			~InternalCrimsonToE2eConvertingParameter()
			{
				Dispose();
			}

			public void Dispose()
			{
				lock (ThisLock)
				{
					if (!isDisposed && traceConvertedEvent != null)
					{
						traceConvertedEvent.Close();
						GC.SuppressFinalize(this);
						isDisposed = true;
					}
				}
			}
		}

		private class InternalTargetTraceRecord
		{
			internal string activityID;

			internal string channel;

			internal string computerName;

			internal DateTime createdDateTime = DateTime.MinValue;

			internal string description;

			internal Dictionary<string, string> eventDataSections = new Dictionary<string, string>();

			internal string eventID;

			internal string guid;

			internal bool isTraceRecord;

			internal TraceEventType level = TraceEventType.Information;

			internal string processID;

			internal string recordText;

			internal string relatedActivityID;

			internal StringBuilder sbUndefinedXmlInSystemXmlElement = new StringBuilder();

			internal StringBuilder sbXmlExt = new StringBuilder();

			internal string sourceName;

			internal string threadID;

			internal string userSid;
		}

		private class InternalTargetTraceRecordComparer : IComparer<InternalTargetTraceRecord>
		{
			int IComparer<InternalTargetTraceRecord>.Compare(InternalTargetTraceRecord x, InternalTargetTraceRecord y)
			{
				if (x != null && y != null)
				{
					if (x.createdDateTime == y.createdDateTime)
					{
						return 0;
					}
					if (x.createdDateTime < y.createdDateTime)
					{
						return -1;
					}
					return 1;
				}
				return 1;
			}
		}

		private const int DETECT_SCHEMA_FILE_HEADER_SIZE = 8192;

		private const int FILE_BATCH_READ_BUFFER_LENGTH = 1024;

		private static string crimsonSchemaTag = "<Event ";

		private static string e2eSchemaTag = "<E2ETraceEvent ";

		private IErrorReport errorReport;

		private IProgressReport progressReport;

		public FileConverter(IErrorReport errorReport, IProgressReport progressReport)
		{
			this.errorReport = errorReport;
			this.progressReport = progressReport;
		}

		public void ConvertFileToE2ESchema(string sourceFileName, string convertedFilename, SupportedFileFormat sourceSchema)
		{
			if (!string.IsNullOrEmpty(sourceFileName) && !string.IsNullOrEmpty(convertedFilename))
			{
				switch (sourceSchema)
				{
				case SupportedFileFormat.EtlBinary:
					EtlToE2e(sourceFileName, convertedFilename);
					break;
				case SupportedFileFormat.CrimsonSchema:
					CrimsonToE2e(sourceFileName, convertedFilename);
					break;
				}
			}
		}

		internal static SupportedFileFormat DetectFileSchema(string filePath)
		{
			if (!string.IsNullOrEmpty(filePath))
			{
				if (filePath.EndsWith(SR.GetString("MainFrm_FileEtl"), StringComparison.OrdinalIgnoreCase))
				{
					return SupportedFileFormat.EtlBinary;
				}
				if (filePath.EndsWith(SR.GetString("PJ_Extension"), StringComparison.OrdinalIgnoreCase))
				{
					return SupportedFileFormat.STVProjectFile;
				}
				FileStream fileStream = null;
				char[] array = new char[8192];
				try
				{
					fileStream = Utilities.CreateFileStreamHelper(filePath);
					Utilities.SeekFileStreamHelper(fileStream, 0L, SeekOrigin.Begin);
					Utilities.CreateStreamReaderHelper(fileStream).Read(array, 0, array.Length);
				}
				catch (IOException)
				{
					return SupportedFileFormat.UnknownFormat;
				}
				finally
				{
					Utilities.CloseStreamWithoutException(fileStream, isFlushStream: false);
				}
				string text = new string(array);
				text = text.Trim(' ', '\0', '\t', '\n');
				if (string.IsNullOrEmpty(text))
				{
					return SupportedFileFormat.E2ETraceEventSchema;
				}
				int num = text.IndexOf(crimsonSchemaTag, StringComparison.Ordinal);
				int num2 = text.IndexOf(e2eSchemaTag, StringComparison.Ordinal);
				if (num != -1 && num2 == -1)
				{
					return SupportedFileFormat.CrimsonSchema;
				}
				if (num == -1 && num2 != -1)
				{
					return SupportedFileFormat.E2ETraceEventSchema;
				}
				if (num != -1 && num2 == -1 && num < num2)
				{
					return SupportedFileFormat.CrimsonSchema;
				}
				if (num != -1 && num2 == -1 && num > num2)
				{
					return SupportedFileFormat.E2ETraceEventSchema;
				}
			}
			return SupportedFileFormat.NotSupported;
		}

		private void CrimsonToE2e(string sourceFileName, string convertedFilename)
		{
			int num = 0;
			using (InternalCrimsonToE2eConvertingParameter internalCrimsonToE2eConvertingParameter = new InternalCrimsonToE2eConvertingParameter())
			{
				internalCrimsonToE2eConvertingParameter.SourceFileName = sourceFileName;
				internalCrimsonToE2eConvertingParameter.ConvertedFilename = convertedFilename;
				progressReport.Begin(100);
				if (!ThreadPool.QueueUserWorkItem(CrimsonToE2eThreadProc, internalCrimsonToE2eConvertingParameter))
				{
				}
				while (!internalCrimsonToE2eConvertingParameter.IsFinished)
				{
					internalCrimsonToE2eConvertingParameter.TraceConvertedEvent.WaitOne();
					if (internalCrimsonToE2eConvertingParameter.Percentage != num && internalCrimsonToE2eConvertingParameter.Percentage > num)
					{
						for (int i = 0; i < internalCrimsonToE2eConvertingParameter.Percentage - num; i++)
						{
							progressReport.Step();
						}
						num = internalCrimsonToE2eConvertingParameter.Percentage;
					}
				}
				progressReport.Complete();
				if (internalCrimsonToE2eConvertingParameter.Exception != null)
				{
					throw internalCrimsonToE2eConvertingParameter.Exception;
				}
			}
		}

		private void CrimsonToE2eThreadProc(object o)
		{
			if (o != null && o is InternalCrimsonToE2eConvertingParameter)
			{
				InternalCrimsonToE2eConvertingParameter internalCrimsonToE2eConvertingParameter = (InternalCrimsonToE2eConvertingParameter)o;
				if (!string.IsNullOrEmpty(internalCrimsonToE2eConvertingParameter.SourceFileName) && !string.IsNullOrEmpty(internalCrimsonToE2eConvertingParameter.ConvertedFilename))
				{
					FileStream fileStream = null;
					FileStream fileStream2 = null;
					try
					{
						fileStream = Utilities.CreateFileStreamHelper(internalCrimsonToE2eConvertingParameter.SourceFileName);
						if (fileStream.Length > 0)
						{
							List<InternalTargetTraceRecord> list = new List<InternalTargetTraceRecord>();
							fileStream2 = Utilities.CreateFileStreamHelper(internalCrimsonToE2eConvertingParameter.ConvertedFilename, FileMode.Create, FileAccess.Write, FileShare.Read);
							XmlTextReader xmlTextReader = new XmlTextReader(fileStream, XmlNodeType.Element, null);
							XmlWriter xmlWriter = new XmlTextWriter(fileStream2, Encoding.UTF8);
							xmlTextReader.WhitespaceHandling = WhitespaceHandling.None;
							xmlTextReader.MoveToContent();
							while (InternalReadToXml(xmlTextReader, "Event"))
							{
								string text = xmlTextReader.ReadOuterXml();
								if (!string.IsNullOrEmpty(text))
								{
									InternalTargetTraceRecord item = InternalConstructTargetTraceRecordXml(text);
									list.Add(item);
								}
								internalCrimsonToE2eConvertingParameter.Percentage = (int)((double)fileStream.Position / (double)fileStream.Length);
								internalCrimsonToE2eConvertingParameter.TraceConvertedEvent.Set();
							}
							OutputCrimsonToE2EResults(list, xmlWriter);
							xmlWriter.Flush();
						}
					}
					catch (LogFileException ex)
					{
						InternalCrimsonToE2eConvertingParameter internalCrimsonToE2eConvertingParameter2 = internalCrimsonToE2eConvertingParameter;
						internalCrimsonToE2eConvertingParameter2.Exception = new FileConverterException(internalCrimsonToE2eConvertingParameter2.SourceFileName, internalCrimsonToE2eConvertingParameter.ConvertedFilename, ex.Message, ex);
					}
					catch (ArgumentException e)
					{
						InternalCrimsonToE2eConvertingParameter internalCrimsonToE2eConvertingParameter3 = internalCrimsonToE2eConvertingParameter;
						internalCrimsonToE2eConvertingParameter3.Exception = new FileConverterException(internalCrimsonToE2eConvertingParameter3.SourceFileName, internalCrimsonToE2eConvertingParameter.ConvertedFilename, SR.GetString("MsgCannotWriteToFile") + internalCrimsonToE2eConvertingParameter.ConvertedFilename + SR.GetString("MsgCannotWriteToFileEnd"), e);
					}
					catch (XmlException ex2)
					{
						errorReport.ReportErrorToUser(new FileConverterException(internalCrimsonToE2eConvertingParameter.SourceFileName, internalCrimsonToE2eConvertingParameter.ConvertedFilename, SR.GetString("MsgErrorOccursOnConvertCrimson") + ex2.Message + SR.GetString("MsgCannotWriteToFileEnd"), ex2));
					}
					finally
					{
						Utilities.CloseStreamWithoutException(fileStream, isFlushStream: false);
						Utilities.CloseStreamWithoutException(fileStream2, isFlushStream: false);
						internalCrimsonToE2eConvertingParameter.IsFinished = true;
						internalCrimsonToE2eConvertingParameter.TraceConvertedEvent.Set();
					}
				}
			}
		}

		private bool InternalReadToXml(XmlReader reader, string tagName)
		{
			if (reader != null && !string.IsNullOrEmpty(tagName))
			{
				while ((!(reader.Name == tagName) || reader.NodeType != XmlNodeType.Element) && reader.Read())
				{
				}
				if (reader.Name == tagName && reader.NodeType == XmlNodeType.Element)
				{
					return true;
				}
			}
			return false;
		}

		private void OutputCrimsonToE2EResults(List<InternalTargetTraceRecord> internalList, XmlWriter xmlWriter)
		{
			if (internalList != null && xmlWriter != null)
			{
				internalList.Sort(new InternalTargetTraceRecordComparer());
				foreach (InternalTargetTraceRecord @internal in internalList)
				{
					InternalWriterTargetTraceRecord(@internal, xmlWriter);
				}
			}
		}

		private void InternalWriterTargetTraceRecord(InternalTargetTraceRecord trace, XmlWriter writer)
		{
			if (trace != null && writer != null)
			{
				try
				{
					writer.WriteStartElement("E2ETraceEvent", "http://schemas.microsoft.com/2004/06/E2ETraceEvent");
					writer.WriteStartElement("System", "http://schemas.microsoft.com/2004/06/windows/eventlog/system");
					writer.WriteStartElement("EventID");
					writer.WriteString((!string.IsNullOrEmpty(trace.eventID)) ? trace.eventID : "0");
					writer.WriteEndElement();
					writer.WriteStartElement("Type");
					writer.WriteString("3");
					writer.WriteEndElement();
					writer.WriteStartElement("SubType");
					writer.WriteAttributeString("Name", trace.level.ToString());
					writer.WriteEndElement();
					writer.WriteStartElement("Level");
					int level = (int)trace.level;
					writer.WriteString(level.ToString(CultureInfo.CurrentCulture));
					writer.WriteEndElement();
					writer.WriteStartElement("TimeCreated");
					writer.WriteAttributeString("SystemTime", trace.createdDateTime.ToString(CultureInfo.CurrentUICulture));
					writer.WriteEndElement();
					writer.WriteStartElement("Source");
					writer.WriteAttributeString("Name", trace.sourceName);
					if (!string.IsNullOrEmpty(trace.guid))
					{
						writer.WriteAttributeString("Id", trace.guid);
					}
					writer.WriteEndElement();
					writer.WriteStartElement("Correlation");
					writer.WriteAttributeString("ActivityID", trace.activityID);
					if (!string.IsNullOrEmpty(trace.relatedActivityID))
					{
						writer.WriteAttributeString("RelatedActivityID", trace.relatedActivityID);
					}
					writer.WriteEndElement();
					writer.WriteStartElement("Execution");
					writer.WriteAttributeString("ProcessID", trace.processID);
					writer.WriteAttributeString("ThreadID", trace.threadID);
					writer.WriteEndElement();
					writer.WriteStartElement("Channel");
					writer.WriteString(trace.channel);
					writer.WriteEndElement();
					writer.WriteStartElement("Computer");
					writer.WriteString(trace.computerName);
					writer.WriteEndElement();
					writer.WriteStartElement("Security");
					if (!string.IsNullOrEmpty(trace.userSid))
					{
						writer.WriteAttributeString("UserSid", trace.userSid);
					}
					writer.WriteEndElement();
					if (trace.sbUndefinedXmlInSystemXmlElement.Length != 0)
					{
						try
						{
							writer.WriteRaw(trace.sbUndefinedXmlInSystemXmlElement.ToString());
						}
						catch (Exception e)
						{
							ExceptionManager.GeneralExceptionFilter(e);
						}
					}
					writer.WriteEndElement();
					writer.WriteStartElement("ApplicationData");
					writer.WriteStartElement("TraceData");
					writer.WriteStartElement("DataItem");
					if (trace.isTraceRecord)
					{
						writer.WriteRaw(trace.recordText);
					}
					else
					{
						writer.WriteStartElement("TraceRecord", "http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord");
						writer.WriteAttributeString("Severity", trace.level.ToString());
						writer.WriteStartElement("TraceIdentifier");
						writer.WriteString("");
						writer.WriteEndElement();
						writer.WriteStartElement("Description");
						writer.WriteString((!string.IsNullOrEmpty(trace.description)) ? trace.description : trace.sourceName);
						writer.WriteEndElement();
						if (trace.eventDataSections.Count != 0 || trace.sbXmlExt.Length != 0)
						{
							writer.WriteStartElement("ExtendedData");
							if (trace.eventDataSections.Count != 0)
							{
								foreach (string key in trace.eventDataSections.Keys)
								{
									writer.WriteStartElement(key);
									writer.WriteString(trace.eventDataSections[key]);
									writer.WriteEndElement();
								}
							}
							if (trace.sbXmlExt.Length != 0)
							{
								try
								{
									writer.WriteRaw(trace.sbXmlExt.ToString());
								}
								catch (Exception e2)
								{
									ExceptionManager.GeneralExceptionFilter(e2);
								}
							}
							writer.WriteEndElement();
						}
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
					writer.WriteEndElement();
					writer.WriteEndElement();
					writer.WriteEndElement();
				}
				catch (InvalidOperationException)
				{
				}
				catch (ArgumentException)
				{
				}
			}
		}

		private void AppendXmlElementToStringBuilder(StringBuilder sb, XmlElement elem)
		{
			if (sb != null && elem != null)
			{
				sb.Append(elem.OuterXml);
			}
		}

		private bool ContainsUserDataAndTraceRecord(InternalTargetTraceRecord trace, XmlElement elem)
		{
			if (elem.Name == "System.Diagnostics.UserData")
			{
				foreach (XmlElement childNode in elem.ChildNodes)
				{
					if (childNode.Name == "Data" && childNode.InnerXml.Trim().StartsWith("&lt;TraceRecord", StringComparison.Ordinal))
					{
						trace.isTraceRecord = true;
						trace.recordText = childNode.InnerText;
						return true;
					}
				}
			}
			return false;
		}

		private InternalTargetTraceRecord InternalConstructTargetTraceRecordXml(string xml)
		{
			try
			{
				if (!string.IsNullOrEmpty(xml))
				{
					InternalTargetTraceRecord internalTargetTraceRecord = new InternalTargetTraceRecord();
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.LoadXml(xml);
					XmlElement documentElement = xmlDocument.DocumentElement;
					if (documentElement != null && documentElement.Name == "Event")
					{
						foreach (XmlElement childNode in documentElement.ChildNodes)
						{
							if (childNode.Name == "System")
							{
								foreach (XmlElement childNode2 in childNode.ChildNodes)
								{
									SetupInternalTargetTraceRecordInSystemXmlElement(childNode2, internalTargetTraceRecord);
								}
							}
							else if (childNode.Name == "EventData")
							{
								foreach (XmlElement childNode3 in childNode.ChildNodes)
								{
									SetupInternalTargetTraceRecordInEventDataXmlElement(childNode3, internalTargetTraceRecord);
								}
							}
							else if (childNode.Name == "RenderingInfo")
							{
								foreach (XmlElement childNode4 in childNode.ChildNodes)
								{
									SetupInternalTargetTraceRecordInRenderingInfoXmlElement(childNode4, internalTargetTraceRecord);
								}
								internalTargetTraceRecord.sbXmlExt.Append(childNode.OuterXml);
							}
							else if (childNode.Name == "UserData")
							{
								foreach (XmlElement childNode5 in childNode.ChildNodes)
								{
									if (ContainsUserDataAndTraceRecord(internalTargetTraceRecord, childNode5))
									{
										break;
									}
								}
								SetupInternalTargetTraceRecordInOthersXmlElement(childNode, internalTargetTraceRecord);
							}
							else
							{
								SetupInternalTargetTraceRecordInOthersXmlElement(childNode, internalTargetTraceRecord);
							}
						}
					}
					return internalTargetTraceRecord;
				}
			}
			catch (XmlException)
			{
			}
			return null;
		}

		private void SetupInternalTargetTraceRecordInEventDataXmlElement(XmlElement element, InternalTargetTraceRecord trace)
		{
			if (element != null && trace != null && element.Name == "Data" && element.HasAttribute("name"))
			{
				if (element.Attributes["name"].Value == "Error")
				{
					trace.level = TraceEventType.Error;
				}
				if (!string.IsNullOrEmpty(element.Attributes["name"].Value))
				{
					if (!trace.eventDataSections.ContainsKey(element.Attributes["name"].Value))
					{
						trace.eventDataSections.Add(element.Attributes["name"].Value, element.InnerText);
					}
					else
					{
						trace.eventDataSections[element.Attributes["name"].Value] = element.InnerText;
					}
				}
			}
		}

		private void SetupInternalTargetTraceRecordInOthersXmlElement(XmlElement element, InternalTargetTraceRecord trace)
		{
			if (element != null)
			{
				trace?.sbXmlExt.Append(element.OuterXml);
			}
		}

		private void SetupInternalTargetTraceRecordInRenderingInfoXmlElement(XmlElement element, InternalTargetTraceRecord trace)
		{
			if (element != null && trace != null)
			{
				string name = element.Name;
				if (!(name == "Message"))
				{
					if (name == "Level" && !string.IsNullOrEmpty(element.InnerText))
					{
						TraceEventType traceEventType = TraceEventTypeFromString(element.InnerText);
						if (traceEventType != 0)
						{
							trace.level = traceEventType;
						}
					}
				}
				else if (!string.IsNullOrEmpty(element.InnerText))
				{
					trace.description = element.InnerText;
				}
			}
		}

		private void SetupInternalTargetTraceRecordInSystemXmlElement(XmlElement element, InternalTargetTraceRecord trace)
		{
			if (element != null && trace != null)
			{
				switch (element.Name)
				{
				case "EventID":
					trace.eventID = element.InnerText;
					break;
				case "Provider":
					if (element.HasAttribute("Name"))
					{
						trace.sourceName = element.Attributes["Name"].Value;
					}
					if (element.HasAttribute("Guid"))
					{
						trace.guid = element.Attributes["Guid"].Value;
					}
					AppendXmlElementToStringBuilder(trace.sbUndefinedXmlInSystemXmlElement, element);
					break;
				case "Level":
				{
					byte result = 0;
					if (byte.TryParse(element.InnerText, out result))
					{
						trace.level = MofUtils.ConvertByteToTraceEventType(result);
					}
					else
					{
						trace.level = (TraceEventType)0;
					}
					break;
				}
				case "TimeCreated":
					if (element.HasAttribute("SystemTime") && !string.IsNullOrEmpty(element.Attributes["SystemTime"].Value))
					{
						try
						{
							trace.createdDateTime = DateTime.Parse(element.Attributes["SystemTime"].Value, CultureInfo.CurrentUICulture);
						}
						catch (FormatException)
						{
							trace.createdDateTime = DateTime.MinValue;
						}
						catch (ArgumentNullException)
						{
							trace.createdDateTime = DateTime.MinValue;
						}
					}
					break;
				case "Correlation":
					if (element.HasAttribute("ActivityID"))
					{
						trace.activityID = element.Attributes["ActivityID"].Value;
						if (element.HasAttribute("RelatedActivityID"))
						{
							trace.relatedActivityID = element.Attributes["RelatedActivityID"].Value;
						}
					}
					break;
				case "Execution":
					if (element.HasAttribute("ProcessID"))
					{
						trace.processID = element.Attributes["ProcessID"].Value;
					}
					if (element.HasAttribute("ThreadID"))
					{
						trace.threadID = element.Attributes["ThreadID"].Value;
					}
					break;
				case "Channel":
					trace.channel = element.InnerText;
					break;
				case "Computer":
					trace.computerName = element.InnerText;
					break;
				case "Security":
					if (element.HasAttribute("UserID"))
					{
						trace.userSid = element.Attributes["UserID"].Value;
					}
					break;
				case "Task":
				case "OpCode":
				case "Keywords":
				case "EventRecordID":
					AppendXmlElementToStringBuilder(trace.sbUndefinedXmlInSystemXmlElement, element);
					break;
				}
			}
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

		private void EtlToE2e(string sourceFileName, string convertedFilename)
		{
			string text = null;
			int num = 0;
			if (!string.IsNullOrEmpty(convertedFilename))
			{
				TracesCollection tracesCollection = null;
				try
				{
					tracesCollection = new TracesCollection(sourceFileName, errorReport);
				}
				catch (FileNotFoundException e)
				{
					text = SR.GetString("MsgCannotFindFile") + sourceFileName;
					throw new FileConverterException(sourceFileName, convertedFilename, text, e);
				}
				catch (LogFileException e2)
				{
					text = SR.GetString("MsgCannotOpenFile") + sourceFileName + SR.GetString("MsgFilePathEnd");
					throw new FileConverterException(sourceFileName, convertedFilename, text, e2);
				}
				DateTime startTime = tracesCollection.StartTime;
				XmlTextWriter xmlTextWriter = null;
				try
				{
					xmlTextWriter = new XmlTextWriter(convertedFilename, Encoding.UTF8);
				}
				catch (UnauthorizedAccessException e3)
				{
					text = SR.GetString("MsgCannotOpenFileReturn") + convertedFilename + SR.GetString("MsgReturnBack") + SR.GetString("MsgAccessDenied");
					throw new FileConverterException(sourceFileName, convertedFilename, text, e3);
				}
				catch (SecurityException e4)
				{
					text = SR.GetString("MsgCannotOpenFileReturn") + convertedFilename + SR.GetString("MsgAccessDenied");
					throw new FileConverterException(sourceFileName, convertedFilename, text, e4);
				}
				xmlTextWriter.Formatting = Formatting.Indented;
				int num2 = 100;
				int num3 = 0;
				progressReport.Begin(num2);
				foreach (TraceEntry item in tracesCollection)
				{
					if (item != null)
					{
						if (!item.IsErrorTrace)
						{
							try
							{
								xmlTextWriter.WriteRaw(item.Xml);
							}
							catch (Exception e5)
							{
								ExceptionManager.GeneralExceptionFilter(e5);
								num++;
							}
						}
						else
						{
							num++;
						}
						progressReport.Step();
						if (++num3 > num2)
						{
							progressReport.Begin(num2);
						}
					}
				}
				xmlTextWriter.Flush();
				xmlTextWriter.Close();
				if (num != 0)
				{
					errorReport.ReportErrorToUser(new FileConverterException(sourceFileName, convertedFilename, SR.GetString("MsgFileConvertTraceErr"), null));
				}
			}
		}
	}
}
