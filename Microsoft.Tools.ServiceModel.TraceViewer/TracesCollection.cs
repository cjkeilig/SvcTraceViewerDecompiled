using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TracesCollection : IEnumerator, IDisposable
	{
		private IErrorReport errorReport;

		private TraceEntry currentTrace;

		private TraceEntry newTrace;

		private ManualResetEvent dataReady = new ManualResetEvent(initialState: false);

		private ManualResetEvent dataRead = new ManualResetEvent(initialState: true);

		private bool haveMore = true;

		private TraceReader reader;

		private DateTime startTimeFilter = DateTime.MinValue;

		private DateTime endTimeFilter = DateTime.MaxValue;

		private bool isDisposed;

		object IEnumerator.Current
		{
			get
			{
				return Current;
			}
		}

		public TraceEntry Current => currentTrace;

		public DateTime StartTime => startTimeFilter;

		public DateTime EndTime => endTimeFilter;

		public TracesCollection(string fileName, IErrorReport errorReport)
		{
			this.errorReport = errorReport;
			Initialzie(fileName);
		}

		private void Initialzie(string fileName)
		{
			if (!string.IsNullOrEmpty(fileName))
			{
				if (!Utilities.CreateFileInfoHelper(fileName).Exists)
				{
					throw new FileNotFoundException(SR.GetString("MsgSFNotFound"), fileName);
				}
				Utilities.CreateFileStreamHelper(fileName).Close();
				reader = new EtwTraceReader(QueueProcessor, StartTime, EndTime);
				reader.FileName = fileName;
			}
		}

		public IEnumerator GetEnumerator()
		{
			Reset();
			return this;
		}

		public bool MoveNext()
		{
			dataReady.WaitOne();
			dataReady.Reset();
			if (!haveMore)
			{
				return false;
			}
			currentTrace = newTrace;
			dataRead.Set();
			return true;
		}

		public void Reset()
		{
			haveMore = true;
			ThreadPool.QueueUserWorkItem(GetTraces, errorReport);
		}

		public void Stop()
		{
			haveMore = false;
			dataReady.Set();
		}

		private void GetTraces(object o)
		{
			try
			{
				reader.GetTraces();
				dataRead.WaitOne();
				Stop();
			}
			catch (TraceViewerException ex)
			{
				if (errorReport != null)
				{
					errorReport.ReportErrorToUser(ex.Message);
				}
				Stop();
			}
			catch (Win32Exception ex2)
			{
				if (errorReport != null)
				{
					errorReport.ReportErrorToUser(ex2.Message);
				}
				Stop();
			}
		}

		private void QueueProcessor(TraceEntry trace)
		{
			dataRead.WaitOne();
			dataRead.Reset();
			newTrace = trace;
			dataReady.Set();
		}

		public void Dispose()
		{
			if (!isDisposed)
			{
				if (reader != null)
				{
					reader.Close();
				}
				dataReady.Close();
				dataRead.Close();
				isDisposed = true;
				GC.SuppressFinalize(this);
			}
		}

		~TracesCollection()
		{
			if (!isDisposed)
			{
				Dispose();
			}
		}
	}
}
