using System.Collections.Generic;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceDetailMessageInfoPart : ExpandablePart
	{
		private MessageHeaderTraceInfoControl messageInfoControl;

		private static List<string> matchPropertyNames;

		protected override string ExpandablePartName => SR.GetString("FV_MSG_Title");

		static TraceDetailMessageInfoPart()
		{
			matchPropertyNames = new List<string>();
			matchPropertyNames.Add("MessageProperties");
			matchPropertyNames.Add("MessageHeaders");
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

		public TraceDetailMessageInfoPart(ExpandablePartStateChanged callback)
		{
			messageInfoControl = new MessageHeaderTraceInfoControl();
			messageInfoControl.Dock = DockStyle.Fill;
			SetupRightPart(messageInfoControl, callback, messageInfoControl.Height + 30);
		}

		public override void ReloadTracePart(TraceDetailedProcessParameter parameter)
		{
			messageInfoControl.CleanUp();
			string messageHeadersInfoXml = null;
			string messagePropertiesInfoXml = null;
			foreach (TraceDetailedProcessParameter.TraceProperty item in parameter)
			{
				if (IsMatchProperty(item))
				{
					string propertyName = item.PropertyName;
					if (!(propertyName == "MessageProperties"))
					{
						if (propertyName == "MessageHeaders")
						{
							messageHeadersInfoXml = item.PropertyValue;
						}
					}
					else
					{
						messagePropertiesInfoXml = item.PropertyValue;
					}
					parameter.RemoveProperty(item);
				}
			}
			messageInfoControl.ReloadMessageRelatedInfo(messagePropertiesInfoXml, messageHeadersInfoXml);
			UpdateUIElements();
		}
	}
}
