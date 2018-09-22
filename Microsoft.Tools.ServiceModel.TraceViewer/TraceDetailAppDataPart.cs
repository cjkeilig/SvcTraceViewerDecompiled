using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class TraceDetailAppDataPart : ExpandablePart
	{
		private AppDataTraceDetailControl appDataCtrl;

		protected override string ExpandablePartName => SR.GetString("FV_AppDataPartName");

		public static bool ContainsMatchProperties(TraceDetailedProcessParameter parameter)
		{
			if (parameter != null)
			{
				foreach (TraceDetailedProcessParameter.TraceProperty item in parameter)
				{
					if (item.PropertyName == SR.GetString("FV_AppDataText"))
					{
						return true;
					}
				}
			}
			return false;
		}

		public TraceDetailAppDataPart(ExpandablePartStateChanged callback)
		{
			appDataCtrl = new AppDataTraceDetailControl();
			appDataCtrl.Dock = DockStyle.Fill;
			SetupRightPart(appDataCtrl, callback, appDataCtrl.Height + 30);
		}

		public override void ReloadTracePart(TraceDetailedProcessParameter parameter)
		{
			appDataCtrl.CleanUp();
			foreach (TraceDetailedProcessParameter.TraceProperty item in parameter)
			{
				if (item.PropertyName == SR.GetString("FV_AppDataText"))
				{
					appDataCtrl.ReloadAppData(item.PropertyValue);
					parameter.RemoveProperty(item);
					break;
				}
			}
		}
	}
}
