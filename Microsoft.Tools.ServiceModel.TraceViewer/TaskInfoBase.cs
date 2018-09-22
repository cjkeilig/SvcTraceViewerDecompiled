using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TaskInfoBase
	{
		public delegate void TaskFinished(TaskInfoBase taskInfo);

		private object thisLock = new object();

		private bool isCancelled;

		private List<TraceViewerException> exceptionList = new List<TraceViewerException>();

		private List<SystemException> systemExceptionList = new List<SystemException>();

		private bool persistErrors = true;

		private TaskFinished taskFinishedCallback;

		private object ThisLock => thisLock;

		public bool HasSystemException
		{
			get
			{
				lock (ThisLock)
				{
					return systemExceptionList.Count != 0;
				}
			}
		}

		public List<TraceViewerException> GetExceptionList
		{
			get
			{
				lock (ThisLock)
				{
					return exceptionList;
				}
			}
		}

		protected void SetTaskFinishedCallback(TaskFinished taskFinishedCallback)
		{
			lock (ThisLock)
			{
				if (taskFinishedCallback != null)
				{
					this.taskFinishedCallback = (TaskFinished)Delegate.Combine(this.taskFinishedCallback, taskFinishedCallback);
				}
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		protected void InvokeTaskFinishCallback()
		{
			lock (ThisLock)
			{
				if (taskFinishedCallback != null)
				{
					RuntimeHelpers.PrepareConstrainedRegions();
					try
					{
						taskFinishedCallback(this);
					}
					catch (Exception e)
					{
						ExceptionManager.GeneralExceptionFilter(e);
					}
					taskFinishedCallback = null;
				}
			}
		}

		protected TaskInfoBase(bool persistErrors)
		{
			this.persistErrors = persistErrors;
			PrepareMethods();
		}

		private void PrepareMethods()
		{
			RuntimeHelpers.PrepareMethod(GetType().GetMethod("SaveException").MethodHandle);
			RuntimeHelpers.PrepareMethod(GetType().GetMethod("IsCancelled").MethodHandle);
			RuntimeHelpers.PrepareMethod(GetType().GetMethod("Cancel").MethodHandle);
			RuntimeHelpers.PrepareMethod(GetType().GetMethod("Close").MethodHandle);
			RuntimeHelpers.PrepareDelegate(taskFinishedCallback);
		}

		public SystemException GetSystemException()
		{
			lock (ThisLock)
			{
				if (HasSystemException)
				{
					SystemException result = systemExceptionList[0];
					systemExceptionList.Clear();
					return result;
				}
				return null;
			}
		}

		public void SaveException(Exception exception)
		{
			lock (ThisLock)
			{
				if (exception != null && persistErrors)
				{
					if (exception is SystemException)
					{
						systemExceptionList.Add((SystemException)exception);
					}
					else if (exception is TraceViewerException)
					{
						exceptionList.Add((TraceViewerException)exception);
					}
				}
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public bool IsCancelled()
		{
			lock (ThisLock)
			{
				return isCancelled;
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private void InternalCancel()
		{
			lock (ThisLock)
			{
				isCancelled = true;
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public virtual void Cancel()
		{
			InternalCancel();
			InvokeTaskFinishCallback();
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public virtual void Close()
		{
		}
	}
}
