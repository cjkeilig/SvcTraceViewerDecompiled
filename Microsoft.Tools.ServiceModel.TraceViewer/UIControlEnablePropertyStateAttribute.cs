using System;
using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	internal sealed class UIControlEnablePropertyStateAttribute : Attribute
	{
		private string expressionVariable;

		private List<string> enabledStateNames;

		public string ExpressionVariable => expressionVariable;

		public List<string> StateNames => enabledStateNames;

		public UIControlEnablePropertyStateAttribute(string[] stateNames)
		{
			enabledStateNames = new List<string>();
			foreach (string item in stateNames)
			{
				enabledStateNames.Add(item);
			}
		}
	}
}
