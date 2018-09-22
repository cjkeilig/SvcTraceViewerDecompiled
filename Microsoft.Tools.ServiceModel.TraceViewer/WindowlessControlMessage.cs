namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class WindowlessControlMessage
	{
		private WindowlessControlBaseExt sender;

		internal WindowlessControlBaseExt Sender => sender;

		protected WindowlessControlMessage(WindowlessControlBaseExt sender)
		{
			this.sender = sender;
		}
	}
}
