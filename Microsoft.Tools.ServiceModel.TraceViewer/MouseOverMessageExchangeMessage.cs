using System.Collections.Generic;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class MouseOverMessageExchangeMessage : WindowlessControlMessage
	{
		private List<WindowlessControlBase> relatedControls = new List<WindowlessControlBase>();

		private bool isReverting;

		public bool IsReverting => isReverting;

		internal List<WindowlessControlBase> RelatedControls => relatedControls;

		internal MouseOverMessageExchangeMessage(WindowlessControlBaseExt sender)
			: base(sender)
		{
			isReverting = true;
		}

		internal MouseOverMessageExchangeMessage(WindowlessControlBaseExt sender, List<WindowlessControlBase> relatedControls)
			: base(sender)
		{
			foreach (WindowlessControlBase relatedControl in relatedControls)
			{
				if (!this.relatedControls.Contains(relatedControl))
				{
					this.relatedControls.Add(relatedControl);
				}
			}
		}
	}
}
