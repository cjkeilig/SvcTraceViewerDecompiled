namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class XmlNodeRecord
	{
		private string value;

		private int pos = -1;

		public string Value => value;

		public int Pos => pos;

		public XmlNodeRecord(string value, int pos)
		{
			this.value = value;
			this.pos = pos;
		}
	}
}
