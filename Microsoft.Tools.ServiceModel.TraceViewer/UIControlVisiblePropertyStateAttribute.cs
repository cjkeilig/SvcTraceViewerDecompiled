using System;
using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	internal sealed class UIControlVisiblePropertyStateAttribute : Attribute
	{
		private List<string> visibleStateNames;

		private string expressionVariable;

		public List<string> StateNames => visibleStateNames;

		public string ExpressionVariable => expressionVariable;

		public UIControlVisiblePropertyStateAttribute(string[] stateNames)
		{
			visibleStateNames = new List<string>();
			foreach (string item in stateNames)
			{
				visibleStateNames.Add(item);
			}
		}
	}
}
