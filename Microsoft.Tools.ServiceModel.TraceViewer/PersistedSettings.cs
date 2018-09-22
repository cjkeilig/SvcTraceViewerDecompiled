using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal static class PersistedSettings
	{
		private const int MAX_RECENT_FILE_COUNT = 10;

		private const string RecentFileAndProjectRegistryPath = "Software\\Microsoft\\ServiceModel\\TraceViewer\\Recent";

		private const string RecentFindStringRegistryPath = "Software\\Microsoft\\ServiceModel\\TraceViewer\\RecentFind";

		public static Queue<string> LoadRecentFiles(bool isProject)
		{
			Queue<string> queue = new Queue<string>();
			using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\ServiceModel\\TraceViewer\\Recent"))
			{
				if (registryKey == null)
				{
					return queue;
				}
				if (registryKey.ValueCount <= 0)
				{
					return queue;
				}
				string[] valueNames = registryKey.GetValueNames();
				int num = 0;
				while (true)
				{
					if (num >= valueNames.Length)
					{
						return queue;
					}
					string name = valueNames[num];
					if (queue.Count > 10)
					{
						break;
					}
					if (!isProject && !registryKey.GetValue(name).ToString().EndsWith(SR.GetString("PJ_Extension"), StringComparison.OrdinalIgnoreCase) && !queue.Contains(registryKey.GetValue(name).ToString()))
					{
						queue.Enqueue(registryKey.GetValue(name).ToString());
					}
					else if (isProject && registryKey.GetValue(name).ToString().EndsWith(SR.GetString("PJ_Extension"), StringComparison.OrdinalIgnoreCase) && !queue.Contains(registryKey.GetValue(name).ToString()))
					{
						queue.Enqueue(registryKey.GetValue(name).ToString());
					}
					num++;
				}
				return queue;
			}
		}

		public static string ParseFileName(string filePath)
		{
			string text = filePath;
			if (!string.IsNullOrEmpty(text) && text.Contains("&"))
			{
				text = text.Replace("&", "&&");
			}
			return text;
		}

		public static void SaveRecentFiles(string[] fileNames, bool isProject)
		{
			Queue<string> queue = isProject ? LoadRecentFiles(isProject: true) : LoadRecentFiles(isProject: false);
			Queue<string> queue2 = new Queue<string>();
			List<string> list = new List<string>();
			list.AddRange(fileNames);
			while (queue.Count != 0)
			{
				string item = queue.Dequeue();
				if (!list.Contains(item))
				{
					queue2.Enqueue(item);
				}
			}
			foreach (string item2 in fileNames)
			{
				if (queue2.Count >= 10)
				{
					queue2.Dequeue();
				}
				queue2.Enqueue(item2);
			}
			SaveRecentFiles(queue2, isProject);
		}

		private static void SaveRecentFiles(Queue<string> fileNames, bool isProject)
		{
			using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\ServiceModel\\TraceViewer\\Recent"))
			{
				string[] valueNames = registryKey.GetValueNames();
				foreach (string name in valueNames)
				{
					if ((!isProject && !registryKey.GetValue(name).ToString().EndsWith(SR.GetString("PJ_Extension"), StringComparison.OrdinalIgnoreCase)) || (isProject && registryKey.GetValue(name).ToString().EndsWith(SR.GetString("PJ_Extension"), StringComparison.OrdinalIgnoreCase)))
					{
						registryKey.DeleteValue(name, throwOnMissingValue: false);
					}
				}
				int num = (!isProject) ? 1 : 11;
				while (fileNames.Count != 0)
				{
					string value = fileNames.Dequeue();
					RegistryKey registryKey2 = registryKey;
					int i = num;
					num = i + 1;
					registryKey2.SetValue(i.ToString(CultureInfo.CurrentCulture), value);
				}
			}
		}

		public static LinkedList<string> LoadRecentFindStringList()
		{
			LinkedList<string> linkedList = new LinkedList<string>();
			using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\ServiceModel\\TraceViewer\\RecentFind"))
			{
				if (registryKey == null)
				{
					return linkedList;
				}
				if (registryKey.ValueCount <= 0)
				{
					return linkedList;
				}
				string[] valueNames = registryKey.GetValueNames();
				foreach (string name in valueNames)
				{
					string text = registryKey.GetValue(name).ToString();
					if (!string.IsNullOrEmpty(text) && text.Length <= 500 && !linkedList.Contains(text) && linkedList.Count <= 50)
					{
						linkedList.AddLast(text);
					}
				}
				return linkedList;
			}
		}

		public static void SaveRecentFindStringList(LinkedList<string> list)
		{
			if (list != null)
			{
				using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\ServiceModel\\TraceViewer\\RecentFind"))
				{
					string[] valueNames = registryKey.GetValueNames();
					foreach (string name in valueNames)
					{
						registryKey.DeleteValue(name, throwOnMissingValue: false);
					}
					int count = list.Count;
					for (int j = 0; j < count; j++)
					{
						registryKey.SetValue(j.ToString(CultureInfo.CurrentCulture), list.First.Value);
						list.RemoveFirst();
					}
				}
			}
		}
	}
}
