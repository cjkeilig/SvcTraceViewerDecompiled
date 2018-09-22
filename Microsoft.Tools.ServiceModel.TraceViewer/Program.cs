using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Threading;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal static class Program
	{
		private const string e2eFileExtension = ".svclog";

		private const string projectFileExtension = ".stvproj";

		private const string e2eFileRegistryType = "ServiceModel.TraceViewer";

		private const string projectFileRegistryType = "ServiceModel.TraceViewerProject";

		[STAThread]
		private static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.ThreadException += Application_ThreadException;
			if (args.Length == 1 && args[0].ToLower(CultureInfo.CurrentCulture).Trim() == "/register")
			{
				RegisterFileAssociation();
			}
			else if (args.Length == 1 && args[0].ToLower(CultureInfo.CurrentCulture).Trim() == "/unregister")
			{
				UnregisterFileAssociation();
			}
			else if (args.Length == 1 && (args[0].ToLower(CultureInfo.CurrentCulture).Trim() == "/?" || args[0].ToLower(CultureInfo.CurrentCulture).Trim() == "/help"))
			{
				MessageBox.Show(null, SR.GetString("MsgCmdLineInfo_Body"), SR.GetString("DlgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
				Application.Exit();
			}
			else
			{
				string[] args2 = PrepareFiles(args);
				Application.SetCompatibleTextRenderingDefault(defaultValue: false);
				Application.Run(new TraceViewerForm(args2));
			}
		}

		private static string[] PrepareFiles(string[] args)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (string text in args)
			{
				if (!string.IsNullOrEmpty(text))
				{
					string text2 = text.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
					string[] array = null;
					string text3 = null;
					string text4 = null;
					try
					{
						text3 = Directory.GetCurrentDirectory();
						text4 = text2;
						int num = text2.LastIndexOf(Path.DirectorySeparatorChar);
						if (num != -1)
						{
							text3 = text2.Substring(0, num);
							text4 = text2.Substring(num + 1);
						}
						array = Directory.GetFileSystemEntries(text3, text4);
					}
					catch (UnauthorizedAccessException)
					{
						MessageBox.Show(null, SR.GetString("MsgSearchPatternAccessDenied") + text, SR.GetString("DlgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
						continue;
					}
					catch (DirectoryNotFoundException)
					{
						MessageBox.Show(null, SR.GetString("MsgSearchPatternDirectoryNotFound") + text3, SR.GetString("DlgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
						continue;
					}
					catch (IOException)
					{
						MessageBox.Show(null, SR.GetString("MsgSearchPatternPathIsFile") + text3, SR.GetString("DlgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
						continue;
					}
					catch (Exception)
					{
						MessageBox.Show(null, SR.GetString("MsgSearchPatternNotSupported") + text, SR.GetString("DlgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
						continue;
					}
					if (array != null)
					{
						string[] array2 = array;
						for (int j = 0; j < array2.Length; j++)
						{
							string text5 = Path.GetFullPath(array2[j]).ToLower(CultureInfo.CurrentCulture);
							if (File.Exists(text5) && !dictionary.ContainsKey(text5))
							{
								dictionary.Add(text5, 0);
							}
						}
					}
				}
			}
			string[] array3 = new string[dictionary.Count];
			int num2 = 0;
			foreach (string key in dictionary.Keys)
			{
				array3[num2++] = key;
			}
			return array3;
		}

		static Program()
		{
			RuntimeHelpers.PrepareMethod(typeof(Program).GetMethod("Application_ThreadException", BindingFlags.Static | BindingFlags.NonPublic).MethodHandle);
			RuntimeHelpers.PrepareMethod(typeof(Program).GetMethod("GlobalSeriousExceptionHandler", BindingFlags.Static | BindingFlags.Public).MethodHandle);
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static void GlobalSeriousExceptionHandler(Exception e)
		{
			if (e is OutOfMemoryException)
			{
				MessageBox.Show(null, SR.GetString("MsgOutOfMemory"), SR.GetString("DlgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
				Application.Exit();
			}
			string str = (e is SystemException) ? SR.GetString("UnhandledSysException") : SR.GetString("UnhandledException");
			str = str + SR.GetString("MsgReturnBack") + e.Message;
			if (e is AccessViolationException || e is StackOverflowException || e is InvalidOperationException || e is ThreadAbortException)
			{
				str = str + SR.GetString("MsgReturnBack") + SR.GetString("STVClosing");
				MessageBox.Show(null, str, SR.GetString("DlgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
				Application.Exit();
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
		{
			Exception exception = e.Exception;
			GlobalSeriousExceptionHandler(exception);
			string str = (exception is SystemException) ? SR.GetString("UnhandledSysException") : SR.GetString("UnhandledException");
			str = str + SR.GetString("MsgReturnBack") + exception.Message;
			str = str + SR.GetString("MsgReturnBack") + SR.GetString("IgnoreException");
			if (MessageBox.Show(null, str, SR.GetString("DlgTitleError"), MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0) != DialogResult.Yes)
			{
				Application.Exit();
			}
		}

		private static void UnregisterFileAssociation()
		{
			RegistryKey classesRoot = Registry.ClassesRoot;
			try
			{
				string[] subKeyNames = classesRoot.GetSubKeyNames();
				if (subKeyNames != null)
				{
					List<string> list = new List<string>();
					list.AddRange(subKeyNames);
					if (list.Contains(".svclog"))
					{
						RegistryKey registryKey = classesRoot.OpenSubKey(".svclog");
						if (registryKey != null && registryKey.GetValueKind("") == RegistryValueKind.String && registryKey.GetValue("").ToString() == "ServiceModel.TraceViewer")
						{
							classesRoot.DeleteSubKeyTree(".svclog");
						}
						registryKey.Close();
					}
					if (list.Contains(".stvproj"))
					{
						RegistryKey registryKey2 = classesRoot.OpenSubKey(".stvproj");
						if (registryKey2 != null && registryKey2.GetValueKind("") == RegistryValueKind.String && registryKey2.GetValue("").ToString() == "ServiceModel.TraceViewerProject")
						{
							classesRoot.DeleteSubKeyTree(".stvproj");
						}
						registryKey2.Close();
					}
					if (list.Contains("ServiceModel.TraceViewer"))
					{
						classesRoot.DeleteSubKeyTree("ServiceModel.TraceViewer");
					}
					if (list.Contains("ServiceModel.TraceViewerProject"))
					{
						classesRoot.DeleteSubKeyTree("ServiceModel.TraceViewerProject");
					}
				}
			}
			catch (ArgumentException)
			{
				MessageBox.Show(null, SR.GetString("UnRegisterFail"), SR.GetString("DlgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
			}
			catch (SecurityException)
			{
				MessageBox.Show(null, SR.GetString("UnRegisterFail"), SR.GetString("DlgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
			}
			catch (UnauthorizedAccessException)
			{
				MessageBox.Show(null, SR.GetString("UnRegisterFail"), SR.GetString("DlgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
			}
			finally
			{
				classesRoot?.Close();
			}
		}

		private static void RegisterFileAssociation()
		{
			RegistryKey classesRoot = Registry.ClassesRoot;
			RegistryKey registryKey = null;
			RegistryKey registryKey2 = null;
			RegistryKey registryKey3 = null;
			RegistryKey registryKey4 = null;
			RegistryKey registryKey5 = null;
			RegistryKey registryKey6 = null;
			try
			{
				classesRoot.CreateSubKey(".svclog").SetValue("", "ServiceModel.TraceViewer");
				registryKey = classesRoot.CreateSubKey("ServiceModel.TraceViewer");
				registryKey.SetValue("", "ServiceModel Trace Viewer");
				registryKey.CreateSubKey("DefaultIcon").SetValue("", Application.ExecutablePath + ",0");
				classesRoot.CreateSubKey(".stvproj").SetValue("", "ServiceModel.TraceViewerProject");
				registryKey2 = classesRoot.CreateSubKey("ServiceModel.TraceViewerProject");
				registryKey2.SetValue("", "ServiceModel Trace Viewer Project");
				registryKey2.CreateSubKey("DefaultIcon").SetValue("", Application.ExecutablePath + ",0");
				registryKey3 = registryKey.CreateSubKey("shell");
				registryKey4 = registryKey3.CreateSubKey("Open");
				registryKey4.SetValue("", "&Open");
				registryKey4.CreateSubKey("Command").SetValue("", "\"" + Application.ExecutablePath + "\" \"%1\"");
				registryKey5 = registryKey2.CreateSubKey("shell");
				registryKey6 = registryKey5.CreateSubKey("Open");
				registryKey6.SetValue("", "&Open");
				registryKey6.CreateSubKey("Command").SetValue("", "\"" + Application.ExecutablePath + "\" \"%1\"");
			}
			catch (ArgumentException)
			{
				MessageBox.Show(null, SR.GetString("RegisterFail"), SR.GetString("DlgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
			}
			catch (SecurityException)
			{
				MessageBox.Show(null, SR.GetString("RegisterFail"), SR.GetString("DlgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
			}
			catch (UnauthorizedAccessException)
			{
				MessageBox.Show(null, SR.GetString("RegisterFail"), SR.GetString("DlgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
			}
			finally
			{
				registryKey6?.Close();
				registryKey5?.Close();
				registryKey4?.Close();
				registryKey3?.Close();
				registryKey?.Close();
				registryKey2?.Close();
				classesRoot?.Close();
			}
		}
	}
}
