namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal enum ActivityType
	{
		RootActivity,
		ServiceHostActivity,
		ListenActivity,
		ConnectionActivity,
		MessageActivity,
		UserCodeExecutionActivity,
		NormalActivity,
		UnknownActivity
	}
}
