using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal static class Utilities
	{
		public static int GetImageIndex(Activity activity)
		{
			if (activity.ActivityType == ActivityType.RootActivity)
			{
				return TraceViewerForm.GetImageIndexFromImageList(Images.RootActivity);
			}
			if (activity.ActivityType == ActivityType.ServiceHostActivity)
			{
				return TraceViewerForm.GetImageIndexFromImageList(Images.HostActivityIcon);
			}
			if (activity.ActivityType == ActivityType.ListenActivity)
			{
				return TraceViewerForm.GetImageIndexFromImageList(Images.ListenActivity);
			}
			if (activity.ActivityType == ActivityType.MessageActivity)
			{
				return TraceViewerForm.GetImageIndexFromImageList(Images.MessageActivityIcon);
			}
			if (activity.ActivityType == ActivityType.ConnectionActivity)
			{
				return TraceViewerForm.GetImageIndexFromImageList(Images.ConnectionActivityIcon);
			}
			if (activity.ActivityType == ActivityType.UserCodeExecutionActivity)
			{
				return TraceViewerForm.GetImageIndexFromImageList(Images.ExecutionActivityIcon);
			}
			return TraceViewerForm.GetImageIndexFromImageList(Images.DefaultActivityIcon);
		}

		public static string GetFileSizeString(long size)
		{
			if (size < 0)
			{
				return SR.GetString("Utility_UNSize");
			}
			if (size < 1000)
			{
				return size.ToString(CultureInfo.CurrentCulture) + SR.GetString("TxtBytes");
			}
			if (size >= 1000 && size < 1000000)
			{
				return ((int)((double)size / 1000.0)).ToString(CultureInfo.CurrentCulture) + SR.GetString("TxtKBytes");
			}
			if (size >= 1000000)
			{
				return ((int)((double)size / 1000000.0)).ToString(CultureInfo.CurrentCulture) + SR.GetString("TxtMB");
			}
			return SR.GetString("Utility_UNSize");
		}

		public static DateTime RoundDateTimeToSecond(DateTime dateTime)
		{
			return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);
		}

		static Utilities()
		{
			MethodInfo[] methods = typeof(ExceptionManager).GetMethods();
			foreach (MethodInfo methodInfo in methods)
			{
				switch (methodInfo.Name)
				{
				case "CreateFileStreamHelper":
				case "SeekFileStreamHelper":
				case "CreateStreamReaderHelper":
				case "UIThreadInvokeHelper":
				case "CreateBinaryReaderHelper":
				case "CreateFileInfoHelper":
				case "DeleteFileByFileInfoHelper":
				case "ReadBytesHelper":
				case "CloseStreamWithoutException":
					RuntimeHelpers.PrepareMethod(methodInfo.MethodHandle);
					break;
				}
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static FileStream CreateFileStreamHelper(string filePath, FileMode mode, FileAccess access, FileShare share)
		{
			try
			{
				return new FileStream(filePath, mode, access, share);
			}
			catch (ArgumentException e)
			{
				LogFileException ex = new LogFileException(SR.GetString("MsgCannotOpenFile") + filePath + SR.GetString("MsgFilePathNotSupport"), filePath, e);
				ExceptionManager.LogAppError(ex);
				throw ex;
			}
			catch (FileNotFoundException e2)
			{
				LogFileException ex2 = new LogFileException(SR.GetString("MsgCannotOpenFile") + filePath + SR.GetString("MsgFileNotFound"), filePath, e2);
				ExceptionManager.LogAppError(ex2);
				throw ex2;
			}
			catch (SecurityException e3)
			{
				LogFileException ex3 = new LogFileException(SR.GetString("MsgCannotOpenFile") + filePath + SR.GetString("MsgAccessDenied"), filePath, e3);
				ExceptionManager.LogAppError(ex3);
				throw ex3;
			}
			catch (DirectoryNotFoundException e4)
			{
				LogFileException ex4 = new LogFileException(SR.GetString("MsgCannotOpenFile") + filePath + SR.GetString("MsgDirectoryNotFound"), filePath, e4);
				ExceptionManager.LogAppError(ex4);
				throw ex4;
			}
			catch (UnauthorizedAccessException e5)
			{
				LogFileException ex5 = new LogFileException(SR.GetString("MsgCannotOpenFile") + filePath + SR.GetString("MsgAccessDenied"), filePath, e5);
				ExceptionManager.LogAppError(ex5);
				throw ex5;
			}
			catch (PathTooLongException e6)
			{
				LogFileException ex6 = new LogFileException(SR.GetString("MsgCannotOpenFile") + filePath + SR.GetString("MsgFilePathTooLong"), filePath, e6);
				ExceptionManager.LogAppError(ex6);
				throw ex6;
			}
			catch (NotSupportedException e7)
			{
				LogFileException ex7 = new LogFileException(SR.GetString("MsgCannotOpenFile") + filePath + SR.GetString("MsgFilePathNotSupport"), filePath, e7);
				ExceptionManager.LogAppError(ex7);
				throw ex7;
			}
			catch (IOException e8)
			{
				LogFileException ex8 = new LogFileException(SR.GetString("MsgIOException") + filePath + SR.GetString("MsgFilePathEnd"), filePath, e8);
				ExceptionManager.LogAppError(ex8);
				throw ex8;
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static FileStream CreateFileStreamHelper(string filePath)
		{
			return CreateFileStreamHelper(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static void SeekFileStreamHelper(FileStream fs, long offset, SeekOrigin origin)
		{
			try
			{
				fs.Seek(offset, origin);
			}
			catch (IOException e)
			{
				LogFileException ex = new LogFileException(SR.GetString("MsgIOExceptionSeek") + fs.Name + SR.GetString("MsgFilePathEnd"), fs.Name, e);
				ExceptionManager.LogAppError(ex);
				throw ex;
			}
			catch (NotSupportedException e2)
			{
				LogFileException ex2 = new LogFileException(SR.GetString("MsgFailToSeekFile") + fs.Name + SR.GetString("MsgFilePathEnd"), fs.Name, e2);
				ExceptionManager.LogAppError(ex2);
				throw ex2;
			}
			catch (ArgumentException e3)
			{
				LogFileException ex3 = new LogFileException(SR.GetString("MsgFailToSeekFile") + fs.Name + SR.GetString("MsgFilePathEnd"), fs.Name, e3);
				ExceptionManager.LogAppError(ex3);
				throw ex3;
			}
			catch (ObjectDisposedException e4)
			{
				LogFileException ex4 = new LogFileException(SR.GetString("MsgFailToSeekFile") + fs.Name + SR.GetString("MsgFilePathEnd") + SR.GetString("MsgStreamClosed"), fs.Name, e4);
				ExceptionManager.LogAppError(ex4);
				throw ex4;
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static StreamReader CreateStreamReaderHelper(FileStream fs)
		{
			try
			{
				return new StreamReader(fs);
			}
			catch (ArgumentException e)
			{
				LogFileException ex = new LogFileException(SR.GetString("MsgFileStreamNotReadable"), fs.Name, e);
				ExceptionManager.LogAppError(ex);
				throw ex;
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static object UIThreadInvokeHelper(IUserInterfaceProvider userIP, Delegate d, params object[] props)
		{
			if (userIP != null && (object)d != null)
			{
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					if (props == null)
					{
						return userIP.InvokeOnUIThread(d);
					}
					return userIP.InvokeOnUIThread(d, props);
				}
				catch (Exception ex)
				{
					ExceptionManager.GeneralExceptionFilter(ex);
					ExceptionManager.LogAppError(new TraceViewerException(SR.GetString("MsgInvokeExceptionOccur"), ex.GetType().ToString() + SR.GetString("MsgReturnBack") + ex.ToString()));
				}
			}
			return null;
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static BinaryReader CreateBinaryReaderHelper(FileStream fs)
		{
			try
			{
				return new BinaryReader(fs);
			}
			catch (ArgumentException e)
			{
				LogFileException ex = new LogFileException(SR.GetString("MsgCannotOpenFile") + fs.Name + SR.GetString("MsgFilePathEnd"), fs.Name, e);
				ExceptionManager.LogAppError(ex);
				throw ex;
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static FileInfo CreateFileInfoHelper(string filePath)
		{
			if (!string.IsNullOrEmpty(filePath))
			{
				try
				{
					return new FileInfo(filePath);
				}
				catch (SecurityException e)
				{
					throw new LogFileException(SR.GetString("MsgCannotOpenFile") + filePath + SR.GetString("MsgAccessDenied"), filePath, e);
				}
				catch (UnauthorizedAccessException e2)
				{
					throw new LogFileException(SR.GetString("MsgCannotOpenFile") + filePath + SR.GetString("MsgAccessDenied"), filePath, e2);
				}
				catch (PathTooLongException e3)
				{
					throw new LogFileException(SR.GetString("MsgCannotOpenFile") + filePath + SR.GetString("MsgFilePathTooLong"), filePath, e3);
				}
				catch (ArgumentException e4)
				{
					throw new LogFileException(SR.GetString("MsgCannotOpenFile") + filePath + SR.GetString("MsgFilePathEnd"), filePath, e4);
				}
				catch (NotSupportedException e5)
				{
					throw new LogFileException(SR.GetString("MsgFailToSeekFile") + filePath + SR.GetString("MsgFilePathEnd"), filePath, e5);
				}
			}
			return null;
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static void DeleteFileByFileInfoHelper(FileInfo fileInfo)
		{
			if (fileInfo != null && fileInfo.Exists)
			{
				try
				{
					fileInfo.Delete();
				}
				catch (SecurityException e)
				{
					throw new LogFileException(SR.GetString("MsgCannotOpenFile") + fileInfo.FullName + SR.GetString("MsgAccessDenied"), fileInfo.FullName, e);
				}
				catch (UnauthorizedAccessException e2)
				{
					throw new LogFileException(SR.GetString("MsgCannotOpenFile") + fileInfo.FullName + SR.GetString("MsgAccessDenied"), fileInfo.FullName, e2);
				}
				catch (IOException e3)
				{
					throw new LogFileException(SR.GetString("MsgIOExceptionSeek") + fileInfo.FullName + SR.GetString("MsgFilePathEnd"), fileInfo.FullName, e3);
				}
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static byte[] ReadBytesHelper(BinaryReader binaryReader, int count)
		{
			try
			{
				return binaryReader.ReadBytes(count);
			}
			catch (IOException e)
			{
				LogFileException ex = new LogFileException(SR.GetString("MsgBinaryReaderExp") + SR.GetString("MsgReturnBack") + SR.GetString("MsgIOExp"), string.Empty, e);
				ExceptionManager.LogAppError(ex);
				throw ex;
			}
			catch (ObjectDisposedException e2)
			{
				LogFileException ex2 = new LogFileException(SR.GetString("MsgBinaryReaderExp") + SR.GetString("MsgReturnBack") + SR.GetString("MsgStreamClosed"), string.Empty, e2);
				ExceptionManager.LogAppError(ex2);
				throw ex2;
			}
		}

		public static Color GetColor(ApplicationColors color)
		{
			switch (color)
			{
			case ApplicationColors.GradientInactiveCaption:
				return Color.FromArgb(157, 185, 235);
			case ApplicationColors.Highlight:
				return Color.FromArgb(49, 106, 197);
			case ApplicationColors.Info:
				return Color.FromArgb(255, 255, 225);
			case ApplicationColors.TitleBorder:
				return Color.FromArgb(172, 168, 153);
			case ApplicationColors.ActiveActivityBack:
				return Color.FromArgb(248, 239, 208);
			case ApplicationColors.ActiveActivityBorder:
				return Color.FromArgb(248, 190, 0);
			case ApplicationColors.DefaultActivityBack:
				return Color.FromArgb(209, 238, 211);
			case ApplicationColors.DefaultActivityBorder:
				return Color.FromArgb(0, 128, 9);
			case ApplicationColors.ActiveActivityInner:
				return Color.FromArgb(255, 221, 102);
			case ApplicationColors.DefaultActivityInner:
				return Color.FromArgb(0, 200, 15);
			case ApplicationColors.MessageTransfer:
				return Color.FromArgb(140, 192, 255);
			case ApplicationColors.MouseOver:
				return Color.FromArgb(204, 227, 255);
			case ApplicationColors.HighlightingBack:
				return Color.FromArgb(193, 210, 238);
			case ApplicationColors.HighlightingBack2:
				return Color.FromArgb(100, 142, 216);
			case ApplicationColors.HighlightingBorder:
				return Color.FromArgb(49, 106, 197);
			case ApplicationColors.RandomColor1:
				return Color.FromArgb(255, 208, 174);
			case ApplicationColors.RandomColor2:
				return Color.FromArgb(158, 205, 165);
			case ApplicationColors.RandomColor3:
				return Color.FromArgb(169, 250, 254);
			case ApplicationColors.RandomColor4:
				return Color.FromArgb(255, 185, 217);
			case ApplicationColors.RandomColor5:
				return Color.FromArgb(204, 225, 225);
			case ApplicationColors.RandomColor6:
				return Color.FromArgb(231, 214, 209);
			case ApplicationColors.RandomColor7:
				return Color.FromArgb(235, 236, 179);
			case ApplicationColors.RandomColor8:
				return Color.FromArgb(198, 202, 240);
			case ApplicationColors.RandomColor9:
				return Color.FromArgb(203, 205, 220);
			case ApplicationColors.RandomColor10:
				return Color.FromArgb(253, 216, 166);
			case ApplicationColors.HightlightedMenuColor:
				return Color.FromArgb(18, 41, 252);
			default:
				return Color.Black;
			}
		}

		[ReliabilityContract(Consistency.MayCorruptInstance, Cer.Success)]
		public static void CloseStreamWithoutException(Stream stream, bool isFlushStream)
		{
			if (stream != null)
			{
				try
				{
					if (isFlushStream)
					{
						stream.Flush();
					}
					stream.Close();
				}
				catch (IOException)
				{
				}
				catch (ObjectDisposedException)
				{
				}
			}
		}

		public static int ByteArrayIndexOf(byte[] bytes, byte[] findingBytes, int offset)
		{
			if (findingBytes == null || bytes == null)
			{
				return -1;
			}
			if (offset + findingBytes.Length + 1 > bytes.Length)
			{
				return -1;
			}
			int i = offset;
			int num = 0;
			for (; i < bytes.Length; i++)
			{
				num = 0;
				for (; i < bytes.Length; i++)
				{
					if (num >= findingBytes.Length)
					{
						break;
					}
					if (findingBytes[num] != bytes[i])
					{
						break;
					}
					num++;
				}
				if (num >= findingBytes.Length)
				{
					return i - num;
				}
			}
			return -1;
		}

		public static string TradeOffXmlPrefixForName(string fullName)
		{
			string text = fullName;
			if (!string.IsNullOrEmpty(text))
			{
				text = text.Trim();
				if (text.Contains("xmlns"))
				{
					text = "xmlns";
				}
				else if (text.Contains(":"))
				{
					string[] array = text.Split(new char[1]
					{
						':'
					}, StringSplitOptions.RemoveEmptyEntries);
					if (array != null && array.Length != 0)
					{
						string[] array2 = array;
						text = array2[array2.Length - 1];
					}
				}
			}
			return text;
		}

		public static string GetShortTimeStringFromDateTime(DateTime dateTime)
		{
			return dateTime.ToShortDateString() + string.Format(CultureInfo.InvariantCulture, " {0:HH}{1}{0:mm}{1}{0:ss.fffffff}", new object[2]
			{
				dateTime,
				SR.GetString("SL_DateTimeMinSep")
			});
		}

		public static void CopyTextToClipboard(string content)
		{
			if (!string.IsNullOrEmpty(content))
			{
				try
				{
					Clipboard.SetText(content, TextDataFormat.UnicodeText);
				}
				catch (ExternalException)
				{
				}
				catch (ThreadStateException)
				{
				}
				catch (InvalidEnumArgumentException)
				{
				}
			}
		}
	}
}
