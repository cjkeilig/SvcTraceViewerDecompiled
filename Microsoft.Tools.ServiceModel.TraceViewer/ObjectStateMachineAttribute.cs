using System;
using System.Reflection;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	internal sealed class ObjectStateMachineAttribute : Attribute
	{
		public const string DefaultScopeName = "NONE_SCOPE_NAME";

		private string scopeName = "NONE_SCOPE_NAME";

		private bool isInitState;

		private ObjectStateBase objectState;

		private ObjectStateBase defaultNextObjectState;

		public Type ObjectStateType => objectState.GetType();

		public string Scope => scopeName;

		public ObjectStateBase ObjectState => objectState;

		public bool IsInitState => isInitState;

		public ObjectStateBase DefaultNextObjectState => defaultNextObjectState;

		public ObjectStateMachineAttribute(Type objectStateType, bool isInitState, Type defaultNextObjectStateType)
		{
			if (objectStateType != null)
			{
				objectState = (ObjectStateBase)Assembly.GetExecutingAssembly().CreateInstance(objectStateType.Namespace + "." + objectStateType.Name);
				this.isInitState = isInitState;
				if (defaultNextObjectStateType != null)
				{
					defaultNextObjectState = (ObjectStateBase)Assembly.GetExecutingAssembly().CreateInstance(defaultNextObjectStateType.Namespace + "." + defaultNextObjectStateType.Name);
				}
			}
		}

		public ObjectStateMachineAttribute(Type objectStateType, bool isInitState, Type defaultNextObjectStateType, string scopeName)
		{
			if (objectStateType != null)
			{
				objectState = (ObjectStateBase)Assembly.GetExecutingAssembly().CreateInstance(objectStateType.Namespace + "." + objectStateType.Name);
				this.isInitState = isInitState;
				if (defaultNextObjectStateType != null)
				{
					defaultNextObjectState = (ObjectStateBase)Assembly.GetExecutingAssembly().CreateInstance(defaultNextObjectStateType.Namespace + "." + defaultNextObjectStateType.Name);
				}
				this.scopeName = scopeName;
			}
		}
	}
}
