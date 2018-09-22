using System.Collections.Generic;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceDetailDiagnosticsPart : ExpandablePart
	{
		private DiagnosticsTraceDetailControl diagControl;

		private static List<string> matchPropertyNames;

		protected override string ExpandablePartName => SR.GetString("FV_Diag_Title");

		static TraceDetailDiagnosticsPart()
		{
			matchPropertyNames = new List<string>();
			matchPropertyNames.Add("System.Diagnostics");
		}

		private static bool IsMatchProperty(TraceDetailedProcessParameter.TraceProperty prop)
		{
			if (prop != null && !string.IsNullOrEmpty(prop.PropertyName) && matchPropertyNames.Contains(prop.PropertyName) && prop.IsXmlFormat && !prop.IsXmlAttribute && !string.IsNullOrEmpty(prop.PropertyValue))
			{
				return true;
			}
			return false;
		}

		public static bool ContainsMatchProperties(TraceDetailedProcessParameter parameter)
		{
			if (parameter != null)
			{
				foreach (TraceDetailedProcessParameter.TraceProperty item in parameter)
				{
					if (IsMatchProperty(item))
					{
						return true;
					}
				}
			}
			return false;
		}

		public TraceDetailDiagnosticsPart(ExpandablePartStateChanged callback)
		{
			diagControl = new DiagnosticsTraceDetailControl();
			diagControl.Dock = DockStyle.Fill;
			SetupRightPart(diagControl, callback, diagControl.Height + 30);
		}

		public override void ReloadTracePart(TraceDetailedProcessParameter parameter)
		{
			diagControl.CleanUp();
			foreach (TraceDetailedProcessParameter.TraceProperty item in parameter)
			{
				if (IsMatchProperty(item))
				{
					diagControl.ReloadExceptions(item.PropertyValue);
					parameter.RemoveProperty(item);
					UpdateUIElements();
					break;
				}
			}
		}
	}
}
