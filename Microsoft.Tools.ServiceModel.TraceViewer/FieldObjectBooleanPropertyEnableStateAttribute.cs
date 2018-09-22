using System;
using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	internal sealed class FieldObjectBooleanPropertyEnableStateAttribute : Attribute
	{
		private List<string> stateNames;

		private string boolPropertyName;

		public List<string> StateNames => stateNames;

		public string BoolPropertyName => boolPropertyName;

		public FieldObjectBooleanPropertyEnableStateAttribute(string[] stateNames, string propertyName)
		{
			this.stateNames = new List<string>();
			foreach (string item in stateNames)
			{
				this.stateNames.Add(item);
			}
			boolPropertyName = propertyName;
		}
	}
}
