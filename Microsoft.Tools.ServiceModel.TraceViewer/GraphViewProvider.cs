using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal abstract class GraphViewProvider : IDisposable
	{
		protected class GraphViewPersistObject
		{
			private GraphViewMode mode;

			private string currentActivityId;

			private object initData;

			internal object InitializeData => initData;

			internal string CurrentActivityID => currentActivityId;

			internal GraphViewMode GraphViewMode => mode;

			internal GraphViewPersistObject(GraphViewMode mode, string currentActivityId, object initData)
			{
				this.mode = mode;
				this.currentActivityId = currentActivityId;
				this.initData = initData;
			}
		}

		private Activity currentActivity;

		private TraceDataSource currentDataSource;

		private IWindowlessControlContainerExt currentContainer;

		private IErrorReport errorReport;

		private GraphViewMode currentViewMode;

		private bool isDisposed;

		private IUserInterfaceProvider userIP;

		protected IUserInterfaceProvider UserInterfaceProvider => userIP;

		public bool IsDisposed
		{
			get
			{
				return isDisposed;
			}
			set
			{
				isDisposed = value;
			}
		}

		internal GraphViewMode CurrentViewMode => currentViewMode;

		protected IErrorReport ErrorReport => errorReport;

		protected IWindowlessControlContainerExt Container => currentContainer;

		internal Activity CurrentActivity => currentActivity;

		protected TraceDataSource CurrentDataSource => currentDataSource;

		protected virtual object InitializeData => null;

		internal abstract bool IsEmpty
		{
			get;
		}

		internal virtual bool CanSupportZoom => false;

		internal virtual WindowlessControlScale CurrentScale => WindowlessControlScale.Normal;

		internal static List<IPersistStatus> GetPersistStatusObjects()
		{
			return new List<IPersistStatus>
			{
				ActivityTraceModeGraphProvider.GetViewSettingStatusObject()
			};
		}

		internal virtual object GetPersistObject()
		{
			return GetPersistObject(isInitData: true);
		}

		internal virtual object GetPersistObject(bool isInitData)
		{
			if (CurrentActivity != null)
			{
				return new GraphViewPersistObject(CurrentViewMode, CurrentActivity.Id, isInitData ? InitializeData : null);
			}
			return null;
		}

		internal static void RestoreGraphView(object persistObject, TraceDataSource dataSource, IWindowlessControlContainerExt container)
		{
			if (persistObject != null && persistObject is GraphViewPersistObject && dataSource != null && container != null)
			{
				GraphViewPersistObject graphViewPersistObject = (GraphViewPersistObject)persistObject;
				if (!string.IsNullOrEmpty(graphViewPersistObject.CurrentActivityID) && dataSource.Activities.ContainsKey(graphViewPersistObject.CurrentActivityID))
				{
					container.AnalysisActivityInHistory(dataSource.Activities[graphViewPersistObject.CurrentActivityID], graphViewPersistObject.GraphViewMode, graphViewPersistObject.InitializeData);
				}
			}
		}

		protected GraphViewProvider(Activity activity, TraceDataSource dataSource, IWindowlessControlContainerExt container, IErrorReport errorReport, IUserInterfaceProvider userIP, GraphViewMode mode)
		{
			currentActivity = activity;
			currentDataSource = dataSource;
			currentContainer = container;
			this.errorReport = errorReport;
			currentViewMode = mode;
			this.userIP = userIP;
		}

		internal static GraphViewProvider GetGraphViewProvider(Activity activity, TraceDataSource dataSource, IWindowlessControlContainerExt container, IErrorReport errorReport, IUserInterfaceProvider userIP, GraphViewMode mode, object initData)
		{
			GraphViewProvider graphViewProvider = null;
			if (activity != null && dataSource != null && container != null && errorReport != null)
			{
				if (mode == GraphViewMode.TraceMode)
				{
					graphViewProvider = new ActivityTraceModeGraphProvider(activity, dataSource, container, errorReport, userIP, mode);
				}
				if (graphViewProvider != null && initData != null)
				{
					graphViewProvider.Initialize(initData);
				}
			}
			return graphViewProvider;
		}

		protected virtual void Initialize(object initData)
		{
		}

		internal virtual bool BeforePerformAnalysis(object parameters)
		{
			return true;
		}

		internal abstract bool PerformAnalysis(bool isRestoring, bool reportError);

		internal virtual void SetupToolbar(ToolStrip toolStrip)
		{
		}

		protected virtual void DisposeObject()
		{
		}

		public void Dispose()
		{
			if (!IsDisposed)
			{
				DisposeObject();
				IsDisposed = true;
				GC.SuppressFinalize(this);
			}
		}

		~GraphViewProvider()
		{
			if (!IsDisposed)
			{
				DisposeObject();
			}
		}
	}
}
