using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class ExceptionManager : IErrorReport
	{
		private static IUserInterfaceProvider uiProvider;

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static void GeneralExceptionFilter(Exception e)
		{
			Program.GlobalSeriousExceptionHandler(e);
		}

		static ExceptionManager()
		{
			MethodInfo[] methods = typeof(ExceptionManager).GetMethods();
			foreach (MethodInfo methodInfo in methods)
			{
				switch (methodInfo.Name)
				{
				case "ReportErrorToUser":
				case "LogError":
				case "LogAppError":
				case "ReportCommonErrorToUser":
					RuntimeHelpers.PrepareMethod(methodInfo.MethodHandle);
					break;
				}
			}
		}

		public static void Initialize(IUserInterfaceProvider uip)
		{
			if (uip == null)
			{
				throw new ArgumentNullException();
			}
			uiProvider = uip;
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public void ReportErrorToUser(string message)
		{
			if (!string.IsNullOrEmpty(message))
			{
				ReportErrorToUser(message, null);
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public void ReportErrorToUser(string message, string debugMessage)
		{
			if (!string.IsNullOrEmpty(message) || !string.IsNullOrEmpty(debugMessage))
			{
				ReportErrorToUser(new TraceViewerException(message, debugMessage));
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public void ReportErrorToUser(TraceViewerException exception)
		{
			if (exception != null)
			{
				LogError(exception);
				ReportCommonErrorToUser(exception);
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public void LogError(string message)
		{
			if (!string.IsNullOrEmpty(message))
			{
				LogError(message, null);
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public void LogError(string message, string debugMessage)
		{
			if (!string.IsNullOrEmpty(message) || !string.IsNullOrEmpty(debugMessage))
			{
				LogError(new TraceViewerException(message, debugMessage));
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public void LogError(TraceViewerException exception)
		{
			if (exception != null)
			{
				LogAppError(exception);
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static void LogAppError(TraceViewerException e)
		{
			string text = e?.Message;
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public static void ReportCommonErrorToUser(TraceViewerException e)
		{
			if (e != null && !string.IsNullOrEmpty(e.Message))
			{
				try
				{
					uiProvider.ShowMessageBox(e.Message, null, MessageBoxIcon.Hand, MessageBoxButtons.OK);
				}
				catch (Exception e2)
				{
					GeneralExceptionFilter(e2);
				}
			}
		}
	}
}
