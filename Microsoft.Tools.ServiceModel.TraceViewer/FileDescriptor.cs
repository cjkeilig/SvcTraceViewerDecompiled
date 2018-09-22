using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal class FileDescriptor
	{
		public delegate void SourceFileChanged(string filePath);

		private const int BATCH_FILE_BLOCK_SIZE = 2046;

		private const int LAST_TRACE_BACKWARD_SIZE = 10240;

		public const long DEFAULT_FILE_BLOCK_SIZE = 5000000L;

		private const int DEFAULT_TRACERECORD_POS_PRIORITY = 1024;

		private byte[] findingBytes = new byte[14]
		{
			60,
			69,
			50,
			69,
			84,
			114,
			97,
			99,
			101,
			69,
			118,
			101,
			110,
			116
		};

		private XmlUtils xmlUtil;

		private FileStream fileStream;

		private string filePath;

		private long fileLength;

		private List<FileBlockInfo> fileBlocks = new List<FileBlockInfo>();

		private List<FileBlockInfo> selectedFileBlocks = new List<FileBlockInfo>();

		private FileSystemWatcher fileSysWatcher;

		private TraceDataSource dataSource;

		private object thisLock = new object();

		private SourceFileChanged sourceFileChangedCallback;

		private object ThisLock => thisLock;

		public long FileSize => fileLength;

		public long SelectedBlockFileSize
		{
			get
			{
				long num = 0L;
				if (selectedFileBlocks == null)
				{
					return num;
				}
				foreach (FileBlockInfo selectedFileBlock in selectedFileBlocks)
				{
					if (selectedFileBlock != null)
					{
						num += selectedFileBlock.EndFileOffset - selectedFileBlock.StartFileOffset;
					}
				}
				return num;
			}
		}

		public string FilePath => filePath;

		public int FileBlockCount => fileBlocks.Count;

		public List<FileBlockInfo> FileBlocks => fileBlocks;

		public List<FileBlockInfo> SelectedFileBlocks => selectedFileBlocks;

		public FileStream GetCachedFileStream()
		{
			return fileStream;
		}

		public bool IsBlockSelected(FileBlockInfo blockInfo)
		{
			if (selectedFileBlocks.Contains(blockInfo))
			{
				return true;
			}
			return false;
		}

		public void SelectFileBlock(FileBlockInfo blockInfo)
		{
			if (!IsBlockSelected(blockInfo))
			{
				selectedFileBlocks.Add(blockInfo);
			}
		}

		public void ClearSelectedFileBlocks()
		{
			selectedFileBlocks.Clear();
		}

		public FileDescriptor(string filePath, SourceFileChanged fileChangedCallback, TraceDataSource dataSource)
		{
			if (string.IsNullOrEmpty(filePath))
			{
				throw new ArgumentException();
			}
			this.dataSource = dataSource;
			FileStream fileStream = Utilities.CreateFileStreamHelper(filePath);
			this.filePath = filePath;
			fileLength = fileStream.Length;
			xmlUtil = new XmlUtils(null);
			try
			{
				FileSectorIndicator(fileStream);
			}
			catch (LogFileException ex)
			{
				fileStream.Close();
				throw ex;
			}
			this.fileStream = fileStream;
			if (fileChangedCallback != null)
			{
				sourceFileChangedCallback = (SourceFileChanged)Delegate.Combine(sourceFileChangedCallback, fileChangedCallback);
			}
		}

		public void RegisterFileChangeCallback()
		{
			lock (ThisLock)
			{
				if (fileSysWatcher != null)
				{
					fileSysWatcher.Dispose();
				}
				if (!string.IsNullOrEmpty(FilePath))
				{
					try
					{
						fileSysWatcher = new FileSystemWatcher(Path.GetDirectoryName(FilePath));
						fileSysWatcher.NotifyFilter = NotifyFilters.LastWrite;
						fileSysWatcher.Filter = Path.GetFileName(FilePath);
						fileSysWatcher.Changed += fileSysWatcher_Changed;
						fileSysWatcher.EnableRaisingEvents = true;
					}
					catch (ArgumentException)
					{
						fileSysWatcher = null;
					}
				}
			}
		}

		public void UnRegisterFileChangeCallback()
		{
			lock (ThisLock)
			{
				if (fileSysWatcher != null)
				{
					fileSysWatcher.Dispose();
				}
			}
		}

		private void fileSysWatcher_Changed(object sender, FileSystemEventArgs e)
		{
			lock (ThisLock)
			{
				if (e.ChangeType == WatcherChangeTypes.Changed && !string.IsNullOrEmpty(e.FullPath) && e.FullPath.Trim().ToLowerInvariant() == FilePath.Trim().ToLowerInvariant())
				{
					try
					{
						sourceFileChangedCallback(e.FullPath);
					}
					catch (Exception e2)
					{
						ExceptionManager.GeneralExceptionFilter(e2);
					}
				}
			}
		}

		~FileDescriptor()
		{
			Close();
		}

		public void Close()
		{
			try
			{
				if (fileStream != null)
				{
					fileStream.Close();
				}
				UnRegisterFileChangeCallback();
			}
			catch (Exception e)
			{
				ExceptionManager.GeneralExceptionFilter(e);
			}
		}

		private void FileSectorIndicator(FileStream fs)
		{
			long num = 0L;
			long foundTraceFileOffset = 0L;
			TraceRecord lastValidTrace = GetLastValidTrace(fs, out foundTraceFileOffset);
			if (lastValidTrace == null)
			{
				return;
			}
			while (true)
			{
				if (num > foundTraceFileOffset)
				{
					return;
				}
				TraceRecord traceRecord = null;
				TraceRecord traceRecord2 = null;
				long foundTraceFileOffset2 = 0L;
				long foundTraceFileOffset3 = 0L;
				traceRecord = GetFirstValidTrace(fs, num, out foundTraceFileOffset2);
				if (traceRecord == null)
				{
					return;
				}
				num = foundTraceFileOffset2 + 5000000;
				if (num >= foundTraceFileOffset)
				{
					traceRecord2 = lastValidTrace;
					foundTraceFileOffset3 = foundTraceFileOffset;
				}
				else
				{
					traceRecord2 = GetFirstValidTrace(fs, num, out foundTraceFileOffset3);
					if (traceRecord2 == null)
					{
						return;
					}
				}
				if (traceRecord.Time > traceRecord2.Time)
				{
					break;
				}
				FileBlockInfo fileBlockInfo = new FileBlockInfo();
				fileBlockInfo.StartDate = traceRecord.Time;
				fileBlockInfo.StartFileOffset = foundTraceFileOffset2;
				fileBlockInfo.EndDate = traceRecord2.Time;
				fileBlockInfo.EndFileOffset = foundTraceFileOffset3;
				fileBlocks.Add(fileBlockInfo);
				num = foundTraceFileOffset3 + findingBytes.Length;
			}
			throw new LogFileException(SR.GetString("MsgTimeRangeError") + SR.GetString("MsgReturnBack") + fs.Name + SR.GetString("MsgReturnBack"), fs.Name, null);
		}

		internal TraceRecord GetLastValidTrace(FileStream fs, out long foundTraceFileOffset)
		{
			foundTraceFileOffset = -1L;
			if (fs == null)
			{
				return null;
			}
			long num = FileSize;
			while (num > 0)
			{
				num -= 10240;
				if (num < 0)
				{
					num = 0L;
				}
				long foundTraceFileOffset2 = 0L;
				long num2 = 0L;
				long fileOffset = num;
				TraceRecord traceRecord = null;
				for (TraceRecord firstValidTrace = GetFirstValidTrace(fs, fileOffset, out foundTraceFileOffset2); firstValidTrace != null; firstValidTrace = GetFirstValidTrace(fs, fileOffset, out foundTraceFileOffset2))
				{
					num2 = foundTraceFileOffset2;
					traceRecord = firstValidTrace;
					fileOffset = foundTraceFileOffset2 + findingBytes.Length;
				}
				if (traceRecord != null)
				{
					foundTraceFileOffset = num2;
					return traceRecord;
				}
			}
			return null;
		}

		internal TraceRecord GetFirstValidTrace(FileStream fs, long fileOffset, out long foundTraceFileOffset, out long potentialNextTraceOffset, TaskInfoBase task)
		{
			foundTraceFileOffset = -1L;
			potentialNextTraceOffset = -1L;
			if (fs == null || fileOffset < 0 || fileOffset > fileLength)
			{
				return null;
			}
			Utilities.SeekFileStreamHelper(fs, fileOffset, SeekOrigin.Begin);
			BinaryReader binaryReader = Utilities.CreateBinaryReaderHelper(fs);
			long num = fileOffset;
			while (true)
			{
				byte[] bytes = Utilities.ReadBytesHelper(binaryReader, 2046);
				int num2 = 0;
				int num3 = Utilities.ByteArrayIndexOf(bytes, findingBytes, num2);
				while (num3 != -1)
				{
					num = fileOffset + num3;
					Utilities.SeekFileStreamHelper(fs, num, SeekOrigin.Begin);
					XmlTextReader xmlTextReader = new XmlTextReader(fs, XmlNodeType.Element, null);
					xmlTextReader.WhitespaceHandling = WhitespaceHandling.None;
					try
					{
						if (xmlTextReader.Read() && xmlTextReader.Name == "E2ETraceEvent")
						{
							TraceRecord traceRecord = new TraceRecord(dataSource);
							traceRecord.ReadFrom(xmlTextReader, xmlUtil);
							foundTraceFileOffset = num;
							potentialNextTraceOffset = foundTraceFileOffset + findingBytes.Length;
							int typePositionPriority = 1024;
							traceRecord.TraceRecordPos = new TraceRecordPosition(this, foundTraceFileOffset, traceRecord.Time, typePositionPriority);
							traceRecord.TraceID = foundTraceFileOffset + filePath.GetHashCode();
							return traceRecord;
						}
						if (fs.Position >= FileSize)
						{
							return null;
						}
					}
					catch (XmlException ex)
					{
						task?.SaveException(new E2EInvalidFileException(ex.Message, fs.Name, ex, num));
					}
					catch (E2EInvalidFileException ex2)
					{
						task?.SaveException(new E2EInvalidFileException(ex2.Message, fs.Name, ex2, num));
					}
					finally
					{
						num2 += num3 + findingBytes.Length;
						num3 = Utilities.ByteArrayIndexOf(bytes, findingBytes, num2);
					}
				}
				if (fs.Position >= FileSize)
				{
					break;
				}
				fileOffset += 2046 - findingBytes.Length - 2;
				Utilities.SeekFileStreamHelper(fs, fileOffset, SeekOrigin.Begin);
			}
			return null;
		}

		public TraceRecord GetFirstValidTrace(FileStream fs, long fileOffset, out long foundTraceFileOffset)
		{
			long potentialNextTraceOffset = 0L;
			return GetFirstValidTrace(fs, fileOffset, out foundTraceFileOffset, out potentialNextTraceOffset);
		}

		internal TraceRecord GetFirstValidTrace(FileStream fs, long fileOffset, out long foundTraceFileOffset, out long potentialNextTraceOffset)
		{
			return GetFirstValidTrace(fs, fileOffset, out foundTraceFileOffset, out potentialNextTraceOffset, null);
		}
	}
}
