namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceEntry
	{
		private string xml;

		public string Xml => xml;

		public bool IsErrorTrace => string.IsNullOrEmpty(xml);

		internal TraceEntry(string xml)
		{
			this.xml = xml;
		}
	}
}
