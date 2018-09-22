using System;
using System.Collections.Generic;
using System.Management;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class EventSchema
	{
		internal ManagementClass ManagementClass;

		internal Dictionary<int, WmiDataType> WmiDataTypes;

		internal Guid ProviderGuid = Guid.Empty;
	}
}
