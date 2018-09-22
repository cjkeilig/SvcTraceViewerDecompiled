namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal interface IStateAwareObject
	{
		void PreStateSwitch(ObjectStateBase fromState, ObjectStateBase toState);

		void PostStateSwitch(ObjectStateBase fromState, ObjectStateBase toState);

		void StateSwitchSuccess(ObjectStateBase fromState, ObjectStateBase toState);

		void StateSwitchFailed(ObjectStateBase fromState, ObjectStateBase toState, ObjectStateSwitchFailReason reason);
	}
}
