using System;
using System.Management;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class WmiDataType
	{
		internal Type Type;

		internal CimType CimType;

		internal bool IsXmlFragment;

		internal bool IsActivityId;

		internal bool IsRelatedActivityId;

		internal string StringFormat;

		internal string StringTermination;

		internal int Index;

		internal string Name;
	}
}
