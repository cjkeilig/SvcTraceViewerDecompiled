using System;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	internal sealed class ObjectStateTransferAttribute : Attribute
	{
		private string mapFromStateName = string.Empty;

		private string mapToStateName = string.Empty;

		public string MapFromStateName => mapFromStateName;

		public string MapToStateName => mapToStateName;

		public ObjectStateTransferAttribute(string mapFromStateName, string mapToStateName)
		{
			this.mapFromStateName = mapFromStateName;
			this.mapToStateName = mapToStateName;
		}
	}
}
