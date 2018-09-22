using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceViewerFormProxy : IProgressReport, IUserInterfaceProvider
	{
		private TraceViewerForm traceViewerForm;

		public TraceViewerFormProxy(TraceViewerForm traceViewerForm)
		{
			if (traceViewerForm == null)
			{
				throw new ArgumentNullException();
			}
			this.traceViewerForm = traceViewerForm;
		}

		static TraceViewerFormProxy()
		{
			MethodInfo[] methods = typeof(TraceViewerFormProxy).GetMethods();
			if (methods != null)
			{
				MethodInfo[] array = methods;
				foreach (MethodInfo methodInfo in array)
				{
					switch (methodInfo.Name)
					{
					case "Begin":
					case "Complete":
					case "IndicateProgress":
					case "Step":
					case "ShowMessageBox":
					case "InvokeOnUIThread":
						RuntimeHelpers.PrepareMethod(methodInfo.MethodHandle);
						break;
					}
				}
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public void Begin(int expectedSteps)
		{
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				traceViewerForm.Invoke(traceViewerForm.InternalBeginToolStripProgressBarProxy, expectedSteps);
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public void Complete()
		{
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				traceViewerForm.Invoke(traceViewerForm.InternalCompleteToolStripProgressBarProxy);
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public void IndicateProgress(int activities, int traces)
		{
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				traceViewerForm.Invoke(traceViewerForm.InternalIndicateToolStripProgressBarProxy, activities, traces);
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public void Step()
		{
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				traceViewerForm.Invoke(traceViewerForm.InternalStepToolStripProgressBarProxy);
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public DialogResult ShowMessageBox(string message, string title, MessageBoxIcon icon, MessageBoxButtons btn)
		{
			string caption = (!string.IsNullOrEmpty(title)) ? (traceViewerForm.DefaultWindowTitle + "-" + title) : traceViewerForm.DefaultWindowTitle;
			traceViewerForm.TraceDataSourceStateController.SwitchState("TraceDataSourceValidatingState");
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				return MessageBox.Show(traceViewerForm, message, caption, btn, icon, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
			finally
			{
				traceViewerForm.TraceDataSourceStateController.SwitchState("TraceDataSourceIdleState");
			}
			return DialogResult.Cancel;
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public object InvokeOnUIThread(Delegate d, params object[] props)
		{
			if ((object)d == null)
			{
				return null;
			}
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				return traceViewerForm.Invoke(d, props);
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
			return null;
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public object InvokeOnUIThread(Delegate d)
		{
			return InvokeOnUIThread(d, null);
		}

		public DialogResult ShowDialog(Form dlg, Form parentForm)
		{
			DialogResult result = DialogResult.OK;
			if (dlg != null)
			{
				traceViewerForm.TraceDataSourceStateController.SwitchState("TraceDataSourceValidatingState");
				result = ((parentForm != null) ? dlg.ShowDialog(parentForm) : dlg.ShowDialog());
				traceViewerForm.TraceDataSourceStateController.SwitchState("TraceDataSourceIdleState");
			}
			return result;
		}

		public DialogResult ShowDialog(FileDialog dlg, Form parentForm)
		{
			DialogResult result = DialogResult.OK;
			if (dlg != null)
			{
				traceViewerForm.TraceDataSourceStateController.SwitchState("TraceDataSourceValidatingState");
				result = ((parentForm != null) ? dlg.ShowDialog(parentForm) : dlg.ShowDialog());
				traceViewerForm.TraceDataSourceStateController.SwitchState("TraceDataSourceIdleState");
			}
			return result;
		}
	}
}
