using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class FindDialog : Form
	{
		private enum TabFocusIndex
		{
			FindNextButton,
			CancelButton,
			FindWhatLabel,
			FindWhatText,
			LookInLabel,
			LookInList,
			FindTargetLabel,
			FindTargetList,
			OptionGroup,
			MatchCaseOptionButton,
			MatchWholeWordOptionButton,
			IgnoreRootOptionButton
		}

		private TraceViewerForm parentForm;

		private bool isShown;

		private bool shouldSuppressEnterForFind;

		private const int FIND_IN_ALL_ACTIVITIES_INDEX = 0;

		private const int FIND_IN_CURRENT_ACTIVITY_INDEX = 1;

		private FindCriteria currentFindCriteria;

		private IContainer components;

		private Label lblFindWhat;

		private TextBox findWhat;

		private Label lblLookIn;

		private ComboBox lookIn;

		private Label lblFindTarget;

		private ComboBox findTarget;

		private Button findNext;

		private Button cancelButton;

		private GroupBox findOptionGroup;

		private CheckBox ignoreRootActivity;

		private CheckBox matchCase;

		private CheckBox matchWholeWord;

		public bool IsShown => isShown;

		internal FindCriteria CurrentFindCriteria => currentFindCriteria;

		public FindDialog()
		{
			InitializeComponent();
		}

		public void Initialize(TraceViewerForm parent)
		{
			parentForm = parent;
			TraceViewerForm traceViewerForm = parentForm;
			traceViewerForm.FindTextChangedCallback = (TraceViewerForm.FindTextChanged)Delegate.Combine(traceViewerForm.FindTextChangedCallback, new TraceViewerForm.FindTextChanged(TraceViewerForm_FindTextChanged));
		}

		private void TraceViewerForm_FindTextChanged(string newText)
		{
			findWhat.Text = newText;
		}

		private void FindDialog_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing)
			{
				e.Cancel = true;
				Hide();
			}
		}

		private bool IsIdenticalFindCriteria(FindCriteria criteria)
		{
			if (criteria.FindingText != findWhat.Text || criteria.Scope != GetFindingScope() || criteria.Target != GetFindingTarget() || criteria.Options != GetFindingOptions())
			{
				return false;
			}
			return true;
		}

		private void lookIn_SelectedIndexChanged(object sender, EventArgs e)
		{
			FindingScope scope = (lookIn.SelectedIndex == 0) ? FindingScope.CurrentLoadedActivities : FindingScope.CurrentLoadedTraces;
			if (parentForm != null)
			{
				parentForm.SyncFindingOptions(scope, this);
			}
		}

		private FindingScope GetFindingScope()
		{
			switch (lookIn.SelectedIndex)
			{
			case 0:
				return FindingScope.CurrentLoadedActivities;
			case 1:
				return FindingScope.CurrentLoadedTraces;
			default:
				return FindingScope.CurrentLoadedTraces;
			}
		}

		internal void UpdateFindOptions(FindingScope scope)
		{
			switch (scope)
			{
			case FindingScope.CurrentLoadedActivities:
				lookIn.SelectedIndex = 0;
				break;
			case FindingScope.CurrentLoadedTraces:
				lookIn.SelectedIndex = 1;
				break;
			}
		}

		private FindingOptions GetFindingOptions()
		{
			FindingOptions findingOptions = FindingOptions.None;
			if (matchCase.Checked)
			{
				findingOptions |= FindingOptions.MatchCase;
			}
			if (ignoreRootActivity.Checked)
			{
				findingOptions |= FindingOptions.IgnoreRootActivity;
			}
			if (matchWholeWord.Checked)
			{
				findingOptions |= FindingOptions.MatchWholeWord;
			}
			return findingOptions;
		}

		private FindingTarget GetFindingTarget()
		{
			switch (findTarget.SelectedIndex)
			{
			case 0:
				return FindingTarget.RawData;
			case 1:
				return FindingTarget.XmlTagValue;
			case 2:
				return FindingTarget.XmlTagAttribute;
			case 3:
				return FindingTarget.LoggedMessage;
			default:
				return FindingTarget.RawData;
			}
		}

		private string EscapeAllCharacters(string str)
		{
			string text = string.Empty;
			foreach (char c in str)
			{
				text += string.Format(CultureInfo.CurrentCulture, "\\u{0:X4}", new object[1]
				{
					(int)c
				});
			}
			return text;
		}

		private FindCriteria ComposeFindCriteria()
		{
			if (!string.IsNullOrEmpty(findWhat.Text))
			{
				FindCriteria findCriteria = new FindCriteria();
				findCriteria.FindingText = findWhat.Text;
				findCriteria.Scope = GetFindingScope();
				findCriteria.Target = GetFindingTarget();
				findCriteria.Options = GetFindingOptions();
				findCriteria.Token = null;
				if ((findCriteria.Options & FindingOptions.MatchWholeWord) > FindingOptions.None)
				{
					findCriteria.WholeWordRegex = new Regex("\\b" + EscapeAllCharacters(findCriteria.FindingText.Trim()) + "\\b", (RegexOptions)(0x200 | (((findCriteria.Options & FindingOptions.MatchCase) == FindingOptions.None) ? 1 : 0)));
				}
				return findCriteria;
			}
			return null;
		}

		public void FindNext()
		{
			if (currentFindCriteria == null)
			{
				currentFindCriteria = ComposeFindCriteria();
				if (currentFindCriteria != null && !parentForm.FindNextTraceRecord(currentFindCriteria))
				{
					MessageBox.Show(this, SR.GetString("Find_NoFound"), parentForm.DefaultWindowTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
					shouldSuppressEnterForFind = true;
				}
			}
			else if (IsIdenticalFindCriteria(currentFindCriteria))
			{
				if (!parentForm.FindNextTraceRecord(currentFindCriteria))
				{
					MessageBox.Show(this, SR.GetString("Find_NoFound"), parentForm.DefaultWindowTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
					shouldSuppressEnterForFind = true;
				}
			}
			else
			{
				currentFindCriteria = ComposeFindCriteria();
				if (currentFindCriteria != null && !parentForm.FindNextTraceRecord(currentFindCriteria))
				{
					MessageBox.Show(this, SR.GetString("Find_NoFound"), parentForm.DefaultWindowTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
					shouldSuppressEnterForFind = true;
				}
			}
			Focus();
		}

		private void findWhat_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return && !string.IsNullOrEmpty(findWhat.Text) && !shouldSuppressEnterForFind)
			{
				FindNext();
			}
			else if (e.KeyCode == Keys.Return && shouldSuppressEnterForFind)
			{
				shouldSuppressEnterForFind = false;
			}
		}

		private void findNext_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(findWhat.Text))
			{
				FindNext();
				shouldSuppressEnterForFind = true;
			}
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			Hide();
		}

		private void FindDialog_Load(object sender, EventArgs e)
		{
			lookIn.SelectedIndex = 0;
			findTarget.SelectedIndex = 0;
		}

		private void findWhat_TextChanged(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(findWhat.Text))
			{
				findNext.Enabled = true;
			}
			else
			{
				findNext.Enabled = false;
			}
		}

		private void FindDialog_VisibleChanged(object sender, EventArgs e)
		{
			if (!base.Visible)
			{
				isShown = false;
				parentForm.BringToFront();
			}
			else
			{
				isShown = true;
				findWhat.Select();
				if (!string.IsNullOrEmpty(findWhat.Text))
				{
					findWhat.Select(0, findWhat.Text.Length);
				}
			}
			findWhat_TextChanged(null, null);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			lblFindWhat = new System.Windows.Forms.Label();
			findWhat = new System.Windows.Forms.TextBox();
			lblLookIn = new System.Windows.Forms.Label();
			lookIn = new System.Windows.Forms.ComboBox();
			lblFindTarget = new System.Windows.Forms.Label();
			findTarget = new System.Windows.Forms.ComboBox();
			findNext = new System.Windows.Forms.Button();
			cancelButton = new System.Windows.Forms.Button();
			findOptionGroup = new System.Windows.Forms.GroupBox();
			ignoreRootActivity = new System.Windows.Forms.CheckBox();
			matchCase = new System.Windows.Forms.CheckBox();
			matchWholeWord = new System.Windows.Forms.CheckBox();
			findOptionGroup.SuspendLayout();
			SuspendLayout();
			lblFindWhat.AutoSize = true;
			lblFindWhat.Location = new System.Drawing.Point(12, 4);
			lblFindWhat.Name = "lblFindWhat";
			lblFindWhat.Size = new System.Drawing.Size(52, 13);
			lblFindWhat.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_What");
			lblFindWhat.TabIndex = 2;
			findWhat.Location = new System.Drawing.Point(13, 20);
			findWhat.Name = "findWhat";
			findWhat.Size = new System.Drawing.Size(288, 20);
			findWhat.TabIndex = 3;
			findWhat.KeyUp += new System.Windows.Forms.KeyEventHandler(findWhat_KeyUp);
			findWhat.TextChanged += new System.EventHandler(findWhat_TextChanged);
			lblLookIn.AutoSize = true;
			lblLookIn.Location = new System.Drawing.Point(12, 48);
			lblLookIn.Name = "lblLookIn";
			lblLookIn.Size = new System.Drawing.Size(41, 13);
			lblLookIn.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_Lookin");
			lblLookIn.TabIndex = 4;
			lookIn.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			lookIn.FormattingEnabled = true;
			lookIn.Items.AddRange(new object[2]
			{
				Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_Scope1"),
				Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_Scope2")
			});
			lookIn.Location = new System.Drawing.Point(12, 64);
			lookIn.Name = "lookIn";
			lookIn.Size = new System.Drawing.Size(289, 21);
			lookIn.TabIndex = 5;
			lookIn.SelectedIndexChanged += new System.EventHandler(lookIn_SelectedIndexChanged);
			lblFindTarget.AutoSize = true;
			lblFindTarget.Location = new System.Drawing.Point(13, 92);
			lblFindTarget.Name = "lblFindTarget";
			lblFindTarget.Size = new System.Drawing.Size(56, 13);
			lblFindTarget.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_Target");
			lblFindTarget.TabIndex = 6;
			findTarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			findTarget.FormattingEnabled = true;
			findTarget.Items.AddRange(new object[4]
			{
				Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_T1"),
				Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_T2"),
				Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_T3"),
				Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_T4")
			});
			findTarget.Location = new System.Drawing.Point(14, 109);
			findTarget.Name = "findTarget";
			findTarget.Size = new System.Drawing.Size(288, 21);
			findTarget.TabIndex = 7;
			findNext.Location = new System.Drawing.Point(88, 242);
			findNext.Name = "findNext";
			findNext.Size = new System.Drawing.Size(101, 23);
			findNext.TabIndex = 0;
			findNext.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_Next");
			findNext.Click += new System.EventHandler(findNext_Click);
			cancelButton.Location = new System.Drawing.Point(199, 242);
			cancelButton.Name = "cancelButton";
			cancelButton.Size = new System.Drawing.Size(101, 23);
			cancelButton.TabIndex = 1;
			cancelButton.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_Cancel");
			cancelButton.Click += new System.EventHandler(cancelButton_Click);
			findOptionGroup.Controls.Add(ignoreRootActivity);
			findOptionGroup.Controls.Add(matchCase);
			findOptionGroup.Controls.Add(matchWholeWord);
			findOptionGroup.Location = new System.Drawing.Point(12, 136);
			findOptionGroup.Name = "findOptionGroup";
			findOptionGroup.Size = new System.Drawing.Size(288, 100);
			findOptionGroup.TabIndex = 8;
			findOptionGroup.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_Options");
			ignoreRootActivity.AutoSize = true;
			ignoreRootActivity.Checked = true;
			ignoreRootActivity.CheckState = System.Windows.Forms.CheckState.Checked;
			ignoreRootActivity.Location = new System.Drawing.Point(7, 66);
			ignoreRootActivity.Name = "ignoreRootActivity";
			ignoreRootActivity.Size = new System.Drawing.Size(109, 17);
			ignoreRootActivity.TabIndex = 11;
			ignoreRootActivity.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_O1");
			matchCase.AutoSize = true;
			matchCase.Location = new System.Drawing.Point(7, 20);
			matchCase.Name = "matchCase";
			matchCase.Size = new System.Drawing.Size(78, 17);
			matchCase.TabIndex = 9;
			matchCase.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_O2");
			matchWholeWord.AutoSize = true;
			matchWholeWord.Location = new System.Drawing.Point(7, 43);
			matchWholeWord.Name = "matchWholeWord";
			matchWholeWord.Size = new System.Drawing.Size(100, 17);
			matchWholeWord.TabIndex = 10;
			matchWholeWord.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_O3");
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(314, 274);
			base.CancelButton = cancelButton;
			base.AcceptButton = findNext;
			base.Controls.Add(findOptionGroup);
			base.Controls.Add(findNext);
			base.Controls.Add(cancelButton);
			base.Controls.Add(findTarget);
			base.Controls.Add(lblFindTarget);
			base.Controls.Add(lookIn);
			base.Controls.Add(lblLookIn);
			base.Controls.Add(findWhat);
			base.Controls.Add(lblFindWhat);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "FindDialog";
			base.ShowInTaskbar = false;
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("Find_Title");
			base.Load += new System.EventHandler(FindDialog_Load);
			base.VisibleChanged += new System.EventHandler(FindDialog_VisibleChanged);
			base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(FindDialog_FormClosing);
			findOptionGroup.ResumeLayout(performLayout: false);
			findOptionGroup.PerformLayout();
			ResumeLayout(performLayout: false);
			PerformLayout();
		}
	}
}
