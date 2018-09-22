using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	[ObjectStateMachine(typeof(EmptyProjectState), true, null)]
	[ObjectStateMachine(typeof(BusyProjectState), false, typeof(IdleProjectState))]
	[ObjectStateMachine(typeof(IdleProjectState), false, typeof(BusyProjectState))]
	[ObjectStateMachine(typeof(NoFileProjectState), false, typeof(IdleProjectState))]
	[ObjectStateTransfer("TraceLoadingProjectState", "BusyProjectState")]
	[ObjectStateTransfer("FileLoadingProjectState", "BusyProjectState")]
	[ObjectStateTransfer("FileRemovingProjectState", "BusyProjectState")]
	[ObjectStateTransfer("FileReloadingProjectState", "BusyProjectState")]
	internal class ProjectTreeViewControl : UserControl, IStateAwareObject
	{
		private StateMachineController objectStateController;

		private EventHandler addFileMenuItemClick;

		private EventHandler closeAllMenuItemClick;

		private EventHandler openProjectMenuItemClick;

		private EventHandler saveProjectMenuItemClick;

		private EventHandler saveProjectAsMenuItemClick;

		private EventHandler closeProjectMenuItemClick;

		private TraceDataSource dataSource;

		private IContainer components;

		private TreeView treeView;

		private ContextMenuStrip projectTreeRootNodeMenu;

		private ContextMenuStrip projectTreeChildNodeMenu;

		[UIToolStripItemEnablePropertyState(new string[]
		{
			"EmptyProjectState",
			"IdleProjectState",
			"NoFileProjectState"
		})]
		private ToolStripMenuItem projectTreeAddFileMenuItem;

		[UIToolStripItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private ToolStripMenuItem projectTreeRemoveFileMenuItem;

		[UIToolStripItemEnablePropertyState(new string[]
		{
			"IdleProjectState"
		})]
		private ToolStripMenuItem projectTreeCloseAllMenuItem;

		private ToolStripSeparator projectTreeSeparator;

		[UIToolStripItemEnablePropertyState(new string[]
		{
			"IdleProjectState",
			"NoFileProjectState"
		})]
		private ToolStripMenuItem projectTreeSaveProjectMenuItem;

		[UIToolStripItemEnablePropertyState(new string[]
		{
			"EmptyProjectState",
			"IdleProjectState",
			"NoFileProjectState"
		})]
		private ToolStripMenuItem projectTreeOpenProjectMenuItem;

		[UIToolStripItemEnablePropertyState(new string[]
		{
			"IdleProjectState",
			"NoFileProjectState"
		})]
		private ToolStripMenuItem projectTreeSaveProjectAsMenuItem;

		[UIToolStripItemEnablePropertyState(new string[]
		{
			"IdleProjectState",
			"NoFileProjectState"
		})]
		private ToolStripMenuItem projectTreeCloseProjectMenuItem;

		public EventHandler AddFileMenuItemClick
		{
			get
			{
				return addFileMenuItemClick;
			}
			set
			{
				addFileMenuItemClick = value;
			}
		}

		public EventHandler CloseAllMenuItemClick
		{
			get
			{
				return closeAllMenuItemClick;
			}
			set
			{
				closeAllMenuItemClick = value;
			}
		}

		public EventHandler OpenProjectMenuItemClick
		{
			get
			{
				return openProjectMenuItemClick;
			}
			set
			{
				openProjectMenuItemClick = value;
			}
		}

		public EventHandler SaveProjectMenuItemClick
		{
			get
			{
				return saveProjectMenuItemClick;
			}
			set
			{
				saveProjectMenuItemClick = value;
			}
		}

		public EventHandler SaveProjectAsMenuItemClick
		{
			get
			{
				return saveProjectAsMenuItemClick;
			}
			set
			{
				saveProjectAsMenuItemClick = value;
			}
		}

		public EventHandler CloseProjectMenuItemClick
		{
			get
			{
				return closeProjectMenuItemClick;
			}
			set
			{
				closeProjectMenuItemClick = value;
			}
		}

		void IStateAwareObject.PreStateSwitch(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		void IStateAwareObject.PostStateSwitch(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		void IStateAwareObject.StateSwitchSuccess(ObjectStateBase fromState, ObjectStateBase toState)
		{
		}

		void IStateAwareObject.StateSwitchFailed(ObjectStateBase fromState, ObjectStateBase toState, ObjectStateSwitchFailReason reason)
		{
		}

		public ProjectTreeViewControl()
		{
			InitializeComponent();
			objectStateController = new StateMachineController(this);
		}

		public void OnProjectNameChange(string projectName)
		{
			if (string.IsNullOrEmpty(projectName))
			{
				treeView.Nodes[0].Text = SR.GetString("PrjView_NoPrjName");
				treeView.Nodes[0].ToolTipText = SR.GetString("PrjView_NoPrjName");
			}
			else
			{
				treeView.Nodes[0].Text = SR.GetString("PrjView_PrjViewHeader") + Path.GetFileName(projectName);
				treeView.Nodes[0].ToolTipText = projectName;
			}
		}

		internal void Initialize(TraceViewerForm parent)
		{
			parent.DataSourceChangedHandler = (TraceViewerForm.DataSourceChanged)Delegate.Combine(parent.DataSourceChangedHandler, new TraceViewerForm.DataSourceChanged(DataSource_OnChanged));
			parent.ObjectStateController.RegisterStateSwitchListener(objectStateController);
		}

		private void DataSource_OnChanged(TraceDataSource dataSource)
		{
			this.dataSource = dataSource;
			if (this.dataSource != null)
			{
				TraceDataSource traceDataSource = this.dataSource;
				traceDataSource.AppendFileBeginCallback = (TraceDataSource.AppendFileBegin)Delegate.Combine(traceDataSource.AppendFileBeginCallback, new TraceDataSource.AppendFileBegin(DataSource_OnAppendFileBegin));
				traceDataSource = this.dataSource;
				traceDataSource.AppendFileFinishedCallback = (TraceDataSource.AppendFileFinished)Delegate.Combine(traceDataSource.AppendFileFinishedCallback, new TraceDataSource.AppendFileFinished(DataSource_OnAppendFileFinished));
				traceDataSource = this.dataSource;
				traceDataSource.RemoveFileFinishedCallback = (TraceDataSource.RemoveFileFinished)Delegate.Combine(traceDataSource.RemoveFileFinishedCallback, new TraceDataSource.RemoveFileFinished(DataSource_OnRemoveFileFinished));
				traceDataSource = this.dataSource;
				traceDataSource.RemoveAllFileFinishedCallback = (TraceDataSource.RemoveAllFileFinished)Delegate.Combine(traceDataSource.RemoveAllFileFinishedCallback, new TraceDataSource.RemoveAllFileFinished(DataSource_OnRemoveAllFileFinished));
			}
		}

		private void DataSource_OnRemoveAllFileFinished()
		{
			RefreshFileTree(null);
		}

		private void DataSource_OnRemoveFileFinished(string[] fileNames)
		{
			string[] array = new string[dataSource.LoadedFileNames.Count];
			dataSource.LoadedFileNames.CopyTo(array);
			RefreshFileTree(array);
		}

		private void RefreshFileTree(string[] fileNames)
		{
			treeView.Nodes[0].Nodes.Clear();
			if (fileNames != null && fileNames.Length != 0)
			{
				foreach (string text in fileNames)
				{
					TreeNode treeNode = new TreeNode();
					treeNode.ContextMenuStrip = projectTreeChildNodeMenu;
					treeNode.Text = Path.GetFileName(text);
					treeNode.ToolTipText = text;
					treeView.Nodes[0].Nodes.Add(treeNode);
				}
				treeView.Nodes[0].Expand();
			}
		}

		private void DataSource_OnAppendFileFinished(string[] fileNames, TaskInfoBase task)
		{
			string[] array = new string[dataSource.LoadedFileNames.Count];
			dataSource.LoadedFileNames.CopyTo(array);
			RefreshFileTree(array);
		}

		private void DataSource_OnAppendFileBegin(string[] fileNames)
		{
			List<string> loadedFileNames = dataSource.LoadedFileNames;
			foreach (string item in fileNames)
			{
				loadedFileNames.Add(item);
			}
			string[] array = new string[loadedFileNames.Count];
			loadedFileNames.CopyTo(array);
			RefreshFileTree(array);
		}

		private void ProjectTreeAddFileMenuItem_Click(object sender, EventArgs e)
		{
			if (AddFileMenuItemClick != null)
			{
				AddFileMenuItemClick(sender, e);
			}
		}

		private void ProjectTreeRemoveFileMenuItem_Click(object sender, EventArgs e)
		{
			if (dataSource != null && treeView.SelectedNode != null && treeView.SelectedNode != treeView.Nodes[0])
			{
				dataSource.RemoveFiles(new string[1]
				{
					treeView.SelectedNode.ToolTipText
				});
			}
		}

		private void treeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Node != null)
			{
				treeView.SelectedNode = e.Node;
			}
		}

		private void ProjectTreeRemoveAllMenuItem_Click(object sender, EventArgs e)
		{
			if (CloseAllMenuItemClick != null)
			{
				CloseAllMenuItemClick(sender, e);
			}
		}

		private void projectTreeOpenProjectMenuItem_Click(object sender, EventArgs e)
		{
			if (OpenProjectMenuItemClick != null)
			{
				OpenProjectMenuItemClick(sender, e);
			}
		}

		private void projectTreeSaveProjectMenuItem_Click(object sender, EventArgs e)
		{
			if (SaveProjectMenuItemClick != null)
			{
				SaveProjectMenuItemClick(sender, e);
			}
		}

		private void projectTreeSaveProjectAsMenuItem_Click(object sender, EventArgs e)
		{
			if (SaveProjectAsMenuItemClick != null)
			{
				SaveProjectAsMenuItemClick(sender, e);
			}
		}

		private void projectTreeCloseProjectMenuItem_Click(object sender, EventArgs e)
		{
			if (CloseProjectMenuItemClick != null)
			{
				CloseProjectMenuItemClick(sender, e);
			}
		}

		private void treeView_KeyUp(object sender, KeyEventArgs e)
		{
			if (dataSource != null && treeView.SelectedNode != null)
			{
				if (e.KeyCode == Keys.Delete && treeView.SelectedNode != treeView.Nodes[0])
				{
					dataSource.RemoveFiles(new string[1]
					{
						treeView.SelectedNode.ToolTipText
					});
				}
				else if (e.KeyCode == Keys.C && e.Control)
				{
					Utilities.CopyTextToClipboard(treeView.SelectedNode.ToolTipText);
				}
			}
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
			components = new System.ComponentModel.Container();
			System.Windows.Forms.TreeNode treeNode = new System.Windows.Forms.TreeNode(Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("PrjView_NoPrjName"));
			treeView = new System.Windows.Forms.TreeView();
			projectTreeRootNodeMenu = new System.Windows.Forms.ContextMenuStrip(components);
			projectTreeChildNodeMenu = new System.Windows.Forms.ContextMenuStrip(components);
			projectTreeAddFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			projectTreeRemoveFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			projectTreeCloseAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			projectTreeSeparator = new System.Windows.Forms.ToolStripSeparator();
			projectTreeOpenProjectMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			projectTreeSaveProjectMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			projectTreeSaveProjectAsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			projectTreeCloseProjectMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			projectTreeRootNodeMenu.SuspendLayout();
			projectTreeChildNodeMenu.SuspendLayout();
			SuspendLayout();
			treeView.Dock = System.Windows.Forms.DockStyle.Fill;
			treeView.HideSelection = false;
			treeView.Location = new System.Drawing.Point(0, 0);
			treeView.Name = "TreeView";
			treeNode.Name = "ProjectTreeRootNode";
			treeNode.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("PrjView_NoPrjName");
			treeNode.ContextMenuStrip = projectTreeRootNodeMenu;
			treeView.Nodes.AddRange(new System.Windows.Forms.TreeNode[1]
			{
				treeNode
			});
			treeView.Scrollable = true;
			treeView.ShowNodeToolTips = true;
			treeView.Size = new System.Drawing.Size(150, 150);
			treeView.TabIndex = 0;
			treeView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(treeView_NodeMouseClick);
			treeView.KeyUp += new System.Windows.Forms.KeyEventHandler(treeView_KeyUp);
			projectTreeRootNodeMenu.Enabled = true;
			projectTreeRootNodeMenu.GripMargin = new System.Windows.Forms.Padding(2);
			projectTreeRootNodeMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[7]
			{
				projectTreeAddFileMenuItem,
				projectTreeCloseAllMenuItem,
				projectTreeSeparator,
				projectTreeOpenProjectMenuItem,
				projectTreeSaveProjectMenuItem,
				projectTreeSaveProjectAsMenuItem,
				projectTreeCloseProjectMenuItem
			});
			projectTreeRootNodeMenu.Location = new System.Drawing.Point(21, 36);
			projectTreeRootNodeMenu.Name = "projectTreeRootNodeMenu";
			projectTreeRootNodeMenu.RightToLeft = System.Windows.Forms.RightToLeft.No;
			projectTreeRootNodeMenu.Size = new System.Drawing.Size(153, 139);
			projectTreeRootNodeMenu.Visible = true;
			projectTreeChildNodeMenu.Enabled = true;
			projectTreeChildNodeMenu.GripMargin = new System.Windows.Forms.Padding(2);
			projectTreeChildNodeMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[1]
			{
				projectTreeRemoveFileMenuItem
			});
			projectTreeChildNodeMenu.Location = new System.Drawing.Point(21, 36);
			projectTreeChildNodeMenu.Name = "projectTreeChildNodeMenu";
			projectTreeChildNodeMenu.RightToLeft = System.Windows.Forms.RightToLeft.No;
			projectTreeChildNodeMenu.Size = new System.Drawing.Size(153, 139);
			projectTreeChildNodeMenu.Visible = true;
			projectTreeAddFileMenuItem.Name = "ProjectTreeAddFileMenuItem";
			projectTreeAddFileMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("TxtMenuAddFile");
			projectTreeAddFileMenuItem.Click += new System.EventHandler(ProjectTreeAddFileMenuItem_Click);
			projectTreeRemoveFileMenuItem.Name = "ProjectTreeRemoveFileMenuItem";
			projectTreeRemoveFileMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("TxtMenuRemoveFile");
			projectTreeRemoveFileMenuItem.Click += new System.EventHandler(ProjectTreeRemoveFileMenuItem_Click);
			projectTreeCloseAllMenuItem.Name = "ProjectTreeRemoveAllMenuItem";
			projectTreeCloseAllMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("TxtMenuRemoveAllFiles");
			projectTreeCloseAllMenuItem.Click += new System.EventHandler(ProjectTreeRemoveAllMenuItem_Click);
			projectTreeSeparator.Name = "ProjectTreeSeparator";
			projectTreeOpenProjectMenuItem.Name = "ProjectTreeOpenProjectMenuItem";
			projectTreeOpenProjectMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("TxtMenuOpenProject");
			projectTreeOpenProjectMenuItem.Click += new System.EventHandler(projectTreeOpenProjectMenuItem_Click);
			projectTreeSaveProjectMenuItem.Name = "ProjectTreeSaveProject";
			projectTreeSaveProjectMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("TxtMenuSaveProject");
			projectTreeSaveProjectMenuItem.Click += new System.EventHandler(projectTreeSaveProjectMenuItem_Click);
			projectTreeSaveProjectAsMenuItem.Name = "ProjectTreeSaveProjectAs";
			projectTreeSaveProjectAsMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("TxtMenuSaveProjectAs");
			projectTreeSaveProjectAsMenuItem.Click += new System.EventHandler(projectTreeSaveProjectAsMenuItem_Click);
			projectTreeCloseProjectMenuItem.Name = "ProjectTreeCloseProject";
			projectTreeCloseProjectMenuItem.Text = Microsoft.Tools.ServiceModel.TraceViewer.SR.GetString("TxtMenuCloseProjectAs");
			projectTreeCloseProjectMenuItem.Click += new System.EventHandler(projectTreeCloseProjectMenuItem_Click);
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(treeView);
			base.Name = "ProjectTreeViewControl";
			projectTreeChildNodeMenu.ResumeLayout(performLayout: false);
			projectTreeRootNodeMenu.ResumeLayout(performLayout: false);
			ResumeLayout(performLayout: false);
		}
	}
}
