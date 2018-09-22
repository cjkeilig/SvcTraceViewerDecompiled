using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal static class TempFileManager
	{
		private const string resourcePrefix = "SvcTraceViewer.";

		[SecurityCritical]
		public static void Initialize()
		{
			AppDomain currentDomain = AppDomain.CurrentDomain;
		}

		public static Stream GetResourceFileStreamByName(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				try
				{
					if (!name.StartsWith("SvcTraceViewer.", StringComparison.Ordinal))
					{
						name = "SvcTraceViewer." + name;
					}
					return Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
				}
				catch (Exception e)
				{
					ExceptionManager.GeneralExceptionFilter(e);
				}
			}
			return null;
		}

		public static Image GetImageFromEmbededResources(Images index, Color transparentColor, bool isMakeTransparent)
		{
			Stream resourceFileStreamByName = GetResourceFileStreamByName(GetImageResourceNameByID(index));
			if (resourceFileStreamByName != null)
			{
				try
				{
					Image image = Image.FromStream(resourceFileStreamByName);
					if (image is Bitmap)
					{
						Bitmap bitmap = (Bitmap)image;
						if (isMakeTransparent)
						{
							bitmap.MakeTransparent(transparentColor);
						}
					}
					return image;
				}
				catch (ArgumentException)
				{
				}
			}
			return null;
		}

		public static Image GetImageFromEmbededResources(Images index)
		{
			return GetImageFromEmbededResources(index, Color.White, isMakeTransparent: false);
		}

		private static string GetImageResourceNameByID(Images index)
		{
			switch (index)
			{
			case Images.Call:
				return "SvcTraceViewer.Call.bmp";
			case Images.Helper:
				return "SvcTraceViewer.Helper.bmp";
			case Images.HttpSys:
				return "SvcTraceViewer.HttpSys.bmp";
			case Images.Operation:
				return "SvcTraceViewer.Operation.bmp";
			case Images.Reply:
				return "SvcTraceViewer.OperationReturn.bmp";
			case Images.Message:
				return "SvcTraceViewer.Message.bmp";
			case Images.MessageHandling:
				return "SvcTraceViewer.MessageHandling.bmp";
			case Images.Transfer:
				return "SvcTraceViewer.Transfer.bmp";
			case Images.CallWithMessage:
				return "SvcTraceViewer.CallWithMsg.bmp";
			case Images.TransferIn:
				return "SvcTraceViewer.TransferIn.bmp";
			case Images.TransferOut:
				return "SvcTraceViewer.TransferOut.bmp";
			case Images.SmartTag:
				return "SvcTraceViewer.SmartTag.bmp";
			case Images.SL_Backward:
				return "SvcTraceViewer.SL_Backward.bmp";
			case Images.SL_Forward:
				return "SvcTraceViewer.SL_Forward.bmp";
			case Images.HostActivityIcon:
				return "SvcTraceViewer.HostActivityIcon.bmp";
			case Images.ExecutionActivityIcon:
				return "SvcTraceViewer.ExecutionActivityIcon.bmp";
			case Images.MessageActivityIcon:
				return "SvcTraceViewer.MessageActivityIcon.bmp";
			case Images.ConnectionActivityIcon:
				return "SvcTraceViewer.ConnectionActivityIcon.bmp";
			case Images.PlusIcon:
				return "SvcTraceViewer.PlusIcon.bmp";
			case Images.MinusIcon:
				return "SvcTraceViewer.MinusIcon.bmp";
			case Images.ActivityStartTrace:
				return "SvcTraceViewer.ActivityStartTrace.bmp";
			case Images.ActivityStopTrace:
				return "SvcTraceViewer.ActivityStopTrace.bmp";
			case Images.ActivityResumeTrace:
				return "SvcTraceViewer.ActivityResumeTrace.bmp";
			case Images.ActivityPauseTrace:
				return "SvcTraceViewer.ActivityPauseTrace.bmp";
			case Images.MessageSentTrace:
				return "SvcTraceViewer.MessageSentTrace.bmp";
			case Images.MessageReceiveTrace:
				return "SvcTraceViewer.MessageReceiveTrace.bmp";
			case Images.RootActivity:
				return "SvcTraceViewer.RootActivity.bmp";
			case Images.ListenActivity:
				return "SvcTraceViewer.ListenActivity.bmp";
			case Images.DefaultActivityIcon:
				return "SvcTraceViewer.DefaultActivityIcon.bmp";
			case Images.ErrorTrace:
				return "SvcTraceViewer.ErrorTrace.bmp";
			case Images.WarningTrace:
				return "SvcTraceViewer.WarningTrace.bmp";
			case Images.TraceDetailedMinus:
				return "SvcTraceViewer.TraceDetailedMinus.bmp";
			case Images.TraceDetailedPlus:
				return "SvcTraceViewer.TraceDetailedPlus.bmp";
			case Images.ProcessTitleBack:
				return "SvcTraceViewer.ProcessTitleBak.bmp";
			case Images.AboutBox:
				return "SvcTraceViewer.AboutBox.bmp";
			case Images.Unknown:
				return "SvcTraceViewer.Unknown.bmp";
			default:
				return "SvcTraceViewer.Unknown.bmp";
			}
		}
	}
}
