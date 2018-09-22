using System.Collections.Generic;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceDetailMessageLogInfoPart : ExpandablePart
	{
		private MessageTraceInfoControl messageLogInfoControl;

		private static List<string> matchPropertyNames;

		protected override string ExpandablePartName => SR.GetString("FV_MSG2_Title");

		static TraceDetailMessageLogInfoPart()
		{
			matchPropertyNames = new List<string>();
			matchPropertyNames.Add("MessageLogTraceRecord");
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

		public TraceDetailMessageLogInfoPart(ExpandablePartStateChanged callback)
		{
			messageLogInfoControl = new MessageTraceInfoControl();
			messageLogInfoControl.Dock = DockStyle.Fill;
			SetupRightPart(messageLogInfoControl, callback, messageLogInfoControl.Height + 30);
		}

		public override void ReloadTracePart(TraceDetailedProcessParameter parameter)
		{
			messageLogInfoControl.CleanUp();
			foreach (TraceDetailedProcessParameter.TraceProperty item in parameter)
			{
				if (IsMatchProperty(item))
				{
					messageLogInfoControl.ReloadMessageInfo(item.PropertyValue);
					parameter.RemoveProperty(item);
					UpdateUIElements();
					break;
				}
			}
		}
	}
}
