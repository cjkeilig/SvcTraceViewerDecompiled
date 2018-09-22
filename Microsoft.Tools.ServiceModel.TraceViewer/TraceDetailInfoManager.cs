using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceDetailInfoManager
	{
		private static object objectLock = new object();

		private static TraceDetailInfoManager internalInstance = null;

		private List<IAdvancedTraceInfoProvider> advancedTraceInfoProviders = new List<IAdvancedTraceInfoProvider>();

		private static object ObjectLock => objectLock;

		public static TraceDetailInfoManager GetInstance()
		{
			lock (ObjectLock)
			{
				if (internalInstance == null)
				{
					internalInstance = new TraceDetailInfoManager();
				}
				return internalInstance;
			}
		}

		private TraceDetailInfoManager()
		{
			lock (ObjectLock)
			{
				advancedTraceInfoProviders.Add(new DefaultTraceInfoProvider());
			}
		}

		public IAdvancedTraceInfoProvider GetAdvancedTraceInfoProvider(TraceRecord trace)
		{
			lock (ObjectLock)
			{
				if (advancedTraceInfoProviders != null)
				{
					foreach (IAdvancedTraceInfoProvider advancedTraceInfoProvider in advancedTraceInfoProviders)
					{
						if (advancedTraceInfoProvider.CanSupport(trace))
						{
							return advancedTraceInfoProvider;
						}
					}
				}
				return null;
			}
		}
	}
}
