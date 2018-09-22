using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceLoadingTaskInfo : TaskInfoBase
	{
		private List<Activity> loadingActivities;

		public List<Activity> LoadingActivities => loadingActivities;

		public TraceLoadingTaskInfo(List<Activity> activities)
			: base(persistErrors: false)
		{
			loadingActivities = activities;
		}
	}
}
