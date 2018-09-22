using System;
using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	internal sealed class FieldObjectSetPropertyStateAttribute : Attribute
	{
		private List<string> stateNames;

		private string variableName;

		private string propertyName;

		public List<string> StateNames => stateNames;

		public string VariableName => variableName;

		public string PropertyName => propertyName;

		public FieldObjectSetPropertyStateAttribute(string[] stateNames, string propertyName, string variableName)
		{
			this.stateNames = new List<string>();
			foreach (string item in stateNames)
			{
				this.stateNames.Add(item);
			}
			this.variableName = variableName;
			this.propertyName = propertyName;
		}
	}
}
