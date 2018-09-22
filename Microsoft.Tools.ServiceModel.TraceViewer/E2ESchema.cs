namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal static class E2ESchema
	{
		public const string E2ETraceEventN = "E2ETraceEvent";

		public const char XmlEmptyC = ' ';

		public const char XmlActivityIdInvalidC = '"';

		public const char XmlActivityIdLeftC = '{';

		public const char XmlActivityIdRightC = '}';

		public const string XmlActivityIdLeft = "{";

		public const string XmlActivityIdRight = "}";

		public const string XmlPrefixSeperator = ":";

		public const char XmlPrefixSeperatorC = ':';

		public const string XmlNamespaceA = "xmlns";

		public const string XmlTextNodeN = "#text";

		public const string XmlCommentNodeN = "#comment";

		public const string CallstackMicrosoftNS = "Microsoft.";

		public const string CallstackSystemNS = "System.";

		public const string CallstackStacktraceSeperator = " at ";

		public const string CallstackStacktraceSeperator2 = "at ";

		public const string AppDataN = "ApplicationData";

		public const string AppDataSysDiagN = "System.Diagnostics";

		public const string AppDataSysDiagCallstackN = "Callstack";

		public const string AppDataExceptionN = "Exception";

		public const string AppDataExceptionTypeN = "ExceptionType";

		public const string AppDataExceptionMessageN = "Message";

		public const string AppDataExceptionNativeCodeN = "NativeErrorCode";

		public const string AppDataExceptionInnerExceptionN = "InnerException";

		public const string AppDataExceptionStackTraceN = "StackTrace";

		public const string AppDataMsgIDN = "MessageID";

		public const string AppDataMsgRelatesToN = "RelatesTo";

		public const string AppDataMsgActivityIDN = "ActivityId";

		public const string AppDataMsgActionN = "Action";

		public const string AppDataMsgReplyToN = "ReplyTo";

		public const string AppDataMsgToN = "To";

		public const string AppDataMsgFromN = "From";

		public const string AppDataMsgAddressN = "Address";

		public const string AppDataMsgEnvelopeN = "Envelope";

		public const string AppDataMsgLogTraceN = "MessageLogTraceRecord";

		public const string AppDataMsgLogTraceTimeA = "Time";

		public const string AppDataMsgLogTraceTypeA = "Type";

		public const string AppDataMsgLogTraceSourceA = "Source";

		public const string AppDataMsgLogTracePropertiesN = "Properties";

		public const string AppDataMsgLogTraceValueTypeN = "ValueType";

		public const string AppDataMsgLogTraceSoapHeaderN = "Header";

		public const string AppDataMsgLogTraceSoapBodyN = "Body";

		public const string AppDataMsgLogTraceSoapPropertiesN = "MessageProperties";

		public const string AppDataMsgLogTraceSoapHeadersN = "MessageHeaders";

		public const string AppDataMsgHeaderActivityIdN = "ActivityId";

		public const string AppDataMsgHeaderActivityIdCorrelationIdA = "CorrelationId";

		public const string AppDataMsgHeaderActivityIdHeaderIdA = "HeaderId";

		public const string AppDataMsgHeaderActionN = "Action";

		public const string AppDataMsgHeaderActionN2 = "a:Action";

		public const string SysSourceNWCFSourceV = "System.ServiceModel";

		public const string XmlNamespaceColon = "xmlns:";

		public const string TransactionTraceKeyWord1 = "<CoordinationType>http://schemas.xmlsoap.org/ws/2004/10/wsat</CoordinationType>";

		public const string TransactionTraceKeyWord2 = "<wscoor:Identifier xmlns:wscoor=\"http://schemas.xmlsoap.org/ws/2004/10/wscoor\">";
	}
}
