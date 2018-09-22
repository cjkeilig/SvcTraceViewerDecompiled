using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[ObjectStateMachine(typeof(TraceFilterDisableState), true, typeof(TraceFilterEnableState))]
	[ObjectStateMachine(typeof(TraceFilterEnableState), false, typeof(TraceFilterDisableState))]
	[ObjectStateTransfer("EmptyProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("FileLoadingProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("FileRemovingProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("TraceLoadingProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("FileReloadingProjectState", "TraceFilterDisableState")]
	[ObjectStateTransfer("IdleProjectState", "TraceFilterEnableState")]
	[ObjectStateTransfer("NoFileProjectState", "TraceFilterDisableState")]
	[ObjectStateMachine(typeof(LeftPanelActivityViewState), false, typeof(LeftPanelActivityViewState), "ParentLeftPanelTableStateScope")]
	[ObjectStateMachine(typeof(LeftPanelProjectViewState), false, typeof(LeftPanelActivityViewState), "ParentLeftPanelTableStateScope")]
	[ObjectStateMachine(typeof(LeftPanelTreeViewState), false, typeof(LeftPanelActivityViewState), "ParentLeftPanelTableStateScope")]
	[ObjectStateMachine(typeof(LeftPanelMessageViewState), false, typeof(LeftPanelActivityViewState), "ParentLeftPanelTableStateScope")]
	internal class FindToolBar : UserControl, IStateAwareObject
	{
		public const int MaxFindingStringLength = 500;

		public const int MaxFindingListLength = 50;

		private const int LOOK_IN_ALL_ACTIVITY_INDEX = 0;

		private const int LOOK_IN_CURRENT_ACTIVITY_INDEX = 1;

		private StateMachineController objectStateController;

		private StateMachineController parentLeftPanelObjectStateController;

		private TraceViewerForm parentForm;

		private IUserInterfaceProvider uiProvider;

		private FindCriteria savedFindCriteria;

		private bool shouldSuppressEnterForFind;

		private LinkedList<string> recentFindStringList;

		private IContainer components;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"TraceFilterEnableState"
		}, "Enabled")]
		private ToolStrip findToolStrip;

		private ToolStripLabel lblFind;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"LeftPanelActivityViewState",
			"LeftPanelTreeViewState"
		}, "Enabled")]
		private ToolStripComboBox findWhatList;

		private ToolStripLabel lblLookin;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"LeftPanelActivityViewState"
		}, "Enabled")]
		private ToolStripComboBox lookinList;

		[FieldObjectBooleanPropertyEnableState(new string[]
		{
			"LeftPanelActivityViewState",
			"LeftPanelTreeViewState"
		}, "Enabled")]
		private ToolStripButton btnFind;

		public FindToolBar()
		{
			InitializeComponent();
			objectStateController = new StateMachineController(this);
			parentLeftPanelObjectStateController = new StateMachineController(this, "ParentLeftPanelTableStateScope");
		}

		void IStateAwareObject.PreStateSwitch(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		void IStateAwareObject.PostStateSwitch(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		void IStateAwareObject.StateSwitchSuccess(ObjectStateBase fromState, ObjectStateBase toState)
		{
			if (toState != null && (toState.StateName == "LeftPanelTreeViewState" || toState.StateName == "LeftPanelActivityViewState"))
			{
				savedFindCriteria = null;
			}
		}

		void IStateAwareObject.StateSwitchFailed(ObjectStateBase fromState, ObjectStateBase toState, ObjectStateSwitchFailReason reason)
		{
		}

		private void RefreshAutoCompleteSource()
		{
			if (recentFindStringList != null && recentFindStringList.Count != 0)
			{
				findWhatList.Items.Clear();
				for (LinkedListNode<string> linkedListNode = recentFindStringList.First; linkedListNode != null; linkedListNode = linkedListNode.Next)
				{
					findWhatList.Items.Add(linkedListNode.Value);
				}
			}
		}

		internal void UpdateFindOptions(FindingScope scope)
		{
			switch (scope)
			{
			case FindingScope.CurrentLoadedActivities:
				lookinList.SelectedIndex = 0;
				break;
			case FindingScope.CurrentLoadedTraces:
				lookinList.SelectedIndex = 1;
				break;
			}
		}

		private void lookinList_SelectedIndexChanged(object sender, EventArgs e)
		{
			FindingScope scope = (lookinList.SelectedIndex == 0) ? FindingScope.CurrentLoadedActivities : FindingScope.CurrentLoadedTraces;
			if (parentForm != null)
			{
				parentForm.SyncFindingOptions(scope, this);
			}
		}

		private void AppendRecentFindString(string findString)
		{
			if (!string.IsNullOrEmpty(findString) && recentFindStringList != null && !recentFindStringList.Contains(findString))
			{
				if (recentFindStringList.Count < 50)
				{
					recentFindStringList.AddFirst(findString);
				}
				else
				{
					recentFindStringList.AddFirst(findString);
					recentFindStringList.RemoveLast();
				}
				RefreshAutoCompleteSource();
			}
		}

		internal void Initialize(TraceViewerForm parentForm, IUserInterfaceProvider uiProvider)
		{
			this.parentForm = parentForm;
			this.uiProvider = uiProvider;
			parentForm.ObjectStateController.RegisterStateSwitchListener(objectStateController);
			parentForm.LeftPanelStateController.RegisterStateSwitchListener(parentLeftPanelObjectStateController);
			parentForm.LeftPanelStateController.SwitchState("LeftPanelActivityViewState");
			lookinList.SelectedIndex = 0;
			recentFindStringList = PersistedSettings.LoadRecentFindStringList();
			RefreshAutoCompleteSource();
			TraceViewerForm traceViewerForm = this.parentForm;
			traceViewerForm.FindTextChangedCallback = (TraceViewerForm.FindTextChanged)Delegate.Combine(traceViewerForm.FindTextChangedCallback, new TraceViewerForm.FindTextChanged(TraceViewerForm_FindTextChanged));
		}

		private void TraceViewerForm_FindTextChanged(string newText)
		{
			findWhatList.Text = newText;
		}

		private void btnFind_Click(object sender, EventArgs e)
		{
			Find();
		}

		private void findWhatList_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return && !string.IsNullOrEmpty(findWhatList.Text) && !shouldSuppressEnterForFind)
			{
				Find();
			}
			else if (e.KeyCode == Keys.Return && shouldSuppressEnterForFind)
			{
				shouldSuppressEnterForFind = false;
			}
		}

		private FindCriteria GetFindCriteria()
		{
			if (savedFindCriteria == null || (savedFindCriteria != null && (savedFindCriteria.FindingText != findWhatList.Text || savedFindCriteria.Scope != (FindingScope)((lookinList.SelectedIndex != 1) ? 1 : 0))))
			{
				FindCriteria findCriteria = new FindCriteria();
				findCriteria.FindingText = findWhatList.Text;
				findCriteria.Options = FindingOptions.None;
				findCriteria.Scope = ((lookinList.SelectedIndex != 1) ? FindingScope.CurrentLoadedActivities : FindingScope.CurrentLoadedTraces);
				findCriteria.Target = FindingTarget.RawData;
				findCriteria.Token = null;
				savedFindCriteria = findCriteria;
			}
			return savedFindCriteria;
		}

		private void Find()
		{
			if (!string.IsNullOrEmpty(findWhatList.Text) && (lookinList.SelectedIndex == 1 || lookinList.SelectedIndex == 0))
			{
				FindCriteria findCriteria = GetFindCriteria();
				AppendRecentFindString(findCriteria.FindingText);
				if (findCriteria != null)
				{
					if ((parentLeftPanelObjectStateController.CurrentStateName != "LeftPanelTreeViewState" && !parentForm.FindNextTraceRecord(findCriteria)) || (parentLeftPanelObjectStateController.CurrentStateName == "LeftPanelTreeViewState" && !parentForm.FindNextTraceRecordInSwinLanes(findCriteria)))
					{
						uiProvider.ShowMessageBox(SR.GetString("Find_NoFound"), null, MessageBoxIcon.Exclamation, MessageBoxButtons.OK);
						shouldSuppressEnterForFind = true;
					}
					findWhatList.Focus();
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			PersistedSettings.SaveRecentFindStringList(recentFindStringList);
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			findToolStrip = new System.Windows.Forms.ToolStrip();
			lblFind = new System.Windows.Forms.ToolStripLabel();
			lblLookin = new System.Windows.Forms.ToolStripLabel();
			findWhatList = new System.Windows.Forms.ToolStripComboBox();
			lookinList = new System.Windows.Forms.ToolStripComboBox();
			btnFind = new System.Windows.Forms.ToolStripButton();
			findToolStrip.SuspendLayout();
			SuspendLayout();
			findToolStrip.AutoSize = false;
			findToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[5]
			{
				lblFind,
				findWhatList,
				lblLookin,
				lookinList,
				btnFind
			});
			findToolStrip.Location = new System.Drawing.Point(0, 0);
			findToolStrip.Name = "findToolStrip";
			findToolStrip.Size = new System.Drawing.Size(800, 25);
			findToolStrip.TabIndex = 0;
			lblFind.Name = "lblFind";
			lblFind.Size = new System.Drawing.Size(58, 22);
			lblFind.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_What2");
			lblLookin.Name = "lblLookin";
			lblLookin.Size = new System.Drawing.Size(58, 22);
			lblLookin.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_Lookin2");
			findWhatList.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
			findWhatList.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			findWhatList.AutoToolTip = true;
			findWhatList.MaxDropDownItems = 50;
			findWhatList.MaxLength = 500;
			findWhatList.Name = "findWhatList";
			findWhatList.Size = new System.Drawing.Size(200, 25);
			findWhatList.KeyUp += new System.Windows.Forms.KeyEventHandler(findWhatList_KeyUp);
			findWhatList.ToolTipText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_FindWhatTip");
			lookinList.AutoSize = false;
			lookinList.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.ImageAndText;
			lookinList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			lookinList.Items.AddRange(new object[2]
			{
				Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_Scope1"),
				Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_Scope2")
			});
			lookinList.Name = "lookinList";
			lookinList.Size = new System.Drawing.Size(250, 25);
			lookinList.SelectedIndexChanged += new System.EventHandler(lookinList_SelectedIndexChanged);
			lookinList.ToolTipText = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_LookInTip");
			btnFind.AutoSize = false;
			btnFind.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			btnFind.Name = "btnFind";
			btnFind.Size = new System.Drawing.Size(64, 22);
			btnFind.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_Button");
			btnFind.Click += new System.EventHandler(btnFind_Click);
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(findToolStrip);
			base.Name = "FindToolbar";
			base.Size = new System.Drawing.Size(800, 25);
			findToolStrip.ResumeLayout(performLayout: false);
			findToolStrip.PerformLayout();
			ResumeLayout(performLayout: false);
		}
	}
}
