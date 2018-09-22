using System;
using System.Runtime.InteropServices;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal static class NativeMethods
	{
		[StructLayout(LayoutKind.Explicit)]
		internal struct FileTime
		{
			[FieldOffset(0)]
			internal uint low;

			[FieldOffset(4)]
			internal uint high;

			[FieldOffset(0)]
			internal long ticks;

			public FileTime(long ft)
			{
				low = (uint)ft;
				high = (uint)(ft >> 32);
				ticks = ft;
			}
		}

		internal struct SYSTEMTIME
		{
			internal ushort Year;

			internal ushort Month;

			internal ushort DayOfWeek;

			internal ushort Day;

			internal ushort Hour;

			internal ushort Minute;

			internal ushort Second;

			internal ushort Milliseconds;
		}

		internal struct EVENT_TRACE_HEADER
		{
			internal ushort Size;

			internal byte HeaderType;

			internal byte MarkerFlags;

			internal byte Type;

			internal byte Level;

			internal ushort Version;

			internal uint ThreadId;

			internal uint ProcessId;

			internal long TimeStamp;

			internal Guid Guid;

			internal uint ClientContext;

			internal uint Flags;
		}

		internal struct EventTrace
		{
			internal EVENT_TRACE_HEADER Header;

			internal uint InstanceId;

			internal uint ParentInstanceId;

			internal Guid ParentGuid;

			internal IntPtr MofData;

			internal uint MofLength;

			internal uint ClientContext;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct TIME_ZONE_INFORMATION
		{
			internal int Bias;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			internal string StandardName;

			internal SYSTEMTIME StandardDate;

			internal int StandardBias;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			internal string DaylightName;

			internal SYSTEMTIME DaylightDate;

			internal int DaylightBias;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct WCHAR8
		{
			internal char c01;

			internal char c02;

			internal char c03;

			internal char c04;

			internal char c05;

			internal char c06;

			internal char c07;

			internal char c08;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct WCHAR32
		{
			internal WCHAR8 c01;

			internal WCHAR8 c02;

			internal WCHAR8 c03;

			internal WCHAR8 c04;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		internal struct TRACE_LOGFILE_HEADER
		{
			internal uint BufferSize;

			internal byte MajorVersion;

			internal byte MinorVersion;

			internal byte SubVersion;

			internal byte SubMinorVersion;

			internal uint ProviderVersion;

			internal uint NumberOfProcessors;

			internal long EndTime;

			internal uint TimerResolution;

			internal uint MaximumFileSize;

			internal uint LogFileMode;

			internal uint BuffersWritten;

			internal Guid LogInstanceGuid;

			[MarshalAs(UnmanagedType.SysInt)]
			internal IntPtr LoggerName;

			internal IntPtr LogFileName;

			internal TIME_ZONE_INFORMATION TimeZone;

			internal long BootTime;

			internal long PerfFreq;

			internal long StartTime;

			internal uint ReservedFlags;

			internal uint BuffersLost;
		}

		public delegate void EventCallback([In] ref EventTrace et);

		public delegate uint BufferCallback([In] ref EventTraceLogFile etl);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct EventTraceLogFile
		{
			[MarshalAs(UnmanagedType.LPTStr)]
			internal string LogFileName;

			[MarshalAs(UnmanagedType.LPTStr)]
			internal string LoggerName;

			internal long CurrentTime;

			internal uint BuffersRead;

			internal uint LogFileMode;

			internal EventTrace CurrentEvent;

			internal TRACE_LOGFILE_HEADER LogfileHeader;

			[MarshalAs(UnmanagedType.FunctionPtr)]
			internal Delegate BufferCallback;

			internal uint BufferSize;

			internal uint Filled;

			internal uint EventsLost;

			[MarshalAs(UnmanagedType.FunctionPtr)]
			internal Delegate EventCallback;

			internal ulong IsKernelTrace;

			internal IntPtr Context;
		}

		public static ulong INVALID_HANDLE_VALUE = ulong.MaxValue;

		public static IntPtr InvalidIntPtr = (IntPtr)(-1);

		public static IntPtr NullPtr = (IntPtr)0;

		public static HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

		public const uint ERROR_SUCCESS = 0u;

		public const uint ERROR_CANCELLED = 1223u;

		[DllImport("advapi32.dll", EntryPoint = "OpenTraceW", SetLastError = true)]
		internal static extern ulong OpenTrace([In] ref EventTraceLogFile etl);

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern uint CloseTrace([In] ulong handle);

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern uint ProcessTrace([In] ulong[] handles, [In] uint count, [In] ref FileTime start, [In] ref FileTime end);

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern uint ProcessTrace([In] ulong[] handles, [In] uint count, [In] IntPtr start, [In] IntPtr end);
	}
}
