namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal interface IProgressReport
	{
		void Begin(int expectedSteps);

		void Complete();

		void IndicateProgress(int activities, int traces);

		void Step();
	}
}
