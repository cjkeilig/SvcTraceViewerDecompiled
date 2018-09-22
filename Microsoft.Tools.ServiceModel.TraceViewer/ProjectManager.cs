using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class ProjectManager
	{
		public delegate void ProjectChanged(string projectName);

		private string currentProjectFilePath;

		private string messageBoxTitle;

		private TraceDataSource dataSource;

		private List<string> openedFilePaths = new List<string>();

		private IUserInterfaceProvider userIP;

		public ProjectChanged ProjectChangedCallback;

		private bool IsProjectChanged
		{
			get
			{
				if (string.IsNullOrEmpty(CurrentProjectFilePath))
				{
					return false;
				}
				if (dataSource != null)
				{
					if (openedFilePaths.Count != dataSource.LoadedFileNames.Count)
					{
						return true;
					}
					foreach (string loadedFileName in dataSource.LoadedFileNames)
					{
						if (!openedFilePaths.Contains(loadedFileName))
						{
							return true;
						}
					}
				}
				else if (openedFilePaths.Count != 0)
				{
					return true;
				}
				return false;
			}
		}

		public string CurrentProjectFilePath => currentProjectFilePath;

		public List<string> OpenedFilePaths => openedFilePaths;

		public ProjectManager(TraceViewerForm parent)
		{
			parent.DataSourceChangedHandler = (TraceViewerForm.DataSourceChanged)Delegate.Combine(parent.DataSourceChangedHandler, new TraceViewerForm.DataSourceChanged(DataSource_OnChanged));
			userIP = parent.GetInterfaceProvider();
		}

		private void DataSource_OnChanged(TraceDataSource dataSource)
		{
			this.dataSource = dataSource;
		}

		private void ProjectNameChangedHelper()
		{
			try
			{
				ProjectChangedCallback(CurrentProjectFilePath);
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
		}

		public bool CloseProject()
		{
			if (IsProjectChanged)
			{
				switch (userIP.ShowMessageBox(SR.GetString("PrjMgr_SaveMsg"), messageBoxTitle, MessageBoxIcon.Question, MessageBoxButtons.YesNoCancel))
				{
				case DialogResult.Yes:
					if (SaveProject())
					{
						openedFilePaths = new List<string>();
						currentProjectFilePath = null;
						ProjectNameChangedHelper();
						return true;
					}
					return false;
				case DialogResult.Cancel:
					return false;
				}
			}
			openedFilePaths = new List<string>();
			currentProjectFilePath = null;
			ProjectNameChangedHelper();
			return true;
		}

		public bool SaveProject()
		{
			if (!string.IsNullOrEmpty(CurrentProjectFilePath))
			{
				try
				{
					return SaveProjectToFile(CurrentProjectFilePath);
				}
				catch (TraceViewerException e)
				{
					ExceptionManager.ReportCommonErrorToUser(e);
					return SaveProjectAs();
				}
			}
			return SaveProjectAs();
		}

		public bool SaveProjectAs()
		{
			string filePath = null;
			using (SaveFileDialog saveFileDialog = new SaveFileDialog())
			{
				saveFileDialog.Title = SR.GetString("PrjMgr_SaveTitle");
				saveFileDialog.Filter = SR.GetString("PrjMgr_OpenFilter");
				saveFileDialog.CheckPathExists = true;
				saveFileDialog.OverwritePrompt = true;
				if (userIP.ShowDialog(saveFileDialog, null) != DialogResult.OK)
				{
					return true;
				}
				filePath = saveFileDialog.FileName;
			}
			try
			{
				if (SaveProjectToFile(filePath))
				{
					currentProjectFilePath = filePath;
					ProjectNameChangedHelper();
					return true;
				}
				return false;
			}
			catch (TraceViewerException e)
			{
				ExceptionManager.ReportCommonErrorToUser(e);
				return true;
			}
		}

		private string CanonicalizeFilePath(string e2eFilePath, string projectFilePath)
		{
			string text = e2eFilePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			if (IsAbsoluteFilePath(text))
			{
				return text;
			}
			if (text[0] == Path.DirectorySeparatorChar && !IsNetworkFilePath(projectFilePath) && projectFilePath.Length >= 2)
			{
				text = projectFilePath.Substring(0, 2) + text;
			}
			return text;
		}

		private bool IsNetworkFilePath(string filePath)
		{
			string value = new string(Path.DirectorySeparatorChar, 2);
			return filePath.StartsWith(value, StringComparison.CurrentCultureIgnoreCase);
		}

		private bool IsAbsoluteFilePath(string filePath)
		{
			string text = filePath.Trim();
			if (IsNetworkFilePath(text))
			{
				return true;
			}
			if (text.Length > 1)
			{
				if (text[1] == Path.VolumeSeparatorChar)
				{
					return char.IsLetter(text[0]);
				}
				return false;
			}
			return false;
		}

		private string GetFilePathPrefix(string filePath)
		{
			int num = filePath.LastIndexOf(Path.DirectorySeparatorChar.ToString(), StringComparison.CurrentCultureIgnoreCase);
			if (num == -1)
			{
				return string.Empty;
			}
			return filePath.Substring(0, num);
		}

		private bool IsSameDrive(string e2eFilePath, string projectFilePath)
		{
			if (e2eFilePath.Length < 2 || e2eFilePath[1] != Path.VolumeSeparatorChar || projectFilePath.Length < 2 || projectFilePath[1] != Path.VolumeSeparatorChar)
			{
				return false;
			}
			return char.ToUpper(e2eFilePath[0], CultureInfo.CurrentCulture).Equals(char.ToUpper(projectFilePath[0], CultureInfo.CurrentCulture));
		}

		private string CopyString(string s, int count)
		{
			string text = string.Empty;
			for (int i = 0; i < count; i++)
			{
				text += s;
			}
			return text;
		}

		private string GetRelativePath(string e2eFilePath, string projectFilePath)
		{
			string filePathPrefix = GetFilePathPrefix(e2eFilePath);
			string filePathPrefix2 = GetFilePathPrefix(projectFilePath);
			string fileName = Path.GetFileName(e2eFilePath);
			if (filePathPrefix.Equals(filePathPrefix2, StringComparison.CurrentCultureIgnoreCase))
			{
				return fileName;
			}
			string[] array = filePathPrefix.Split(Path.DirectorySeparatorChar);
			string[] array2 = filePathPrefix2.Split(Path.DirectorySeparatorChar);
			int i;
			for (i = 0; i < array.Length && i < array2.Length && array[i].Equals(array2[i], StringComparison.CurrentCultureIgnoreCase); i++)
			{
			}
			string s = ".." + Path.DirectorySeparatorChar.ToString();
			if (i < array2.Length && i == array.Length)
			{
				return CopyString(s, array2.Length - i) + fileName;
			}
			if (i < array2.Length && i == array2.Length)
			{
				string str = string.Empty;
				for (int j = i; j < array.Length; j++)
				{
					str = str + Path.DirectorySeparatorChar.ToString() + array[j];
				}
				return str + fileName;
			}
			string str2 = CopyString(s, array2.Length - i);
			for (int k = i; k < array.Length; k++)
			{
				str2 = str2 + array[k] + Path.DirectorySeparatorChar.ToString();
			}
			return str2 + fileName;
		}

		private string GetAbsolutePath(string e2eFilePath, string projectFilePath)
		{
			return Path.GetFullPath(Path.Combine(GetFilePathPrefix(projectFilePath) + Path.DirectorySeparatorChar.ToString(), e2eFilePath));
		}

		private bool ComposeProjectFileStream(XmlTextWriter xmlWriter, string projectFilePath)
		{
			try
			{
				xmlWriter.WriteStartElement("traceviewer_project");
				openedFilePaths = new List<string>();
				if (dataSource != null)
				{
					foreach (string loadedFileName in dataSource.LoadedFileNames)
					{
						xmlWriter.WriteStartElement("e2efile");
						string text = loadedFileName.Trim();
						string item = text;
						if (!IsNetworkFilePath(text) && !IsNetworkFilePath(projectFilePath) && IsSameDrive(projectFilePath, text))
						{
							text = GetRelativePath(text, projectFilePath);
						}
						xmlWriter.WriteAttributeString("path", text);
						xmlWriter.WriteEndElement();
						openedFilePaths.Add(item);
					}
				}
				xmlWriter.WriteEndElement();
				xmlWriter.Flush();
				return true;
			}
			catch (XmlException)
			{
				return false;
			}
		}

		private bool SaveProjectToFile(string filePath)
		{
			List<string> list = openedFilePaths;
			FileStream fileStream = null;
			XmlTextWriter xmlTextWriter = null;
			try
			{
				fileStream = Utilities.CreateFileStreamHelper(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
				xmlTextWriter = new XmlTextWriter(fileStream, Encoding.UTF8);
			}
			catch (LogFileException ex)
			{
				throw new TraceViewerException(ex.Message);
			}
			catch (ArgumentException)
			{
				throw new TraceViewerException(SR.GetString("MsgFailSavePrj") + filePath);
			}
			if (!ComposeProjectFileStream(xmlTextWriter, filePath))
			{
				openedFilePaths = list;
				return false;
			}
			try
			{
				xmlTextWriter.Close();
				PersistedSettings.SaveRecentFiles(new string[1]
				{
					filePath
				}, isProject: true);
			}
			catch (InvalidOperationException)
			{
				openedFilePaths = list;
				return false;
			}
			finally
			{
				Utilities.CloseStreamWithoutException(fileStream, isFlushStream: false);
			}
			return true;
		}

		private string[] ExtraceFilePathsFromProject(string path)
		{
			try
			{
				List<string> list = new List<string>();
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(path);
				foreach (XmlNode childNode in xmlDocument.ChildNodes)
				{
					if (childNode.Name == "traceviewer_project")
					{
						foreach (XmlNode childNode2 in childNode.ChildNodes)
						{
							if (childNode2.Name == "e2efile" && childNode2.Attributes["path"] != null)
							{
								string text = CanonicalizeFilePath(childNode2.Attributes["path"].Value.Trim(), path);
								if (!IsAbsoluteFilePath(text))
								{
									text = GetAbsolutePath(text, path);
								}
								text = text.ToLower(CultureInfo.CurrentCulture);
								if (!list.Contains(text))
								{
									list.Add(text);
								}
							}
						}
					}
				}
				currentProjectFilePath = path;
				string[] array = new string[list.Count];
				list.CopyTo(array);
				openedFilePaths = list;
				ProjectNameChangedHelper();
				return array;
			}
			catch (XmlException ex)
			{
				ExceptionManager.ReportCommonErrorToUser(new TraceViewerException(SR.GetString("PrjMgr_FailOpen") + ex.Message));
				return null;
			}
			catch (Exception ex2)
			{
				ExceptionManager.ReportCommonErrorToUser(new TraceViewerException(SR.GetString("PrjMgr_FailOpen") + ex2.Message));
				return null;
			}
		}

		public string[] OpenProject(string projectPath)
		{
			if (!string.IsNullOrEmpty(projectPath) && projectPath.EndsWith(SR.GetString("PJ_Extension"), StringComparison.OrdinalIgnoreCase))
			{
				if (!CloseProject())
				{
					return null;
				}
				PersistedSettings.SaveRecentFiles(new string[1]
				{
					projectPath
				}, isProject: true);
				return ExtraceFilePathsFromProject(projectPath);
			}
			return null;
		}

		public string[] OpenProject()
		{
			string text = null;
			using (OpenFileDialog openFileDialog = new OpenFileDialog())
			{
				openFileDialog.CheckFileExists = true;
				openFileDialog.CheckPathExists = true;
				openFileDialog.ValidateNames = true;
				openFileDialog.Title = SR.GetString("PrjMgr_OpenTitle");
				openFileDialog.Filter = SR.GetString("PrjMgr_OpenFilter");
				openFileDialog.FilterIndex = 0;
				openFileDialog.Multiselect = false;
				if (userIP.ShowDialog(openFileDialog, null) != DialogResult.OK)
				{
					return null;
				}
				text = openFileDialog.FileName;
			}
			if (!CloseProject())
			{
				return null;
			}
			currentProjectFilePath = null;
			PersistedSettings.SaveRecentFiles(new string[1]
			{
				text
			}, isProject: true);
			openedFilePaths = new List<string>();
			if (!string.IsNullOrEmpty(text) && text.EndsWith(SR.GetString("PJ_Extension"), StringComparison.OrdinalIgnoreCase))
			{
				return ExtraceFilePathsFromProject(text);
			}
			return null;
		}
	}
}
