using System;
using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	internal sealed class UIControlSpecialStateEventHandlerAttribute : Attribute
	{
		private string defaultProcName = string.Empty;

		private string delegateProcName = string.Empty;

		private string eventName = string.Empty;

		private List<string> enableStateNames;

		public string DefaultEventHandlerName => defaultProcName;

		public string DelegatedEventHandlerName => delegateProcName;

		public string EventName => eventName;

		public List<string> StateNames => enableStateNames;

		public UIControlSpecialStateEventHandlerAttribute(string[] stateNames, string delegateProcName, string eventName, string defaultProcName)
		{
			this.delegateProcName = delegateProcName;
			this.defaultProcName = defaultProcName;
			this.eventName = eventName;
			enableStateNames = new List<string>();
			foreach (string item in stateNames)
			{
				enableStateNames.Add(item);
			}
		}
	}
}
