namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal abstract class ObjectStateBase
	{
		private string stateName = string.Empty;

		private ObjectStateBase previousState;

		public string StateName
		{
			get
			{
				return stateName;
			}
			set
			{
				stateName = value;
			}
		}

		public ObjectStateBase PreviousState
		{
			get
			{
				return previousState;
			}
			set
			{
				previousState = value;
			}
		}

		public virtual bool EntranceCheck(ObjectStateBase previousState)
		{
			return true;
		}

		public virtual bool ExitCheck(ObjectStateBase nextState)
		{
			return true;
		}

		public virtual void PreState()
		{
		}

		public virtual void PostState()
		{
		}
	}
}
