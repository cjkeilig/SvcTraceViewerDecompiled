using System.Collections.Generic;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceDetailExceptionPart : ExpandablePart
	{
		private ExceptionTraceDetailControl exceptionControl;

		private static List<string> matchPropertyNames;

		protected override string ExpandablePartName => SR.GetString("FV_Exp_Title");

		static TraceDetailExceptionPart()
		{
			matchPropertyNames = new List<string>();
			matchPropertyNames.Add("Exception");
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

		public TraceDetailExceptionPart(ExpandablePartStateChanged callback)
		{
			exceptionControl = new ExceptionTraceDetailControl();
			exceptionControl.Dock = DockStyle.Fill;
			SetupRightPart(exceptionControl, callback, exceptionControl.Height + 30);
		}

		public override void ReloadTracePart(TraceDetailedProcessParameter parameter)
		{
			exceptionControl.CleanUp();
			List<string> list = new List<string>();
			foreach (TraceDetailedProcessParameter.TraceProperty item in parameter)
			{
				if (IsMatchProperty(item))
				{
					list.Add(item.PropertyValue);
					parameter.RemoveProperty(item);
				}
			}
			exceptionControl.ReloadExceptions(list);
			UpdateUIElements();
		}
	}
}
