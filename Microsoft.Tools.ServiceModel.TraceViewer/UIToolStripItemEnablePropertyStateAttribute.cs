using System;
using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	internal sealed class UIToolStripItemEnablePropertyStateAttribute : Attribute
	{
		private List<string> enableStateNames;

		private string expressionVariable;

		public List<string> StateNames => enableStateNames;

		public string ExpressionVariable => expressionVariable;

		public UIToolStripItemEnablePropertyStateAttribute(string[] stateNames)
		{
			enableStateNames = new List<string>();
			foreach (string item in stateNames)
			{
				enableStateNames.Add(item);
			}
		}

		public UIToolStripItemEnablePropertyStateAttribute(string[] stateNames, string expressionVariable)
			: this(stateNames)
		{
			this.expressionVariable = expressionVariable;
		}
	}
}
