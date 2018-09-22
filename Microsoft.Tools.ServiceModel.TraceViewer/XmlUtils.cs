using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class XmlUtils
	{
		private IErrorReport errorReport;

		private void XmlExceptionHandler(XmlException e)
		{
			E2EInvalidFileException exception = new E2EInvalidFileException(SR.GetString("MsgFailToSkipNode"), string.Empty, e, -1L);
			if (errorReport != null)
			{
				errorReport.LogError(exception);
			}
			throw e;
		}

		public XmlUtils(IErrorReport errorReport)
		{
			this.errorReport = errorReport;
		}

		public Guid GuidFromIntString(string request)
		{
			try
			{
				byte[] array = new byte[16];
				if (request.Trim().StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				{
					string text = request.Trim().Substring(2, request.Trim().Length - 2);
					if (text.Length <= 32)
					{
						StringBuilder stringBuilder = new StringBuilder("00000000-0000-0000-0000-000000000000");
						int num = 0;
						int num2 = text.Length - 1;
						while (num2 >= 0)
						{
							StringBuilder stringBuilder2 = stringBuilder;
							stringBuilder2[stringBuilder2.Length - num - 1] = text[num2];
							num2--;
							num++;
						}
						return new Guid(stringBuilder.ToString());
					}
				}
				ulong num3 = ulong.Parse(request, CultureInfo.InvariantCulture);
				for (int i = 0; i < 16; i++)
				{
					array[15 - i] = (byte)(0xFF & num3);
					num3 >>= 8;
				}
				return new Guid(array);
			}
			catch (FormatException)
			{
				return Guid.Empty;
			}
			catch (OverflowException)
			{
				return Guid.Empty;
			}
			catch (ArgumentNullException)
			{
				return Guid.Empty;
			}
		}

		public bool SkipToChild(XmlReader reader, string name)
		{
			try
			{
				if (reader == null || reader.EOF)
				{
					return false;
				}
				int depth = reader.Depth;
				while (reader.Name != name && reader.Read() && (!(reader.Name == "E2ETraceEvent") || reader.NodeType != XmlNodeType.EndElement) && reader.Name != name && reader.Depth > depth)
				{
					reader.Skip();
				}
			}
			catch (XmlException e)
			{
				XmlExceptionHandler(e);
			}
			return reader.Name == name;
		}

		public bool SkipToSibling(XmlReader reader, string name)
		{
			try
			{
				if (reader == null || reader.EOF)
				{
					return false;
				}
				int depth = reader.Depth;
				while (reader.Read() && (!(reader.Name == "E2ETraceEvent") || reader.NodeType != XmlNodeType.EndElement) && reader.Name != name && reader.Depth >= depth)
				{
					reader.Skip();
				}
			}
			catch (XmlException e)
			{
				XmlExceptionHandler(e);
			}
			return reader.Name == name;
		}

		public bool SkipToEither(XmlReader reader, string name1, string name2)
		{
			return SkipToEither(reader, name1, name2, continuePastClosingTag: true);
		}

		public bool SkipToEither(XmlReader reader, string name1, string name2, bool continuePastClosingTag)
		{
			Hashtable hashtable = new Hashtable(2);
			hashtable.Add(name1, null);
			hashtable.Add(name2, null);
			return SkipToEither(reader, hashtable, continuePastClosingTag);
		}

		public bool SkipTo(XmlReader reader, string name, bool throwExceptionOnEnd)
		{
			return SkipTo(reader, name,true, throwExceptionOnEnd);
		}

		public bool SkipTo(XmlReader reader, string name)
		{
			return SkipTo(reader, name, throwExceptionOnEnd: true);
		}

		private bool SkipTo(XmlReader reader, string name, bool continuePastClosingTag, bool throwExceptionOnEnd)
		{
			try
			{
				if (reader == null || reader.EOF)
				{
					return false;
				}
				int depth = reader.Depth;
				while ((reader.Name != name && reader.Read() && (!(reader.Name == "E2ETraceEvent") || reader.NodeType != XmlNodeType.EndElement) && reader.Name != name) || reader.NodeType == XmlNodeType.EndElement)
				{
					if (!continuePastClosingTag && reader.Name == name && reader.NodeType == XmlNodeType.EndElement)
					{
						return false;
					}
					reader.Skip();
					if (!continuePastClosingTag && reader.Name == name && reader.NodeType == XmlNodeType.EndElement)
					{
						return false;
					}
				}
			}
			catch (XmlException e)
			{
				XmlExceptionHandler(e);
			}
			if (throwExceptionOnEnd && (reader.EOF || (reader.Name == "E2ETraceEvent" && reader.NodeType == XmlNodeType.EndElement)))
			{
				throw new E2EInvalidFileException(SR.GetString("MsgReaderEOF"), string.Empty, null, -1L);
			}
			return reader.Name == name;
		}

		public bool SkipToEither(XmlReader reader, Hashtable names, bool continuePastClosingTag)
		{
			try
			{
				if (reader == null || reader.EOF)
				{
					return false;
				}
				int depth = reader.Depth;
				while ((!names.ContainsKey(reader.Name) && reader.Read() && (!(reader.Name == "E2ETraceEvent") || reader.NodeType != XmlNodeType.EndElement) && !names.ContainsKey(reader.Name)) || reader.NodeType == XmlNodeType.EndElement)
				{
					if (!continuePastClosingTag && names.ContainsKey(reader.Name) && reader.NodeType == XmlNodeType.EndElement)
					{
						return false;
					}
					reader.Skip();
					if (!continuePastClosingTag && names.ContainsKey(reader.Name) && reader.NodeType == XmlNodeType.EndElement)
					{
						return false;
					}
				}
			}
			catch (XmlException e)
			{
				XmlExceptionHandler(e);
			}
			return names.ContainsKey(reader.Name);
		}
	}
}
