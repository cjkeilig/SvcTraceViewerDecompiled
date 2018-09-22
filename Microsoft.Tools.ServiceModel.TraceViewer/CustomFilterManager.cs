using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class CustomFilterManager
	{
		private AppConfigManager configManager;

		private List<CustomFilter> currentFilters;

		private IErrorReport errorReport;

		internal List<CustomFilter> CurrentFilters
		{
			get
			{
				return currentFilters;
			}
			set
			{
				currentFilters = value;
			}
		}

		public CustomFilterManager(AppConfigManager configManager, XmlNode rootNode, IErrorReport errorReport)
		{
			this.errorReport = errorReport;
			this.configManager = configManager;
			currentFilters = LoadCustomFilters(rootNode, reportErrors: false);
		}

		public bool Save()
		{
			return configManager.UpdateConfigFile(this);
		}

		private bool IsDuplicateFilterName(string filterName)
		{
			foreach (CustomFilter currentFilter in CurrentFilters)
			{
				if (currentFilter.FilterName == filterName)
				{
					return true;
				}
			}
			return false;
		}

		public void RemoveFilter(CustomFilter filter)
		{
			if (CurrentFilters.Contains(filter))
			{
				CurrentFilters.Remove(filter);
			}
		}

		public bool Import()
		{
			using (OpenFileDialog openFileDialog = new OpenFileDialog())
			{
				openFileDialog.CheckFileExists = true;
				openFileDialog.CheckPathExists = true;
				openFileDialog.ValidateNames = true;
				openFileDialog.Title = SR.GetString("CFIP_Title");
				openFileDialog.Filter = SR.GetString("CFIP_Filter");
				openFileDialog.Multiselect = false;
				if (openFileDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(openFileDialog.FileName))
				{
					FileStream fileStream = null;
					try
					{
						fileStream = Utilities.CreateFileStreamHelper(openFileDialog.FileName);
					}
					catch (LogFileException ex)
					{
						errorReport.ReportErrorToUser(SR.GetString("CF_Err11") + ex.Message);
						return false;
					}
					try
					{
						XmlDocument xmlDocument = new XmlDocument();
						xmlDocument.Load(fileStream);
						List<CustomFilter> list = null;
						foreach (XmlNode childNode in xmlDocument.ChildNodes)
						{
							if (childNode.Name == "customFilters")
							{
								list = LoadCustomFilters(childNode, reportErrors: true);
								break;
							}
						}
						if (list != null && list.Count != 0)
						{
							Random random = new Random((int)DateTime.Now.Ticks);
							foreach (CustomFilter item in list)
							{
								if (IsDuplicateFilterName(item.FilterName))
								{
									item.ChangeFilterName(item.FilterName + random.Next(0, 65535).ToString(CultureInfo.InvariantCulture));
								}
								currentFilters.Add(item);
							}
						}
					}
					catch (XmlException)
					{
						errorReport.ReportErrorToUser(SR.GetString("CF_InvalidFilterFile"));
					}
					finally
					{
						Utilities.CloseStreamWithoutException(fileStream, isFlushStream: false);
					}
					return true;
				}
				return false;
			}
		}

		public void Export(List<CustomFilter> filters)
		{
			if (filters != null && filters.Count != 0)
			{
				using (SaveFileDialog saveFileDialog = new SaveFileDialog())
				{
					saveFileDialog.Title = SR.GetString("CFEP_Title");
					saveFileDialog.Filter = SR.GetString("CFEP_Filter");
					if (saveFileDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(saveFileDialog.FileName))
					{
						FileInfo fileInfo = null;
						try
						{
							fileInfo = Utilities.CreateFileInfoHelper(saveFileDialog.FileName);
						}
						catch (LogFileException ex)
						{
							errorReport.ReportErrorToUser(ex.Message);
							return;
						}
						if (fileInfo.Exists)
						{
							try
							{
								Utilities.DeleteFileByFileInfoHelper(fileInfo);
							}
							catch (LogFileException ex2)
							{
								errorReport.ReportErrorToUser(ex2.Message);
								return;
							}
						}
						FileStream fileStream = null;
						try
						{
							fileStream = Utilities.CreateFileStreamHelper(saveFileDialog.FileName, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
						}
						catch (LogFileException ex3)
						{
							errorReport.ReportErrorToUser(SR.GetString("CF_Err12") + ex3.Message);
							return;
						}
						XmlTextWriter xmlTextWriter = new XmlTextWriter(fileStream, Encoding.UTF8);
						xmlTextWriter.WriteStartElement("customFilters");
						foreach (CustomFilter filter in filters)
						{
							filter.OutputToStream(xmlTextWriter);
						}
						xmlTextWriter.WriteEndElement();
						xmlTextWriter.Flush();
						xmlTextWriter.Close();
					}
				}
			}
		}

		public void UpdateCurrentFilters(XmlTextWriter writer)
		{
			foreach (CustomFilter currentFilter in currentFilters)
			{
				try
				{
					currentFilter.OutputToStream(writer);
				}
				catch (AppSettingsException)
				{
				}
			}
		}

		private List<CustomFilter> LoadCustomFilters(XmlNode rootNode, bool reportErrors)
		{
			List<CustomFilter> list = new List<CustomFilter>();
			if (rootNode != null && rootNode.Name == "customFilters")
			{
				foreach (XmlNode childNode in rootNode.ChildNodes)
				{
					if (childNode.Name == "filter")
					{
						try
						{
							CustomFilter item = new CustomFilter(childNode.OuterXml);
							list.Add(item);
						}
						catch (AppSettingsException)
						{
							if (reportErrors)
							{
								string str = SR.GetString("CF_NoInvalidFilterName");
								if (childNode["name"] != null && !string.IsNullOrEmpty(childNode["name"].InnerText))
								{
									str = childNode["name"].InnerText;
								}
								errorReport.ReportErrorToUser(SR.GetString("CF_InvalidFilter") + str);
							}
						}
					}
				}
				return list;
			}
			return list;
		}
	}
}
