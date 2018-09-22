using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal sealed class StateMachineController
	{
		private class StringPair
		{
			public string PropertyName;

			public string ValueName;

			public StringPair(string propertyName, string variableName)
			{
				PropertyName = propertyName;
				ValueName = variableName;
			}
		}

		private class ObjectEventHandlerPair
		{
			public object o;

			public Delegate eventHandler;

			public EventInfo eventInfo;

			public string eventFieldName = string.Empty;
		}

		private class ObjectStateWrapper
		{
			public string scopeName;

			public ObjectStateBase objectState;

			public ObjectStateBase nextObjectState;

			private bool isInitializeState;

			public List<Control> enabledControls = new List<Control>();

			public List<Control> visibleControls = new List<Control>();

			public List<MenuItem> enabledMenuItems = new List<MenuItem>();

			public List<ToolStripItem> enabledToolStripItems = new List<ToolStripItem>();

			public List<ObjectEventHandlerPair> objEventHandlerPairs = new List<ObjectEventHandlerPair>();

			public Dictionary<object, string> expressionEnabledObjects = new Dictionary<object, string>();

			public Dictionary<object, string> expressionVisibledObjects = new Dictionary<object, string>();

			public Dictionary<object, List<string>> enabledObjectBoolProperties = new Dictionary<object, List<string>>();

			public Dictionary<object, List<StringPair>> setPropertyObjectProperties = new Dictionary<object, List<StringPair>>();

			public bool IsInitializeState
			{
				get
				{
					return isInitializeState;
				}
				set
				{
					isInitializeState = value;
				}
			}
		}

		private class StateMapping
		{
			public string mapFromName;

			public string mapToName;

			public StateMapping(string from, string to)
			{
				mapFromName = from;
				mapToName = to;
			}
		}

		private IStateAwareObject registeredStateObject;

		private List<Control> attributedEnableControls = new List<Control>();

		private List<Control> attributedVisibleControls = new List<Control>();

		private List<MenuItem> attributedMenuItems = new List<MenuItem>();

		private List<ToolStripItem> attributedToolStripItems = new List<ToolStripItem>();

		private List<ObjectEventHandlerPair> attributedObjectEventHandlerPairs = new List<ObjectEventHandlerPair>();

		private Dictionary<object, List<string>> attributedObjectBoolProperties = new Dictionary<object, List<string>>();

		private Dictionary<object, List<StringPair>> attributedPropertyObjectProperties = new Dictionary<object, List<StringPair>>();

		private ObjectStateWrapper currentState;

		private Dictionary<string, ObjectStateWrapper> states = new Dictionary<string, ObjectStateWrapper>();

		private List<StateMachineController> stateSwitchListeners = new List<StateMachineController>();

		private Dictionary<string, StateMapping> stateMappings = new Dictionary<string, StateMapping>();

		private object thisLock = new object();

		private object ThisLock => thisLock;

		public string CurrentStateName => currentState.objectState.StateName;

		public bool SwitchState(string stateName)
		{
			lock (ThisLock)
			{
				ObjectStateWrapper stateByName = GetStateByName(stateName);
				if (currentState == null)
				{
					if (stateByName == null)
					{
						registeredStateObject.StateSwitchFailed(null, null, ObjectStateSwitchFailReason.TargetStateUnclear);
						return false;
					}
					currentState = stateByName;
					currentState.objectState.PreState();
				}
				if (currentState != null && currentState.objectState.StateName == stateName)
				{
					registeredStateObject.PreStateSwitch(currentState.objectState, currentState.objectState);
					ReconfirmState();
					registeredStateObject.PostStateSwitch(currentState.objectState, currentState.objectState);
					registeredStateObject.StateSwitchSuccess(currentState.objectState, currentState.objectState);
					NotifyRegisteredStateListener(currentState);
					return true;
				}
				if (stateByName == null)
				{
					registeredStateObject.StateSwitchFailed(currentState.objectState, null, ObjectStateSwitchFailReason.TargetStateUnclear);
					return false;
				}
				if (!currentState.objectState.ExitCheck(stateByName.objectState))
				{
					registeredStateObject.StateSwitchFailed(currentState.objectState, stateByName.objectState, ObjectStateSwitchFailReason.ExitCheck);
					return false;
				}
				if (!stateByName.objectState.EntranceCheck(currentState.objectState))
				{
					registeredStateObject.StateSwitchFailed(currentState.objectState, stateByName.objectState, ObjectStateSwitchFailReason.EntranceCheck);
					return false;
				}
				registeredStateObject.PreStateSwitch(currentState.objectState, stateByName.objectState);
				currentState.objectState.PostState();
				stateByName.objectState.PreviousState = currentState.objectState;
				stateByName.objectState.PreState();
				EnableAndVisibleStateFields(stateByName);
				AdjustEventHandlers(stateByName);
				registeredStateObject.PostStateSwitch(currentState.objectState, stateByName.objectState);
				registeredStateObject.StateSwitchSuccess(currentState.objectState, stateByName.objectState);
				currentState = stateByName;
				NotifyRegisteredStateListener(stateByName);
				return true;
			}
		}

		public void ReconfirmState()
		{
			EnableAndVisibleStateFields(currentState);
			AdjustEventHandlers(currentState);
		}

		public bool IsStateSupported(string stateName)
		{
			if (string.IsNullOrEmpty(stateName))
			{
				return false;
			}
			if (GetStateByName(stateName) == null)
			{
				return false;
			}
			return true;
		}

		private ObjectStateWrapper GetStateByName(string name)
		{
			string b = name;
			if (stateMappings.ContainsKey(name))
			{
				b = stateMappings[name].mapToName;
			}
			foreach (ObjectStateWrapper value in states.Values)
			{
				if (value.objectState.StateName == b)
				{
					return value;
				}
			}
			return null;
		}

		public void RegisterStateSwitchListener(StateMachineController stateController)
		{
			if (!stateSwitchListeners.Contains(stateController))
			{
				stateSwitchListeners.Add(stateController);
			}
		}

		public bool SwitchState(ObjectStateBase os)
		{
			if (os == null)
			{
				return false;
			}
			return SwitchState(os.StateName);
		}

		public bool SwitchState()
		{
			if (currentState == null || currentState.nextObjectState == null)
			{
				return false;
			}
			return SwitchState(currentState.nextObjectState);
		}

		private void NotifyRegisteredStateListener(ObjectStateWrapper switchTo)
		{
			foreach (StateMachineController stateSwitchListener in stateSwitchListeners)
			{
				if (stateSwitchListener.IsStateSupported(switchTo.objectState.StateName))
				{
					stateSwitchListener.SwitchState(switchTo.objectState.StateName);
				}
			}
		}

		private void RegisterStateObject(IStateAwareObject instance, string scopeName)
		{
			if (instance != null)
			{
				registeredStateObject = instance;
				Type type = instance.GetType();
				object[] customAttributes = type.GetCustomAttributes(inherit: false);
				foreach (object obj in customAttributes)
				{
					if (obj is ObjectStateMachineAttribute)
					{
						ObjectStateMachineAttribute objectStateMachineAttribute = (ObjectStateMachineAttribute)obj;
						if (!(objectStateMachineAttribute.Scope != scopeName))
						{
							ObjectStateWrapper objectStateWrapper = new ObjectStateWrapper();
							objectStateWrapper.objectState = objectStateMachineAttribute.ObjectState;
							objectStateWrapper.IsInitializeState = objectStateMachineAttribute.IsInitState;
							objectStateWrapper.nextObjectState = objectStateMachineAttribute.DefaultNextObjectState;
							states.Add(objectStateMachineAttribute.ObjectState.StateName, objectStateWrapper);
						}
					}
				}
				customAttributes = type.GetCustomAttributes(inherit: false);
				foreach (object obj2 in customAttributes)
				{
					if (obj2 is ObjectStateTransferAttribute)
					{
						ObjectStateTransferAttribute objectStateTransferAttribute = (ObjectStateTransferAttribute)obj2;
						stateMappings.Add(objectStateTransferAttribute.MapFromStateName, new StateMapping(objectStateTransferAttribute.MapFromStateName, objectStateTransferAttribute.MapToStateName));
					}
				}
				FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
				foreach (FieldInfo fieldInfo in fields)
				{
					object[] customAttributes2 = fieldInfo.GetCustomAttributes(typeof(UIControlSpecialStateEventHandlerAttribute), inherit: false);
					if (customAttributes2 != null && customAttributes2.Length != 0)
					{
						customAttributes = customAttributes2;
						for (int j = 0; j < customAttributes.Length; j++)
						{
							UIControlSpecialStateEventHandlerAttribute uIControlSpecialStateEventHandlerAttribute = (UIControlSpecialStateEventHandlerAttribute)customAttributes[j];
							bool flag = false;
							foreach (string stateName in uIControlSpecialStateEventHandlerAttribute.StateNames)
							{
								if (GetStateByName(stateName) != null)
								{
									flag = true;
									break;
								}
							}
							if (!flag)
							{
								break;
							}
							EventInfo @event = fieldInfo.FieldType.GetEvent(uIControlSpecialStateEventHandlerAttribute.EventName);
							if (@event == null)
							{
								break;
							}
							MethodInfo method = registeredStateObject.GetType().GetMethod(uIControlSpecialStateEventHandlerAttribute.DefaultEventHandlerName, BindingFlags.Instance | BindingFlags.NonPublic);
							if (method == null)
							{
								break;
							}
							MethodInfo method2 = registeredStateObject.GetType().GetMethod(uIControlSpecialStateEventHandlerAttribute.DelegatedEventHandlerName, BindingFlags.Instance | BindingFlags.NonPublic);
							if (method2 == null)
							{
								break;
							}
							ObjectEventHandlerPair objectEventHandlerPair = new ObjectEventHandlerPair();
							objectEventHandlerPair.eventHandler = Delegate.CreateDelegate(@event.EventHandlerType, registeredStateObject, method);
							objectEventHandlerPair.eventInfo = @event;
							objectEventHandlerPair.o = fieldInfo.GetValue(registeredStateObject);
							objectEventHandlerPair.eventFieldName = @event.Name;
							attributedObjectEventHandlerPairs.Add(objectEventHandlerPair);
							foreach (string stateName2 in uIControlSpecialStateEventHandlerAttribute.StateNames)
							{
								ObjectStateWrapper stateByName = GetStateByName(stateName2);
								if (stateByName == null)
								{
									break;
								}
								ObjectEventHandlerPair objectEventHandlerPair2 = GetObjectEventHandlerPair(stateByName.objEventHandlerPairs, fieldInfo.GetValue(registeredStateObject), @event);
								if (objectEventHandlerPair2 == null)
								{
									objectEventHandlerPair2 = new ObjectEventHandlerPair();
								}
								objectEventHandlerPair2.eventHandler = Delegate.CreateDelegate(@event.EventHandlerType, registeredStateObject, method2);
								objectEventHandlerPair2.o = fieldInfo.GetValue(registeredStateObject);
								objectEventHandlerPair2.eventInfo = @event;
								objectEventHandlerPair2.eventFieldName = @event.Name;
								stateByName.objEventHandlerPairs.Add(objectEventHandlerPair2);
								attributedObjectEventHandlerPairs.Add(objectEventHandlerPair2);
							}
						}
					}
					object[] customAttributes3 = fieldInfo.GetCustomAttributes(inherit: false);
					if (customAttributes3 != null && customAttributes3.Length != 0)
					{
						customAttributes = customAttributes3;
						foreach (object obj3 in customAttributes)
						{
							if (obj3 is FieldObjectSetPropertyStateAttribute)
							{
								FieldObjectSetPropertyStateAttribute fieldObjectSetPropertyStateAttribute = (FieldObjectSetPropertyStateAttribute)obj3;
								bool flag2 = false;
								foreach (string stateName3 in fieldObjectSetPropertyStateAttribute.StateNames)
								{
									if (GetStateByName(stateName3) != null)
									{
										flag2 = true;
										break;
									}
								}
								if (!flag2)
								{
									break;
								}
								if (!attributedPropertyObjectProperties.ContainsKey(fieldInfo.GetValue(registeredStateObject)))
								{
									List<StringPair> list = new List<StringPair>();
									list.Add(new StringPair(fieldObjectSetPropertyStateAttribute.PropertyName, fieldObjectSetPropertyStateAttribute.VariableName));
									attributedPropertyObjectProperties.Add(fieldInfo.GetValue(registeredStateObject), list);
								}
								else
								{
									attributedPropertyObjectProperties[fieldInfo.GetValue(registeredStateObject)].Add(new StringPair(fieldObjectSetPropertyStateAttribute.PropertyName, fieldObjectSetPropertyStateAttribute.VariableName));
								}
								foreach (string stateName4 in fieldObjectSetPropertyStateAttribute.StateNames)
								{
									if (states.ContainsKey(stateName4))
									{
										if (!states[stateName4].setPropertyObjectProperties.ContainsKey(fieldInfo.GetValue(registeredStateObject)))
										{
											List<StringPair> list2 = new List<StringPair>();
											list2.Add(new StringPair(fieldObjectSetPropertyStateAttribute.PropertyName, fieldObjectSetPropertyStateAttribute.VariableName));
											states[stateName4].setPropertyObjectProperties.Add(fieldInfo.GetValue(registeredStateObject), list2);
										}
										else
										{
											states[stateName4].setPropertyObjectProperties[fieldInfo.GetValue(registeredStateObject)].Add(new StringPair(fieldObjectSetPropertyStateAttribute.PropertyName, fieldObjectSetPropertyStateAttribute.VariableName));
										}
									}
								}
							}
							if (obj3 is FieldObjectBooleanPropertyEnableStateAttribute)
							{
								FieldObjectBooleanPropertyEnableStateAttribute fieldObjectBooleanPropertyEnableStateAttribute = (FieldObjectBooleanPropertyEnableStateAttribute)obj3;
								bool flag3 = false;
								foreach (string stateName5 in fieldObjectBooleanPropertyEnableStateAttribute.StateNames)
								{
									if (GetStateByName(stateName5) != null)
									{
										flag3 = true;
										break;
									}
								}
								if (!flag3)
								{
									break;
								}
								if (!attributedObjectBoolProperties.ContainsKey(fieldInfo.GetValue(registeredStateObject)))
								{
									List<string> list3 = new List<string>();
									list3.Add(fieldObjectBooleanPropertyEnableStateAttribute.BoolPropertyName);
									attributedObjectBoolProperties.Add(fieldInfo.GetValue(registeredStateObject), list3);
								}
								else
								{
									attributedObjectBoolProperties[fieldInfo.GetValue(registeredStateObject)].Add(fieldObjectBooleanPropertyEnableStateAttribute.BoolPropertyName);
								}
								foreach (string stateName6 in fieldObjectBooleanPropertyEnableStateAttribute.StateNames)
								{
									if (states.ContainsKey(stateName6))
									{
										if (!states[stateName6].enabledObjectBoolProperties.ContainsKey(fieldInfo.GetValue(registeredStateObject)))
										{
											List<string> list4 = new List<string>();
											list4.Add(fieldObjectBooleanPropertyEnableStateAttribute.BoolPropertyName);
											states[stateName6].enabledObjectBoolProperties.Add(fieldInfo.GetValue(registeredStateObject), list4);
										}
										else
										{
											states[stateName6].enabledObjectBoolProperties[fieldInfo.GetValue(registeredStateObject)].Add(fieldObjectBooleanPropertyEnableStateAttribute.BoolPropertyName);
										}
									}
								}
							}
							if (obj3 is UIControlEnablePropertyStateAttribute)
							{
								UIControlEnablePropertyStateAttribute uIControlEnablePropertyStateAttribute = (UIControlEnablePropertyStateAttribute)obj3;
								bool flag4 = false;
								foreach (string stateName7 in uIControlEnablePropertyStateAttribute.StateNames)
								{
									if (GetStateByName(stateName7) != null)
									{
										flag4 = true;
										break;
									}
								}
								if (!flag4)
								{
									break;
								}
								if (!attributedEnableControls.Contains((Control)fieldInfo.GetValue(registeredStateObject)))
								{
									attributedEnableControls.Add((Control)fieldInfo.GetValue(registeredStateObject));
								}
								foreach (string stateName8 in uIControlEnablePropertyStateAttribute.StateNames)
								{
									if (states.ContainsKey(stateName8))
									{
										states[stateName8].enabledControls.Add((Control)fieldInfo.GetValue(registeredStateObject));
										if (uIControlEnablePropertyStateAttribute.ExpressionVariable != null && uIControlEnablePropertyStateAttribute.ExpressionVariable != string.Empty)
										{
											states[stateName8].expressionEnabledObjects.Add(fieldInfo.GetValue(registeredStateObject), uIControlEnablePropertyStateAttribute.ExpressionVariable);
										}
									}
								}
							}
							if (obj3 is UIControlVisiblePropertyStateAttribute)
							{
								UIControlVisiblePropertyStateAttribute uIControlVisiblePropertyStateAttribute = (UIControlVisiblePropertyStateAttribute)obj3;
								bool flag5 = false;
								foreach (string stateName9 in uIControlVisiblePropertyStateAttribute.StateNames)
								{
									if (GetStateByName(stateName9) != null)
									{
										flag5 = true;
										break;
									}
								}
								if (!flag5)
								{
									break;
								}
								if (!attributedVisibleControls.Contains((Control)fieldInfo.GetValue(registeredStateObject)))
								{
									attributedVisibleControls.Add((Control)fieldInfo.GetValue(registeredStateObject));
								}
								foreach (string stateName10 in ((UIControlVisiblePropertyStateAttribute)obj3).StateNames)
								{
									if (states.ContainsKey(stateName10))
									{
										states[stateName10].visibleControls.Add((Control)fieldInfo.GetValue(registeredStateObject));
									}
									if (uIControlVisiblePropertyStateAttribute.ExpressionVariable != null && uIControlVisiblePropertyStateAttribute.ExpressionVariable != string.Empty)
									{
										states[stateName10].expressionVisibledObjects.Add(fieldInfo.GetValue(registeredStateObject), uIControlVisiblePropertyStateAttribute.ExpressionVariable);
									}
								}
							}
							if (obj3 is UIMenuItemEnablePropertyStateAttribute)
							{
								UIMenuItemEnablePropertyStateAttribute uIMenuItemEnablePropertyStateAttribute = (UIMenuItemEnablePropertyStateAttribute)obj3;
								bool flag6 = false;
								foreach (string stateName11 in uIMenuItemEnablePropertyStateAttribute.StateNames)
								{
									if (GetStateByName(stateName11) != null)
									{
										flag6 = true;
										break;
									}
								}
								if (!flag6)
								{
									break;
								}
								if (!attributedMenuItems.Contains((MenuItem)fieldInfo.GetValue(registeredStateObject)))
								{
									attributedMenuItems.Add((MenuItem)fieldInfo.GetValue(registeredStateObject));
								}
								foreach (string stateName12 in ((UIMenuItemEnablePropertyStateAttribute)obj3).StateNames)
								{
									if (states.ContainsKey(stateName12))
									{
										states[stateName12].enabledMenuItems.Add((MenuItem)fieldInfo.GetValue(registeredStateObject));
									}
									if (uIMenuItemEnablePropertyStateAttribute.ExpressionVariable != null && uIMenuItemEnablePropertyStateAttribute.ExpressionVariable != string.Empty)
									{
										states[stateName12].expressionEnabledObjects.Add(fieldInfo.GetValue(registeredStateObject), uIMenuItemEnablePropertyStateAttribute.ExpressionVariable);
									}
								}
							}
							if (obj3 is UIToolStripItemEnablePropertyStateAttribute)
							{
								UIToolStripItemEnablePropertyStateAttribute uIToolStripItemEnablePropertyStateAttribute = (UIToolStripItemEnablePropertyStateAttribute)obj3;
								bool flag7 = false;
								foreach (string stateName13 in uIToolStripItemEnablePropertyStateAttribute.StateNames)
								{
									if (GetStateByName(stateName13) != null)
									{
										flag7 = true;
										break;
									}
								}
								if (!flag7)
								{
									break;
								}
								if (!attributedToolStripItems.Contains((ToolStripItem)fieldInfo.GetValue(registeredStateObject)))
								{
									attributedToolStripItems.Add((ToolStripItem)fieldInfo.GetValue(registeredStateObject));
								}
								foreach (string stateName14 in ((UIToolStripItemEnablePropertyStateAttribute)obj3).StateNames)
								{
									if (states.ContainsKey(stateName14))
									{
										states[stateName14].enabledToolStripItems.Add((ToolStripItem)fieldInfo.GetValue(registeredStateObject));
									}
									if (uIToolStripItemEnablePropertyStateAttribute.ExpressionVariable != null && uIToolStripItemEnablePropertyStateAttribute.ExpressionVariable != string.Empty)
									{
										states[stateName14].expressionEnabledObjects.Add(fieldInfo.GetValue(registeredStateObject), uIToolStripItemEnablePropertyStateAttribute.ExpressionVariable);
									}
								}
							}
						}
					}
				}
			}
		}

		public StateMachineController(IStateAwareObject instance, string scopeName)
		{
			lock (ThisLock)
			{
				RegisterStateObject(instance, scopeName);
				if (states != null && states.Count != 0)
				{
					foreach (ObjectStateWrapper value in states.Values)
					{
						if (value.IsInitializeState)
						{
							if (!SwitchState(value.objectState))
							{
								currentState = null;
							}
							break;
						}
					}
				}
			}
		}

		public StateMachineController(IStateAwareObject instance)
		{
			lock (ThisLock)
			{
				RegisterStateObject(instance, "NONE_SCOPE_NAME");
				if (states != null && states.Count != 0)
				{
					foreach (ObjectStateWrapper value in states.Values)
					{
						if (value.IsInitializeState)
						{
							if (!SwitchState(value.objectState))
							{
								currentState = null;
							}
							break;
						}
					}
				}
			}
		}

		private ObjectEventHandlerPair GetObjectEventHandlerPair(List<ObjectEventHandlerPair> pairs, object o, EventInfo e)
		{
			foreach (ObjectEventHandlerPair pair in pairs)
			{
				if (pair.o == o && (object)pair.eventInfo == e)
				{
					return pair;
				}
			}
			return null;
		}

		private void EnableAndVisibleStateFields(ObjectStateWrapper state)
		{
			if (state != null)
			{
				foreach (object key in state.setPropertyObjectProperties.Keys)
				{
					if (key != null)
					{
						foreach (StringPair item in state.setPropertyObjectProperties[key])
						{
							FieldInfo field = registeredStateObject.GetType().GetField(item.ValueName, BindingFlags.Instance | BindingFlags.NonPublic);
							if (!(field == null))
							{
								object value = field.GetValue(registeredStateObject);
								key.GetType().InvokeMember(item.PropertyName, BindingFlags.SetProperty, null, key, new object[1]
								{
									value
								}, CultureInfo.CurrentCulture);
							}
						}
					}
				}
				foreach (object key2 in attributedObjectBoolProperties.Keys)
				{
					if (key2 != null)
					{
						foreach (string item2 in attributedObjectBoolProperties[key2])
						{
							if (state.enabledObjectBoolProperties.ContainsKey(key2) && state.enabledObjectBoolProperties[key2].Contains(item2))
							{
								key2.GetType().InvokeMember(item2, BindingFlags.SetProperty, null, key2, new object[1]
								{
									true
								}, CultureInfo.CurrentCulture);
							}
							else
							{
								key2.GetType().InvokeMember(item2, BindingFlags.SetProperty, null, key2, new object[1]
								{
									false
								}, CultureInfo.CurrentCulture);
							}
						}
					}
				}
				foreach (Control attributedEnableControl in attributedEnableControls)
				{
					if (attributedEnableControl != null)
					{
						if (state.enabledControls.Contains(attributedEnableControl))
						{
							if (!state.expressionEnabledObjects.ContainsKey(attributedEnableControl))
							{
								attributedEnableControl.Enabled = true;
							}
							else if (CheckExpression(state.expressionEnabledObjects[attributedEnableControl]))
							{
								attributedEnableControl.Enabled = true;
							}
							else
							{
								attributedEnableControl.Enabled = false;
							}
						}
						else
						{
							attributedEnableControl.Enabled = false;
						}
					}
				}
				foreach (Control attributedVisibleControl in attributedVisibleControls)
				{
					if (attributedVisibleControl != null)
					{
						if (state.visibleControls.Contains(attributedVisibleControl))
						{
							if (!state.expressionVisibledObjects.ContainsKey(attributedVisibleControl))
							{
								attributedVisibleControl.Visible = true;
							}
							else if (CheckExpression(state.expressionVisibledObjects[attributedVisibleControl]))
							{
								attributedVisibleControl.Visible = true;
							}
							else
							{
								attributedVisibleControl.Visible = false;
							}
						}
						else
						{
							attributedVisibleControl.Visible = false;
						}
					}
				}
				foreach (MenuItem attributedMenuItem in attributedMenuItems)
				{
					if (attributedMenuItem != null)
					{
						if (state.enabledMenuItems.Contains(attributedMenuItem))
						{
							if (!state.expressionEnabledObjects.ContainsKey(attributedMenuItem))
							{
								attributedMenuItem.Enabled = true;
							}
							else if (CheckExpression(state.expressionEnabledObjects[attributedMenuItem]))
							{
								attributedMenuItem.Enabled = true;
							}
							else
							{
								attributedMenuItem.Enabled = false;
							}
						}
						else
						{
							attributedMenuItem.Enabled = false;
						}
					}
				}
				foreach (ToolStripItem attributedToolStripItem in attributedToolStripItems)
				{
					if (attributedToolStripItem != null)
					{
						if (state.enabledToolStripItems.Contains(attributedToolStripItem))
						{
							if (!state.expressionEnabledObjects.ContainsKey(attributedToolStripItem))
							{
								attributedToolStripItem.Enabled = true;
							}
							else if (CheckExpression(state.expressionEnabledObjects[attributedToolStripItem]))
							{
								attributedToolStripItem.Enabled = true;
							}
							else
							{
								attributedToolStripItem.Enabled = false;
							}
						}
						else
						{
							attributedToolStripItem.Enabled = false;
						}
					}
				}
			}
		}

		private bool CheckExpression(string expression)
		{
			FieldInfo field = registeredStateObject.GetType().GetField(expression, BindingFlags.Instance | BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(bool))
			{
				return (bool)field.GetValue(registeredStateObject);
			}
			return false;
		}

		private void AdjustEventHandlers(ObjectStateWrapper state)
		{
			foreach (ObjectEventHandlerPair attributedObjectEventHandlerPair in attributedObjectEventHandlerPairs)
			{
				attributedObjectEventHandlerPair.eventInfo.RemoveEventHandler(attributedObjectEventHandlerPair.o, attributedObjectEventHandlerPair.eventHandler);
			}
			foreach (ObjectEventHandlerPair objEventHandlerPair in state.objEventHandlerPairs)
			{
				objEventHandlerPair.eventInfo.AddEventHandler(objEventHandlerPair.o, objEventHandlerPair.eventHandler);
			}
		}
	}
}
