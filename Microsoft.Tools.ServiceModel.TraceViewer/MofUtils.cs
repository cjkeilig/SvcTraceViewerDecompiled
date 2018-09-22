using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal static class MofUtils
	{
		private static readonly string GuidNull = Guid.Empty.ToString("B");

		internal const string E2ETRACEEVENT_SCHEMA_URL = "http://schemas.microsoft.com/2004/06/E2ETraceEvent";

		internal const string WINDOWS_EVENTLOG_SYSTEM_SCHEMA_URL = "http://schemas.microsoft.com/2004/06/windows/eventlog/system";

		private static Dictionary<EventKey, EventSchema> mofCollection = new Dictionary<EventKey, EventSchema>();

		private static Dictionary<Guid, ManagementClass> GuidLookupTable = new Dictionary<Guid, ManagementClass>();

		public static EventSchema GetEventSchema(EventKey eventKey)
		{
            EventSchema value;

			if (mofCollection.TryGetValue(eventKey, out value))
			{
				return value;
			}
			if (GuidLookupTable.Count <= 0)
			{
				InitGuidLookupTable();
			}
            ManagementClass value2;

            if (GuidLookupTable.TryGetValue(eventKey.Guid, out value2))
			{
				return GetMofFromWbem(value2, eventKey);
			}
			return null;
		}

		private static void InitGuidLookupTable()
		{
			ManagementPath.DefaultPath = new ManagementPath("\\\\.\\root\\WMI");
			FindGuid(new ManagementClass("EventTrace"));
		}

		private static void FindGuid(ManagementClass managementClass)
		{
			QualifierDataCollection.QualifierDataEnumerator enumerator = managementClass.Qualifiers.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					QualifierData current = enumerator.Current;
					if (current.Name.Equals("Guid", StringComparison.Ordinal))
					{
						Guid key = new Guid((string)current.Value);
						if (GuidLookupTable.ContainsKey(key))
						{
							if (!managementClass.ClassPath.ToString().EndsWith("_V0", StringComparison.OrdinalIgnoreCase))
							{
								GuidLookupTable[key] = managementClass;
							}
						}
						else
						{
							GuidLookupTable.Add(key, managementClass);
						}
					}
				}
			}
			finally
			{
				(enumerator as IDisposable)?.Dispose();
			}
			foreach (ManagementClass subclass in managementClass.GetSubclasses())
			{
				FindGuid(subclass);
			}
		}

		private static EventSchema GetMofFromWbem(ManagementClass parent, EventKey eventKey)
		{
			if (parent == null)
			{
				return null;
			}
			EventSchema eventSchema = null;
			foreach (ManagementClass subclass in parent.GetSubclasses())
			{
				if (MatchEventType(subclass, eventKey.Type))
				{
					eventSchema = new EventSchema();
					eventSchema.ManagementClass = subclass;
					eventSchema.WmiDataTypes = GetMofTypes(subclass);
					mofCollection.Add(eventKey, eventSchema);
				}
				else
				{
					eventSchema = GetMofFromWbem(subclass, eventKey);
				}
				if (eventSchema != null)
				{
					return eventSchema;
				}
			}
			return eventSchema;
		}

		private static bool MatchEventType(ManagementClass ddmc, byte eventType)
		{
			bool result = false;
			object qualifierValue = ddmc.GetQualifierValue("EventType");
			if (qualifierValue != null && typeof(int) == qualifierValue.GetType())
			{
				result = (eventType == (int)qualifierValue);
			}
			else
			{
				int[] array = (int[])qualifierValue;
				foreach (int num in array)
				{
					if (eventType == num)
					{
						result = true;
						break;
					}
				}
			}
			return result;
		}

		private static WmiDataType GetTypeFromPropertyData(PropertyData propertyData)
		{
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool handleObjectAsGuid = false;
			string stringFormat = string.Empty;
			string stringTermination = string.Empty;
			int index = -1;
			QualifierDataCollection.QualifierDataEnumerator enumerator = propertyData.Qualifiers.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					QualifierData current = enumerator.Current;
					string name = current.Name;
					if (name.Equals("WmiDataId", StringComparison.Ordinal))
					{
						flag = true;
						index = (int)current.Value;
					}
					if (name.Equals("extension", StringComparison.Ordinal) && current.Value.Equals("Guid"))
					{
						handleObjectAsGuid = true;
					}
					if (name.Equals("ActivityID", StringComparison.Ordinal))
					{
						flag2 = true;
					}
					if (name.Equals("RelatedActivityID", StringComparison.Ordinal))
					{
						flag3 = true;
					}
					if (name.Equals("XMLFragment", StringComparison.Ordinal))
					{
						flag4 = true;
					}
					if (name.Equals("format", StringComparison.Ordinal))
					{
						stringFormat = current.Value.ToString();
					}
					if (name.Equals("StringTermination", StringComparison.Ordinal))
					{
						stringTermination = current.Value.ToString();
					}
				}
			}
			finally
			{
				(enumerator as IDisposable)?.Dispose();
			}
			if (flag)
			{
				return new WmiDataType
				{
					CimType = propertyData.Type,
					Index = index,
					IsActivityId = flag2,
					IsRelatedActivityId = flag3,
					IsXmlFragment = (flag4 | flag2 | flag3),
					Name = propertyData.Name,
					StringTermination = stringTermination,
					StringFormat = stringFormat,
					Type = TypeFromCimType(propertyData.Type, handleObjectAsGuid)
				};
			}
			return null;
		}

		private static Type TypeFromCimType(CimType cimType, bool handleObjectAsGuid)
		{
			if (cimType == CimType.Object && handleObjectAsGuid)
			{
				return typeof(Guid);
			}
			return TypeFromCimType(cimType);
		}

		private static Type TypeFromCimType(CimType cimType)
		{
			switch (cimType)
			{
			case CimType.Boolean:
				return typeof(bool);
			case CimType.Object:
				return typeof(object);
			case CimType.String:
				return typeof(string);
			case CimType.DateTime:
				return typeof(DateTime);
			case CimType.Real32:
				return typeof(float);
			case CimType.Real64:
				return typeof(double);
			case CimType.SInt16:
				return typeof(short);
			case CimType.SInt32:
				return typeof(int);
			case CimType.SInt64:
				return typeof(long);
			case CimType.UInt16:
				return typeof(ushort);
			case CimType.UInt8:
				return typeof(byte);
			case CimType.UInt32:
				return typeof(uint);
			case CimType.UInt64:
				return typeof(ulong);
			default:
				return typeof(object);
			}
		}

		private static Dictionary<int, WmiDataType> GetMofTypes(ManagementClass managementClass)
		{
			Dictionary<int, WmiDataType> dictionary = new Dictionary<int, WmiDataType>();
			PropertyDataCollection.PropertyDataEnumerator enumerator = managementClass.Properties.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					WmiDataType typeFromPropertyData = GetTypeFromPropertyData(enumerator.Current);
					if (typeFromPropertyData != null)
					{
						dictionary.Add(typeFromPropertyData.Index, typeFromPropertyData);
					}
				}
				return dictionary;
			}
			finally
			{
				(enumerator as IDisposable)?.Dispose();
			}
		}

		public static string GetXml(NativeMethods.EventTrace trace)
		{
			EventKey eventKey = default(EventKey);
			eventKey.Guid = trace.Header.Guid;
			eventKey.Type = trace.Header.Type;
			eventKey.Version = trace.Header.Version;
			eventKey.Level = trace.Header.Level;
			EventSchema eventSchema = GetEventSchema(eventKey);
			return GetXml(trace, eventSchema);
		}

		public static TraceEventType ConvertByteToTraceEventType(byte level)
		{
			switch (level)
			{
			case 0:
				return (TraceEventType)0;
			case 1:
				return TraceEventType.Critical;
			case 2:
				return TraceEventType.Error;
			case 3:
				return TraceEventType.Warning;
			case 4:
				return TraceEventType.Information;
			case 5:
				return TraceEventType.Verbose;
			default:
				return (TraceEventType)0;
			}
		}

		public static string GetXml(NativeMethods.EventTrace trace, EventSchema es)
		{
			StringBuilder stringBuilder = new StringBuilder(string.Empty);
			stringBuilder.Append("<E2ETraceEvent xmlns=\"");
			stringBuilder.Append("http://schemas.microsoft.com/2004/06/E2ETraceEvent");
			stringBuilder.Append("\">");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("  <System xmlns=\"");
			stringBuilder.Append("http://schemas.microsoft.com/2004/06/windows/eventlog/system");
			stringBuilder.Append("\">");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("    <EventID>0</EventID>");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("    <Type>3</Type>");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("    <SubType>");
			stringBuilder.Append(trace.Header.Type.ToString(CultureInfo.InvariantCulture));
			stringBuilder.Append("</SubType>");
			stringBuilder.Append(Environment.NewLine);
			TraceEventType traceEventType = (TraceEventType)0;
			traceEventType = ConvertByteToTraceEventType(trace.Header.Level);
			StringBuilder stringBuilder2 = stringBuilder;
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] obj = new object[1];
			uint num = (uint)traceEventType;
			obj[0] = num.ToString(CultureInfo.InvariantCulture);
			stringBuilder2.Append(string.Format(invariantCulture, "    <Level>{0}</Level>", obj));
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("    <Version>0</Version>");
			stringBuilder.Append(Environment.NewLine);
			DateTime dateTime = DateTime.FromFileTime(trace.Header.TimeStamp);
			stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "    <TimeCreated SystemTime=\"{0}\"/>", new object[1]
			{
				dateTime.ToString("o", CultureInfo.InvariantCulture)
			}));
			stringBuilder.Append(Environment.NewLine);
			byte[] array = new byte[trace.MofLength];
			if (trace.MofLength != 0 && trace.MofData != IntPtr.Zero)
			{
				Marshal.Copy(trace.MofData, array, 0, (int)trace.MofLength);
			}
			int pos = 0;
			StringBuilder stringBuilder3 = new StringBuilder(string.Empty);
			string activityId = GuidNull;
			string relatedActivityId = string.Empty;
			if (es == null)
			{
				stringBuilder3.Append(string.Format(CultureInfo.InvariantCulture, "    <Length>{0}</Length>", new object[1]
				{
					trace.MofLength.ToString(CultureInfo.InvariantCulture)
				}) + Environment.NewLine);
			}
			else
			{
				for (int i = 1; i <= es.WmiDataTypes.Count; i++)
				{
					if (!es.WmiDataTypes.ContainsKey(i) || array.GetUpperBound(0) == 0)
					{
						stringBuilder3.Append(string.Format(CultureInfo.InvariantCulture, "    <Length>{0}</Length>", new object[1]
						{
							trace.MofLength.ToString(CultureInfo.InvariantCulture)
						}) + Environment.NewLine);
						stringBuilder3.Append(string.Format(CultureInfo.InvariantCulture, "    <Error>Key {0} is missing from {1}: MOF does not match the trace</Error>", new object[2]
						{
							i.ToString(CultureInfo.InvariantCulture),
							es.ManagementClass.ClassPath.ToString()
						}) + Environment.NewLine);
					}
					GetXmlFromWmiDataType(stringBuilder3, es.WmiDataTypes[i], trace.MofData, array, ref pos, ref activityId, ref relatedActivityId);
				}
			}
			if (es == null)
			{
				stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "    <Source Name=\"{0}\" ID=\"{1}\"/>", new object[2]
				{
					trace.Header.Guid.ToString("B", CultureInfo.InvariantCulture),
					trace.Header.Guid.ToString("B", CultureInfo.InvariantCulture)
				}));
				stringBuilder.Append(Environment.NewLine);
			}
			else
			{
				string text = es.ManagementClass.ClassPath.ToString();
				int num2 = text.IndexOf(':');
				if (num2 > 0)
				{
					text = text.Substring(num2 + 1);
				}
				stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "    <Source Name=\"{0}\" ID=\"{1}\"/>", new object[2]
				{
					text,
					trace.Header.Guid.ToString("B")
				}));
				stringBuilder.Append(Environment.NewLine);
			}
			stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "    <Correlation ActivityID=\"{0}\"", new object[1]
			{
				activityId
			}));
			if (!string.IsNullOrEmpty(relatedActivityId))
			{
				stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, " RelatedActivityID=\"{0}\"", new object[1]
				{
					relatedActivityId
				}));
			}
			stringBuilder.Append("/>");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "    <Execution ProcessID=\"{0}\" ThreadID=\"{1}\"/>", new object[2]
			{
				trace.Header.ProcessId.ToString(CultureInfo.InvariantCulture),
				trace.Header.ThreadId.ToString(CultureInfo.InvariantCulture)
			}));
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "    <Channel />"));
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "    <Computer />"));
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("  </System>");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("  <ApplicationData>");
			stringBuilder.Append(Environment.NewLine);
			string value = stringBuilder3.ToString().Trim();
			if (!string.IsNullOrEmpty(value))
			{
				stringBuilder.Append("  <TraceData>");
				stringBuilder.Append(Environment.NewLine);
				stringBuilder.Append("  <DataItem>");
				stringBuilder.Append(Environment.NewLine);
				stringBuilder.Append(value);
				stringBuilder.Append("  ");
				stringBuilder.Append("  </DataItem>");
				stringBuilder.Append(Environment.NewLine);
				stringBuilder.Append("  </TraceData>");
				stringBuilder.Append(Environment.NewLine);
			}
			stringBuilder.Append("  </ApplicationData>");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("</E2ETraceEvent>");
			stringBuilder.Append(Environment.NewLine);
			return stringBuilder.ToString();
		}

		private static void GetXmlFromWmiDataType(StringBuilder buffer, WmiDataType wmiDataType, IntPtr MofData, byte[] appData, ref int pos, ref string activityId, ref string relatedActivityId)
		{
			bool flag = false;
			buffer.Append("    ");
			if (pos >= appData.GetUpperBound(0))
			{
				buffer.Append(string.Format(CultureInfo.InvariantCulture, "<{0}/>", new object[1]
				{
					wmiDataType.Name
				}));
			}
			else
			{
				if (!wmiDataType.IsXmlFragment)
				{
					buffer.Append(string.Format(CultureInfo.InvariantCulture, "<{0}>", new object[1]
					{
						wmiDataType.Name
					}));
				}
				if (typeof(Guid) == wmiDataType.Type)
				{
					byte[] array = new byte[16];
					Buffer.BlockCopy(appData, pos, array, 0, 16);
					string text = new Guid(array).ToString("B", CultureInfo.InvariantCulture);
					if (wmiDataType.IsActivityId)
					{
						activityId = text;
					}
					else if (wmiDataType.IsRelatedActivityId)
					{
						relatedActivityId = text;
					}
					else
					{
						buffer.Append(text);
					}
					pos += 16;
					flag = true;
				}
				else if (typeof(string) == wmiDataType.Type)
				{
					if (wmiDataType.StringTermination.Equals("NullTerminated", StringComparison.Ordinal))
					{
						if (wmiDataType.StringFormat.Equals("w", StringComparison.Ordinal))
						{
							StringBuilder stringBuilder = new StringBuilder();
							bool flag2 = false;
							while (!flag2 && pos < appData.GetUpperBound(0))
							{
								char c = BitConverter.ToChar(appData, pos);
								if (c == '\0')
								{
									flag2 = true;
								}
								else
								{
									stringBuilder.Append(c);
								}
								pos += 2;
							}
							buffer.Append(StringBuilderToXmlString(stringBuilder, !wmiDataType.IsXmlFragment));
							flag = true;
						}
						else
						{
							ASCIIEncoding aSCIIEncoding = new ASCIIEncoding();
							StringBuilder stringBuilder2 = new StringBuilder();
							bool flag3 = false;
							while (!flag3 && pos < appData.GetUpperBound(0))
							{
								char[] chars = aSCIIEncoding.GetChars(appData, pos, 1);
								pos++;
								if (chars[0] == '\0')
								{
									flag3 = true;
								}
								else
								{
									stringBuilder2.Append(chars[0]);
								}
							}
							buffer.Append(StringBuilderToXmlString(stringBuilder2, !wmiDataType.IsXmlFragment));
							flag = true;
						}
					}
					else
					{
						buffer.Append("<NonNullTerminatedString>Not supported by ETW parser</NonNullTerminatedString>");
					}
				}
				else if (typeof(ulong) == wmiDataType.Type)
				{
					string value = BitConverter.ToUInt64(appData, pos).ToString(CultureInfo.InvariantCulture);
					buffer.Append(value);
					pos += 8;
					flag = true;
				}
				else if (typeof(uint) == wmiDataType.Type)
				{
					string value2 = BitConverter.ToUInt32(appData, pos).ToString(CultureInfo.InvariantCulture);
					buffer.Append(value2);
					pos += 4;
					flag = true;
				}
				else if (typeof(ushort) == wmiDataType.Type)
				{
					string value3 = BitConverter.ToUInt16(appData, pos).ToString(CultureInfo.InvariantCulture);
					buffer.Append(value3);
					pos += 2;
					flag = true;
				}
				else if (typeof(long) == wmiDataType.Type)
				{
					string value4 = BitConverter.ToInt64(appData, pos).ToString(CultureInfo.InvariantCulture);
					buffer.Append(value4);
					pos += 8;
					flag = true;
				}
				else if (typeof(int) == wmiDataType.Type)
				{
					string value5 = BitConverter.ToInt32(appData, pos).ToString(CultureInfo.InvariantCulture);
					buffer.Append(value5);
					pos += 4;
					flag = true;
				}
				else if (typeof(short) == wmiDataType.Type)
				{
					string value6 = BitConverter.ToInt16(appData, pos).ToString(CultureInfo.InvariantCulture);
					buffer.Append(value6);
					pos += 2;
					flag = true;
				}
				else if (typeof(byte) == wmiDataType.Type)
				{
					string value7 = appData[pos].ToString(CultureInfo.InvariantCulture);
					buffer.Append(value7);
					pos++;
					flag = true;
				}
				if (!flag)
				{
					buffer.Append(wmiDataType.Type.ToString());
				}
				if (!wmiDataType.IsXmlFragment)
				{
					buffer.Append(string.Format(CultureInfo.InvariantCulture, "</{0}>", new object[1]
					{
						wmiDataType.Name
					}));
				}
			}
		}

		private static string ValidateXml(string supposedlyXml)
		{
			try
			{
				XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
				xmlReaderSettings.ConformanceLevel = ConformanceLevel.Fragment;
				XmlReader xmlReader = XmlReader.Create(new StringReader(supposedlyXml), xmlReaderSettings);
				while (xmlReader.Read())
				{
				}
				xmlReader.Close();
				return supposedlyXml;
			}
			catch (XmlException)
			{
				return XmlConvert.EncodeName(supposedlyXml);
			}
		}

		private static string StringBuilderToXmlString(StringBuilder builder, bool xmlEncode)
		{
			string text = builder.ToString();
			if (xmlEncode)
			{
				text = XmlEncode(text);
			}
			return ValidateXml(text);
		}

		private static string XmlEncode(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}
			int length = text.Length;
			StringBuilder stringBuilder = new StringBuilder(length + 8);
			for (int i = 0; i < length; i++)
			{
				char c = text[i];
				switch (c)
				{
				case '<':
					stringBuilder.Append("&lt;");
					break;
				case '>':
					stringBuilder.Append("&gt;");
					break;
				case '&':
					stringBuilder.Append("&amp;");
					break;
				default:
					stringBuilder.Append(c);
					break;
				}
			}
			return stringBuilder.ToString();
		}
	}
}
