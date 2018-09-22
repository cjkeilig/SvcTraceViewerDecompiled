using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class AppConfigManager
	{
		private static AppConfigManager instance;

		private static string configFilePath;

		private static List<IPersistStatus> registeredPersistObjects;

		private const int RETRY_COUNT = 5;

		private const int MIN_RETRY_SLEEP_VALUE = 200;

		private const int MAX_RETRY_SLEEP_VALUE = 600;

		private CustomFilterOptionSettings customFilterOptionSettings;

		private IErrorReport errorReport;

		private IUserInterfaceProvider userIP;

		public static void RegisterPersistObject(IPersistStatus persistObject)
		{
			if (persistObject != null)
			{
				registeredPersistObjects.Add(persistObject);
			}
		}

		static AppConfigManager()
		{
			instance = null;
			configFilePath = null;
			registeredPersistObjects = new List<IPersistStatus>();
			try
			{
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				configFilePath = (folderPath.EndsWith(new string(new char[1]
				{
					Path.DirectorySeparatorChar
				}), StringComparison.CurrentCultureIgnoreCase) ? (folderPath + Path.GetFileName(Assembly.GetExecutingAssembly().CodeBase) + ".settings") : (folderPath + Path.DirectorySeparatorChar.ToString() + Path.GetFileName(Assembly.GetExecutingAssembly().CodeBase) + ".settings"));
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
				configFilePath = null;
			}
		}

		private AppConfigManager(IErrorReport errorReport, IUserInterfaceProvider userIP)
		{
			try
			{
				this.errorReport = errorReport;
				this.userIP = userIP;
				XmlNode xmlNode = InternalLoadSettingNodeFromAppConfigFile("filterOptions");
				if (xmlNode == null)
				{
					customFilterOptionSettings = new CustomFilterOptionSettings();
				}
				else
				{
					customFilterOptionSettings = new CustomFilterOptionSettings(xmlNode);
				}
			}
			catch (LogFileException)
			{
				customFilterOptionSettings = new CustomFilterOptionSettings();
			}
		}

		public static AppConfigManager GetInstance(IErrorReport errorReport, IUserInterfaceProvider userIP)
		{
			if (instance == null)
			{
				instance = new AppConfigManager(errorReport, userIP);
			}
			return instance;
		}

		public CustomFilterOptionSettings LoadCustomFilterOptionSettings()
		{
			return customFilterOptionSettings;
		}

		private XmlNode InternalLoadSettingNodeFromAppConfigFile(string nodeName)
		{
			if (!string.IsNullOrEmpty(configFilePath))
			{
				if (Utilities.CreateFileInfoHelper(configFilePath).Exists)
				{
					FileStream fileStream = Utilities.CreateFileStreamHelper(configFilePath);
					try
					{
						XmlDocument xmlDocument = new XmlDocument();
						xmlDocument.Load(fileStream);
						XmlNode documentElement = xmlDocument.DocumentElement;
						if (documentElement != null)
						{
							foreach (XmlNode childNode in documentElement.ChildNodes)
							{
								if (childNode != null && childNode.Name == nodeName)
								{
									return childNode;
								}
							}
						}
						return null;
					}
					catch (XmlException)
					{
						return null;
					}
					finally
					{
						Utilities.CloseStreamWithoutException(fileStream, isFlushStream: false);
					}
				}
				return null;
			}
			return null;
		}

		public CustomFilterManager LoadCustomFilterManager()
		{
			try
			{
				XmlNode xmlNode = InternalLoadSettingNodeFromAppConfigFile("customFilters");
				if (xmlNode == null)
				{
					return new CustomFilterManager(this, null, errorReport);
				}
				return new CustomFilterManager(this, xmlNode, errorReport);
			}
			catch (LogFileException)
			{
				return new CustomFilterManager(this, null, errorReport);
			}
		}

		public bool UpdateConfigFile()
		{
			return UpdateConfigFile(LoadCustomFilterManager());
		}

		public void RestoreUISettings()
		{
			try
			{
				XmlNode node = InternalLoadSettingNodeFromAppConfigFile("uiSettings");
				InternalExtractRegisteredObjectSettings(node);
			}
			catch (LogFileException)
			{
			}
		}

		public bool UpdateConfigFile(CustomFilterManager filterManager)
		{
			FileStream fileStream = null;
			int num = 5;
			bool flag = true;
			while (flag)
			{
				try
				{
					fileStream = Utilities.CreateFileStreamHelper(configFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
					if (fileStream != null)
					{
						flag = false;
					}
				}
				catch (LogFileException ex)
				{
					if (ex.InnerException == null || !(ex.InnerException is IOException) || num < 0)
					{
						switch (userIP.ShowMessageBox(SR.GetString("CF_Err10") + ex.Message, null, MessageBoxIcon.Hand, MessageBoxButtons.AbortRetryIgnore))
						{
						case DialogResult.Abort:
							return false;
						case DialogResult.Ignore:
							return true;
						case DialogResult.Retry:
							flag = true;
							break;
						}
					}
					else
					{
						flag = true;
						num--;
						Thread.Sleep(new Random((int)DateTime.Now.Ticks).Next(200, 600));
					}
				}
			}
			XmlTextWriter xmlTextWriter = null;
			try
			{
				xmlTextWriter = new XmlTextWriter(fileStream, Encoding.UTF8);
				xmlTextWriter.WriteStartElement("appSettings");
				xmlTextWriter.WriteStartElement("customFilters");
				filterManager.UpdateCurrentFilters(xmlTextWriter);
				xmlTextWriter.WriteEndElement();
				xmlTextWriter.WriteStartElement("filterOptions");
				customFilterOptionSettings.OutputToStream(xmlTextWriter);
				xmlTextWriter.WriteEndElement();
				xmlTextWriter.WriteStartElement("uiSettings");
				InternalPersisitRegisteredObjects(xmlTextWriter);
				xmlTextWriter.WriteEndElement();
				xmlTextWriter.WriteEndElement();
				xmlTextWriter.Flush();
				return true;
			}
			catch (XmlException)
			{
				errorReport.ReportErrorToUser(SR.GetString("CF_Err10"));
				return true;
			}
			finally
			{
				Utilities.CloseStreamWithoutException(fileStream, isFlushStream: false);
			}
		}

		private void InternalExtractRegisteredObjectSettings(XmlNode node)
		{
			if (node != null && node.HasChildNodes)
			{
				foreach (XmlNode childNode in node.ChildNodes)
				{
					foreach (IPersistStatus registeredPersistObject in registeredPersistObjects)
					{
						if (registeredPersistObject != null && registeredPersistObject.IsCurrentPersistNode(childNode))
						{
							registeredPersistObject.RestoreFromXMLNode(childNode);
						}
					}
				}
			}
		}

		private void InternalPersisitRegisteredObjects(XmlTextWriter writer)
		{
			if (writer != null)
			{
				bool flag = false;
				foreach (IPersistStatus registeredPersistObject in registeredPersistObjects)
				{
					if (registeredPersistObject != null)
					{
						try
						{
							registeredPersistObject.OutputToStream(writer);
						}
						catch (AppSettingsException)
						{
							flag = true;
						}
					}
				}
				if (flag)
				{
					errorReport.ReportErrorToUser(SR.GetString("MsgAppSettingSaveError"));
				}
			}
		}
	}
}
