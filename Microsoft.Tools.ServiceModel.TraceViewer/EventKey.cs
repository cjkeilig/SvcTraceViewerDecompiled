using System;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal struct EventKey
	{
		internal byte Type;

		internal byte Level;

		internal ushort Version;

		internal Guid Guid;
	}
}
