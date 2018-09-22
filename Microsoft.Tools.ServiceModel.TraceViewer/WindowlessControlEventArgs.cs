namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class WindowlessControlEventArgs
	{
		private WindowlessControlEventType eventType;

		internal WindowlessControlEventType EventType => eventType;

		public WindowlessControlEventArgs(WindowlessControlEventType type)
		{
			eventType = type;
		}
	}
}
