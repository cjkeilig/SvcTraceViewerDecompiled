using System.Drawing;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal interface IWindowlessControlContainer
	{
		WindowlessControlScale GetCurrentScale();

		void RegisterWindowlessControl(WindowlessControlBase control);

		void InvalidateParent();

		void InvalidateParent(Rectangle rect);

		void HighlightSelectedTraceRecordRow(TraceRecordCellControl traceCell);

		void RegisterHighlightedControls(WindowlessControlBase control);

		void RevertAllHighlightedControls();

		Color GetRandColorByIndex(int index);

		Activity GetActivityFromID(string activityId);

		void AnalysisActivityInTraceMode(Activity activity, TraceRecord trace);

		void AnalysisActivityInTraceMode(Activity activity);

		void AnalysisActivityInTraceMode(string activityId);

		void AnalysisActivityInTraceMode(Activity activity, TraceRecord trace, ActivityTraceModeAnalyzerParameters parameters);

		void ScrollControlIntoView(WindowlessControlBase ctrl);

		void ScrollControlIntoView(WindowlessControlBase ctrl, bool isCenter);

		void SetFocus();

		WindowlessControlBase FindWindowlessControl(object o);

		void RegisterExtentionEventListener(WindowlessControlExtentionEventCallback callback);
	}
}
