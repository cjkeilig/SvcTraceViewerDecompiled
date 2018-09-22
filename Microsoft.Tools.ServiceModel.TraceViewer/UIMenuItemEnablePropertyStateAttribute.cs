using System;
using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	internal sealed class UIMenuItemEnablePropertyStateAttribute : Attribute
	{
		private List<string> enableStateNames;

		private string expressionVariable;

		public List<string> StateNames => enableStateNames;

		public string ExpressionVariable => expressionVariable;

		public UIMenuItemEnablePropertyStateAttribute(string[] stateNames)
		{
			enableStateNames = new List<string>();
			foreach (string item in stateNames)
			{
				enableStateNames.Add(item);
			}
		}
	}
}
