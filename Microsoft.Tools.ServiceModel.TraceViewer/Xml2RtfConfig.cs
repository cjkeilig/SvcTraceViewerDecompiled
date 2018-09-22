using System.Drawing;
using System.Globalization;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal static class Xml2RtfConfig
	{
		internal static readonly float SystemFontSize = SystemFonts.DefaultFont.Size;

		internal const string RtfHeader = "{\\rtf1\\ansi\\deff0";

		internal const string XmlFont = "{\\fonttbl{\\f0\\fnil\\fcharset0 Microsoft Sans Serif;}{\\f1\\fnil\\fcharset0 Verdana;}}";

		internal const string ColorTable = "{\\colortbl ;";

		internal const string TagColor = "\\red0\\green0\\blue255;";

		internal const string ElementColor = "\\red153\\green0\\blue0;";

		internal const string AttributeColor = "\\red255\\green0\\blue0;";

		internal const string ElementValueColor = "\\red0\\green0\\blue0;";

		internal const string CommentColor = "\\red136\\green136\\blue136;";

		internal const string RtfBegin = "}\\viewkind4\\uc0\\pard\\f0\\fs";

		internal static readonly string FontSizeFormat = ((int)(SystemFontSize * 2f)).ToString(CultureInfo.InvariantCulture);

		internal const string RtfEnd = "\\par}";

		internal static readonly int IndentIncrement = (int)(SystemFontSize * 30f);

		internal const string ElementValueFormat = "\\cf0\\f1\\b ";

		internal const string TagFormat = "\\cf1\\f1";

		internal const string ElementNameFormat = "\\cf2\\f1";

		internal const string AttributeNameFormat = "\\cf2\\f1";

		internal const string AttributeValueFormat = "\\cf0\\f1\\b ";

		internal const string CloseBoldFormat = "\\b0";

		internal const string NamespaceNameFormat = "\\cf3\\f1";

		internal const string NamespaceValueFormat = "\\cf3\\f1\\b ";

		internal const string CommentFormat = "\\cf5\\f1";

		internal const string CDataFormat = "\\cf1\\f1\\";

		internal const string NewLineFormat = "\\par\\pard";

		internal const string IndentFormat = "\\li";
	}
}
