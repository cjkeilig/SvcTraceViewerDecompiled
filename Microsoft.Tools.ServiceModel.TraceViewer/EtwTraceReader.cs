using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class EtwTraceReader : TraceReader
	{
		private ulong handle;

		private NativeMethods.EventTraceLogFile logFile;

		private NativeMethods.BufferCallback bufferCallback;

		private NativeMethods.EventCallback eventCallback;

		private bool isClosed;

		public EtwTraceReader(TraceCallback callback, DateTime start, DateTime end)
			: base(callback, start, end)
		{
			Initialize();
		}

		~EtwTraceReader()
		{
			Close();
		}

		private void Initialize()
		{
			logFile = default(NativeMethods.EventTraceLogFile);
			bufferCallback = this.BufferCallback;
			eventCallback = this.EventCallback;
		}

		public override void Close()
		{
			if (!isClosed)
			{
				if (handle != 0L)
				{
					NativeMethods.CloseTrace(handle);
					handle = 0uL;
				}
				GC.SuppressFinalize(this);
				isClosed = true;
			}
		}

		public override void GetTraces()
		{
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				if (!isTraceOpen)
				{
					try
					{
						OpenLogFile();
					}
					catch (Win32Exception)
					{
						throw new TraceViewerException(SR.GetString("MsgInvalidInputFile"));
					}
				}
				else if (handle == 0L)
				{
					throw new TraceViewerException(SR.GetString("MsgNullHandle"));
				}
				if (startTimeFilter != DateTime.MinValue || endTimeFilter != DateTime.MaxValue)
				{
					filterTimeRange = true;
				}
				else
				{
					filterTimeRange = false;
				}
				ulong[] handles = new ulong[1]
				{
					handle
				};
				uint num;
				if (filterTimeRange)
				{
					NativeMethods.FileTime start = new NativeMethods.FileTime(startTimeFilter.ToFileTime());
					NativeMethods.FileTime end = new NativeMethods.FileTime(endTimeFilter.ToFileTime());
					num = NativeMethods.ProcessTrace(handles, 1u, ref start, ref end);
					if (num != 0)
					{
						throw new Win32Exception(Marshal.GetLastWin32Error());
					}
				}
				else
				{
					num = NativeMethods.ProcessTrace(handles, 1u, IntPtr.Zero, IntPtr.Zero);
					if (num != 0)
					{
						throw new Win32Exception(Marshal.GetLastWin32Error());
					}
				}
				if (num != 0 && num != 1223)
				{
					throw new TraceViewerException(SR.GetString("MsgProcessTraceFailed"));
				}
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
				throw new TraceViewerException(SR.GetString("MsgUnhandledExceptionInEtwGetTraces"));
			}
		}

		private void OpenLogFile()
		{
			if (handle == 0L)
			{
				logFile.LoggerName = null;
				logFile.LogFileName = base.FileName;
				logFile.BufferCallback = bufferCallback;
				logFile.EventCallback = eventCallback;
				handle = NativeMethods.OpenTrace(ref logFile);
				if (handle == NativeMethods.INVALID_HANDLE_VALUE)
				{
					handle = 0uL;
					throw new Win32Exception();
				}
			}
			isTraceOpen = true;
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private uint BufferCallback(ref NativeMethods.EventTraceLogFile etl)
		{
			if (cancelTraceProcessing)
			{
				cancelTraceProcessing = false;
				return 0u;
			}
			return 1u;
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private void EventCallback(ref NativeMethods.EventTrace et)
		{
			string text = null;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				text = MofUtils.GetXml(et);
				if (!string.IsNullOrEmpty(text))
				{
					processor(new TraceEntry(text));
				}
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
				processor(new TraceEntry(null));
			}
		}
	}
}
