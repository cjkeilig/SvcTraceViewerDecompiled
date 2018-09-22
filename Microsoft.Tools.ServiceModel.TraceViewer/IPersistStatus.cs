using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal interface IPersistStatus
	{
		void OutputToStream(XmlTextWriter writer);

		void RestoreFromXMLNode(XmlNode node);

		bool IsCurrentPersistNode(XmlNode node);
	}
}
