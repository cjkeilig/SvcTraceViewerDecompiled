using System;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[Flags]
	internal enum SourceLevels
	{
		All = -1,
		Off = 0x0,
		Critical = 0x1,
		Error = 0x3,
		Warning = 0x7,
		Information = 0xF,
		Verbose = 0x1F,
		Start = 0x100,
		Stop = 0x200,
		ActivityTracing = 0xFF00
	}
}
