using System.Globalization;
using System.Resources;
using System.Threading;

namespace Microsoft.Tools.ServiceModel.TraceViewer
{
	internal sealed class SR
	{
		internal const string SortByStartTime = "SortByStartTime";

		internal const string SortByEndTime = "SortByEndTime";

		internal const string MsgInvalidInputFile = "MsgInvalidInputFile";

		internal const string MsgNullHandle = "MsgNullHandle";

		internal const string MsgProcessTraceFailed = "MsgProcessTraceFailed";

		internal const string MsgUnhandledExceptionInEtwGetTraces = "MsgUnhandledExceptionInEtwGetTraces";

		internal const string MsgCannotOpenFile = "MsgCannotOpenFile";

		internal const string MsgFilePathNotSupport = "MsgFilePathNotSupport";

		internal const string MsgFileNotFound = "MsgFileNotFound";

		internal const string MsgAccessDenied = "MsgAccessDenied";

		internal const string MsgDirectoryNotFound = "MsgDirectoryNotFound";

		internal const string MsgFilePathTooLong = "MsgFilePathTooLong";

		internal const string MsgIOException = "MsgIOException";

		internal const string MsgFilePathEnd = "MsgFilePathEnd";

		internal const string MsgIOExceptionSeek = "MsgIOExceptionSeek";

		internal const string MsgFailToSeekFile = "MsgFailToSeekFile";

		internal const string MsgFileStreamNotReadable = "MsgFileStreamNotReadable";

		internal const string MsgInvokeExceptionOccur = "MsgInvokeExceptionOccur";

		internal const string MsgReturnBack = "MsgReturnBack";

		internal const string MsgCannotFindFile = "MsgCannotFindFile";

		internal const string MsgCannotOpenFileReturn = "MsgCannotOpenFileReturn";

		internal const string MsgTraceFilterChangeCallbackError = "MsgTraceFilterChangeCallbackError";

		internal const string MsgReturn2 = "MsgReturn2";

		internal const string MsgSortingTraceWarning = "MsgSortingTraceWarning";

		internal const string MsgFailToSkipNode = "MsgFailToSkipNode";

		internal const string MsgTimeRangeError = "MsgTimeRangeError";

		internal const string MsgStreamClosed = "MsgStreamClosed";

		internal const string MsgIOExp = "MsgIOExp";

		internal const string MsgBinaryReaderExp = "MsgBinaryReaderExp";

		internal const string MsgSFNotFound = "MsgSFNotFound";

		internal const string MsgSysExpLA1 = "MsgSysExpLA1";

		internal const string MsgSysExpC = "MsgSysExpC";

		internal const string MsgSysExpLF = "MsgSysExpLF";

		internal const string MsgTimeFormatErr = "MsgTimeFormatErr";

		internal const string MsgLevelFormatErr = "MsgLevelFormatErr";

		internal const string MsgPrjNotExist = "MsgPrjNotExist";

		internal const string MsgFileNotExist = "MsgFileNotExist";

		internal const string MsgAppSettingSaveError = "MsgAppSettingSaveError";

		internal const string MsgFileConvertTraceErr = "MsgFileConvertTraceErr";

		internal const string MsgFailToSaveProject = "MsgFailToSaveProject";

		internal const string MsgNoValidTraceInFile = "MsgNoValidTraceInFile";

		internal const string MsgUnsupportedSchema = "MsgUnsupportedSchema";

		internal const string MsgCannotWriteToFile = "MsgCannotWriteToFile";

		internal const string MsgCannotWriteToFileEnd = "MsgCannotWriteToFileEnd";

		internal const string MsgErrorOccursOnConvertCrimson = "MsgErrorOccursOnConvertCrimson";

		internal const string MsgUnknownFileFormat = "MsgUnknownFileFormat";

		internal const string MsgOutOfMemory = "MsgOutOfMemory";

		internal const string MsgUnknownOpenParam = "MsgUnknownOpenParam";

		internal const string UnRegisterFail = "UnRegisterFail";

		internal const string MsgFailSavePrj = "MsgFailSavePrj";

		internal const string MsgReaderEOF = "MsgReaderEOF";

		internal const string MsgErrorTraceLoading = "MsgErrorTraceLoading";

		internal const string MsgRefreshTraceSource = "MsgRefreshTraceSource";

		internal const string CreateCustomFilter = "CreateCustomFilter";

		internal const string CopyTraceToClipboard = "CopyTraceToClipboard";

		internal const string SelectAllActivities = "SelectAllActivities";

		internal const string ApplicationIconResourceName = "ApplicationIconResourceName";

		internal const string MainFrm_WindowTitle = "MainFrm_WindowTitle";

		internal const string MainFrm_Activities = "MainFrm_Activities";

		internal const string MainFrm_Traces = "MainFrm_Traces";

		internal const string MainFrm_FileOpenDlg1 = "MainFrm_FileOpenDlg1";

		internal const string MainFrm_FileOpenDlg2 = "MainFrm_FileOpenDlg2";

		internal const string MainFrm_FileSaveDlg1 = "MainFrm_FileSaveDlg1";

		internal const string MainFrm_FileEtl = "MainFrm_FileEtl";

		internal const string MainFrm_FileE2e = "MainFrm_FileE2e";

		internal const string MainFrm_FileMenu = "MainFrm_FileMenu";

		internal const string MainFrm_OpenMI = "MainFrm_OpenMI";

		internal const string MainFrm_AddMI = "MainFrm_AddMI";

		internal const string MainFrm_CloseAllMI = "MainFrm_CloseAllMI";

		internal const string MainFrm_OpenPrjMI = "MainFrm_OpenPrjMI";

		internal const string MainFrm_SavePrjMI = "MainFrm_SavePrjMI";

		internal const string MainFrm_SavePrjAsMI = "MainFrm_SavePrjAsMI";

		internal const string MainFrm_ClosePrj = "MainFrm_ClosePrj";

		internal const string MainFrm_RecentMI = "MainFrm_RecentMI";

		internal const string MainFrm_ExitMI = "MainFrm_ExitMI";

		internal const string MainFrm_EditMenu = "MainFrm_EditMenu";

		internal const string MainFrm_FindMI = "MainFrm_FindMI";

		internal const string MainFrm_FindNextMI = "MainFrm_FindNextMI";

		internal const string MainFrm_ViewMenu = "MainFrm_ViewMenu";

		internal const string MainFrm_ActivityViewMI = "MainFrm_ActivityViewMI";

		internal const string MainFrm_ProjectViewMI = "MainFrm_ProjectViewMI";

		internal const string MainFrm_TreeViewMI = "MainFrm_TreeViewMI";

		internal const string MainFrm_CustomFilterMI = "MainFrm_CustomFilterMI";

		internal const string MainFrm_FilterOptionMI = "MainFrm_FilterOptionMI";

		internal const string MainFrm_RefreshMI = "MainFrm_RefreshMI";

		internal const string MainFrm_ActivityMenu = "MainFrm_ActivityMenu";

		internal const string MainFrm_FollowForwJO = "MainFrm_FollowForwJO";

		internal const string MainFrm_FollowBackJI = "MainFrm_FollowBackJI";

		internal const string MainFrm_FollowForwMI = "MainFrm_FollowForwMI";

		internal const string MainFrm_FollowBackMI = "MainFrm_FollowBackMI";

		internal const string MainFrm_FollowForwJT = "MainFrm_FollowForwJT";

		internal const string MainFrm_HelpMenu = "MainFrm_HelpMenu";

		internal const string MainFrm_HelpMI = "MainFrm_HelpMI";

		internal const string MainFrm_AboutMI = "MainFrm_AboutMI";

		internal const string MainFrm_OperationsMenu = "MainFrm_OperationsMenu";

		internal const string MainFrm_CancelMI = "MainFrm_CancelMI";

		internal const string MainFrm_FormattedTab = "MainFrm_FormattedTab";

		internal const string MainFrm_XMLTab = "MainFrm_XMLTab";

		internal const string MainFrm_MessageTab = "MainFrm_MessageTab";

		internal const string MainFrm_DescriptionClm = "MainFrm_DescriptionClm";

		internal const string MainFrm_LevelClm = "MainFrm_LevelClm";

		internal const string MainFrm_ThreadIDClm = "MainFrm_ThreadIDClm";

		internal const string MainFrm_ProcessNameClm = "MainFrm_ProcessNameClm";

		internal const string MainFrm_TimeClm = "MainFrm_TimeClm";

		internal const string MainFrm_TraceCodeClm = "MainFrm_TraceCodeClm";

		internal const string MainFrm_ActivityTab = "MainFrm_ActivityTab";

		internal const string MainFrm_ActivityClm = "MainFrm_ActivityClm";

		internal const string MainFrm_DurationClm = "MainFrm_DurationClm";

		internal const string MainFrm_SourceClm = "MainFrm_SourceClm";

		internal const string MainFrm_TracesClm = "MainFrm_TracesClm";

		internal const string MainFrm_StartTicksClm = "MainFrm_StartTicksClm";

		internal const string MainFrm_EndTicksClm = "MainFrm_EndTicksClm";

		internal const string MainFrm_ProjectTab = "MainFrm_ProjectTab";

		internal const string MainFrm_TreeTab = "MainFrm_TreeTab";

		internal const string MainFrm_FilterEnabled = "MainFrm_FilterEnabled";

		internal const string MainFrm_FilterDisable = "MainFrm_FilterDisable";

		internal const string MainFrm_ReloadFile = "MainFrm_ReloadFile";

		internal const string MainFrm_ToString = "MainFrm_ToString";

		internal const string MainFrm_FromString = "MainFrm_FromString";

		internal const string MainFrm_MultiNamedActivityTip = "MainFrm_MultiNamedActivityTip";

		internal const string MainFrm_ReturnSingle = "MainFrm_ReturnSingle";

		internal const string MainFrm_TabSingle = "MainFrm_TabSingle";

		internal const string MainFrm_TraceFrom = "MainFrm_TraceFrom";

		internal const string MainFrm_TraceFromActivity = "MainFrm_TraceFromActivity";

		internal const string MainFrm_GroupByNone = "MainFrm_GroupByNone";

		internal const string MainFrm_GroupBySource = "MainFrm_GroupBySource";

		internal const string MainFrm_GroupByNone2 = "MainFrm_GroupByNone2";

		internal const string MainFrm_GroupByActivity2 = "MainFrm_GroupByActivity2";

		internal const string MainFrm_GroupByProcess2 = "MainFrm_GroupByProcess2";

		internal const string MainFrm_GroupByIO2 = "MainFrm_GroupByIO2";

		internal const string MainFrm_GNone = "MainFrm_GNone";

		internal const string MainFrm_GActivity = "MainFrm_GActivity";

		internal const string MainFrm_GSource = "MainFrm_GSource";

		internal const string MainFrm_GroupBy = "MainFrm_GroupBy";

		internal const string MainFrm_ActivityNameClm = "MainFrm_ActivityNameClm";

		internal const string MainFrm_FilterNowMI = "MainFrm_FilterNowMI";

		internal const string MainFrm_FilterBar = "MainFrm_FilterBar";

		internal const string MainFrm_FindToolBar = "MainFrm_FindToolBar";

		internal const string MainFrm_MessageViewTab = "MainFrm_MessageViewTab";

		internal const string MainFrm_MessageViewMI = "MainFrm_MessageViewMI";

		internal const string MainFrm_ActivityGraphMenu = "MainFrm_ActivityGraphMenu";

		internal const string MainFrm_ActivityMenuEmpty = "MainFrm_ActivityMenuEmpty";

		internal const string MainFrm_ActivityMenuNonEmpty = "MainFrm_ActivityMenuNonEmpty";

		internal const string MainFrm_ActivityMenuRes = "MainFrm_ActivityMenuRes";

		internal const string MainFrm_ErrTraceRecord = "MainFrm_ErrTraceRecord";

		internal const string MainFrm_ErrTraceRecord2 = "MainFrm_ErrTraceRecord2";

		internal const string MainFrm_RecentProjectMI = "MainFrm_RecentProjectMI";

		internal const string MainFrm_DurationWrong = "MainFrm_DurationWrong";

		internal const string MainFrm_DurationZero = "MainFrm_DurationZero";

		internal const string MainFrm_DurationMillSecond = "MainFrm_DurationMillSecond";

		internal const string MainFrm_DurationSecond = "MainFrm_DurationSecond";

		internal const string MainFrm_DurationMinute = "MainFrm_DurationMinute";

		internal const string MainFrm_DurationHour = "MainFrm_DurationHour";

		internal const string MainFrm_DurationDay = "MainFrm_DurationDay";

		internal const string MainFrm_FileNameSep = "MainFrm_FileNameSep";

		internal const string MainFrm_SaveActivityGraphMenu = "MainFrm_SaveActivityGraphMenu";

		internal const string MainFrm_CreateCustomFilter = "MainFrm_CreateCustomFilter";

		internal const string MainFrm_CreateCustomFilterToolTip = "MainFrm_CreateCustomFilterToolTip";

		internal const string MainFrm_GProcess = "MainFrm_GProcess";

		internal const string MainFrm_GIO = "MainFrm_GIO";

		internal const string MainFrm_GroupByTip = "MainFrm_GroupByTip";

		internal const string MainFrm_GroupByNoneTip = "MainFrm_GroupByNoneTip";

		internal const string MainFrm_GroupByActivityTip = "MainFrm_GroupByActivityTip";

		internal const string MainFrm_GroupBySourceTip = "MainFrm_GroupBySourceTip";

		internal const string MainFrm_GroupByProcessTip = "MainFrm_GroupByProcessTip";

		internal const string MainFrm_GroupByInOutTip = "MainFrm_GroupByInOutTip";

		internal const string MainFrm_ExecProcessMode = "MainFrm_ExecProcessMode";

		internal const string MainFrm_ExecProcessMode2 = "MainFrm_ExecProcessMode2";

		internal const string MainFrm_ExecProcessModeTip0 = "MainFrm_ExecProcessModeTip0";

		internal const string MainFrm_ExecProcessModeTip = "MainFrm_ExecProcessModeTip";

		internal const string MainFrm_ExecThreadMode = "MainFrm_ExecThreadMode";

		internal const string MainFrm_ExecThreadMode2 = "MainFrm_ExecThreadMode2";

		internal const string MainFrm_ExecThreadModeTip = "MainFrm_ExecThreadModeTip";

		internal const string Utility_UNSize = "Utility_UNSize";

		internal const string GP_MsgUnkn = "GP_MsgUnkn";

		internal const string GP_MsgIn = "GP_MsgIn";

		internal const string GP_MsgOut = "GP_MsgOut";

		internal const string TxtBytes = "TxtBytes";

		internal const string TxtKBytes = "TxtKBytes";

		internal const string TxtMB = "TxtMB";

		internal const string TxtMenuAddFile = "TxtMenuAddFile";

		internal const string TxtMenuRemoveFile = "TxtMenuRemoveFile";

		internal const string TxtMenuRemoveAllFiles = "TxtMenuRemoveAllFiles";

		internal const string TxtMenuOpenProject = "TxtMenuOpenProject";

		internal const string TxtMenuSaveProject = "TxtMenuSaveProject";

		internal const string TxtFrom = "TxtFrom";

		internal const string TxtTo = "TxtTo";

		internal const string TxtLookFor = "TxtLookFor";

		internal const string TxtTime = "TxtTime";

		internal const string TxtSearchIn = "TxtSearchIn";

		internal const string TxtTraceLevel = "TxtTraceLevel";

		internal const string TxtCannotLoadMessage = "TxtCannotLoadMessage";

		internal const string TxtMenuSaveProjectAs = "TxtMenuSaveProjectAs";

		internal const string TxtMenuCloseProjectAs = "TxtMenuCloseProjectAs";

		internal const string TxtPrjSeperator = "TxtPrjSeperator";

		internal const string TR_TraceTransfer = "TR_TraceTransfer";

		internal const string TR_Arrow = "TR_Arrow";

		internal const string TR_LeftQu = "TR_LeftQu";

		internal const string TR_RightQu = "TR_RightQu";

		internal const string TR_MsgLogTrace = "TR_MsgLogTrace";

		internal const string BtnFilterNow = "BtnFilterNow";

		internal const string Find_What = "Find_What";

		internal const string Find_What2 = "Find_What2";

		internal const string Find_Lookin = "Find_Lookin";

		internal const string Find_Lookin2 = "Find_Lookin2";

		internal const string Find_Scope1 = "Find_Scope1";

		internal const string Find_Scope2 = "Find_Scope2";

		internal const string Find_Target = "Find_Target";

		internal const string Find_T1 = "Find_T1";

		internal const string Find_T2 = "Find_T2";

		internal const string Find_T3 = "Find_T3";

		internal const string Find_T4 = "Find_T4";

		internal const string Find_Next = "Find_Next";

		internal const string Find_Options = "Find_Options";

		internal const string Find_O1 = "Find_O1";

		internal const string Find_O2 = "Find_O2";

		internal const string Find_O3 = "Find_O3";

		internal const string Find_Title = "Find_Title";

		internal const string Find_Button = "Find_Button";

		internal const string Find_Cancel = "Find_Cancel";

		internal const string Find_LookInTip = "Find_LookInTip";

		internal const string Find_FindWhatTip = "Find_FindWhatTip";

		internal const string Find_NoFound = "Find_NoFound";

		internal const string AppFilterItem1 = "AppFilterItem1";

		internal const string AppFilterItem2 = "AppFilterItem2";

		internal const string AppFilterItem3 = "AppFilterItem3";

		internal const string AppFilterItem4 = "AppFilterItem4";

		internal const string AppFilterItem5 = "AppFilterItem5";

		internal const string AppFilterItem6 = "AppFilterItem6";

		internal const string AppFilterItem7 = "AppFilterItem7";

		internal const string AppFilterItem8 = "AppFilterItem8";

		internal const string AppFilterItem9 = "AppFilterItem9";

		internal const string AppFilterItem10 = "AppFilterItem10";

		internal const string AppFilterItem11 = "AppFilterItem11";

		internal const string AppFilterItem12 = "AppFilterItem12";

		internal const string AppFilterItem13 = "AppFilterItem13";

		internal const string AppFilterItem14 = "AppFilterItem14";

		internal const string AppFilterItem15 = "AppFilterItem15";

		internal const string AppFilterItem16 = "AppFilterItem16";

		internal const string AppFilterItem17 = "AppFilterItem17";

		internal const string AppFilterItem18 = "AppFilterItem18";

		internal const string Filter_TP_SearchIn = "Filter_TP_SearchIn";

		internal const string Filter_TP_TraceLevel = "Filter_TP_TraceLevel";

		internal const string Filter_TP_FilterNow = "Filter_TP_FilterNow";

		internal const string FileBlockInfo_LoadingSize = "FileBlockInfo_LoadingSize";

		internal const string FileBlockInfo_LoadingPre = "FileBlockInfo_LoadingPre";

		internal const string FileBlockInfo_EmptyFile = "FileBlockInfo_EmptyFile";

		internal const string PL_Adjust = "PL_Adjust";

		internal const string PL_StartTime = "PL_StartTime";

		internal const string PL_EndTime = "PL_EndTime";

		internal const string PJ_Extension = "PJ_Extension";

		internal const string PLDlg_Title = "PLDlg_Title";

		internal const string PLDlg_OK = "PLDlg_OK";

		internal const string PLDlg_Cancel = "PLDlg_Cancel";

		internal const string PLDlg_Description = "PLDlg_Description";

		internal const string PrjMgr_FailOpen = "PrjMgr_FailOpen";

		internal const string PrjMgr_SaveMsg = "PrjMgr_SaveMsg";

		internal const string PrjMgr_SaveTitle = "PrjMgr_SaveTitle";

		internal const string PrjMgr_OpenTitle = "PrjMgr_OpenTitle";

		internal const string PrjMgr_OpenFilter = "PrjMgr_OpenFilter";

		internal const string PrjView_NoPrjName = "PrjView_NoPrjName";

		internal const string PrjView_PrjViewHeader = "PrjView_PrjViewHeader";

		internal const string MsgView_Count = "MsgView_Count";

		internal const string MsgView_Header0 = "MsgView_Header0";

		internal const string MsgView_Header1 = "MsgView_Header1";

		internal const string MsgView_Header2 = "MsgView_Header2";

		internal const string MsgView_Header3 = "MsgView_Header3";

		internal const string MsgView_Header4 = "MsgView_Header4";

		internal const string MsgView_CountTip = "MsgView_CountTip";

		internal const string CF_Prefix = "CF_Prefix";

		internal const string CF_Sep = "CF_Sep";

		internal const string CF_Equal = "CF_Equal";

		internal const string CF_Ext = "CF_Ext";

		internal const string CF_None = "CF_None";

		internal const string CF_Err1 = "CF_Err1";

		internal const string CF_Err2 = "CF_Err2";

		internal const string CF_Err3 = "CF_Err3";

		internal const string CF_Err4 = "CF_Err4";

		internal const string CF_Err5 = "CF_Err5";

		internal const string CF_Err7 = "CF_Err7";

		internal const string CF_Err8 = "CF_Err8";

		internal const string CF_Err9 = "CF_Err9";

		internal const string CF_Err10 = "CF_Err10";

		internal const string CF_Err11 = "CF_Err11";

		internal const string CF_Err12 = "CF_Err12";

		internal const string CF_Err13 = "CF_Err13";

		internal const string CF_Err14 = "CF_Err14";

		internal const string CF_Err15 = "CF_Err15";

		internal const string CF_Err16 = "CF_Err16";

		internal const string CF_Err17 = "CF_Err17";

		internal const string CF_Err18 = "CF_Err18";

		internal const string CF_Err19 = "CF_Err19";

		internal const string CF_InvalidFilterFile = "CF_InvalidFilterFile";

		internal const string CF_NoInvalidFilterName = "CF_NoInvalidFilterName";

		internal const string CF_InvalidFilter = "CF_InvalidFilter";

		internal const string CF_Type1 = "CF_Type1";

		internal const string CF_Type2 = "CF_Type2";

		internal const string CF_Type3 = "CF_Type3";

		internal const string CF_LeftB = "CF_LeftB";

		internal const string CF_RightB = "CF_RightB";

		internal const string CF_NoNodeName = "CF_NoNodeName";

		internal const string CF_InvalidLogic = "CF_InvalidLogic";

		internal const string CF_DlgDescription = "CF_DlgDescription";

		internal const string CFIP_Title = "CFIP_Title";

		internal const string CFIP_Filter = "CFIP_Filter";

		internal const string CFEP_Title = "CFEP_Title";

		internal const string CFEP_Filter = "CFEP_Filter";

		internal const string FO_MSG1 = "FO_MSG1";

		internal const string FO_Title = "FO_Title";

		internal const string FO_O1 = "FO_O1";

		internal const string FO_O2 = "FO_O2";

		internal const string FO_O3 = "FO_O3";

		internal const string FO_O4 = "FO_O4";

		internal const string FO_O5 = "FO_O5";

		internal const string FO_O6 = "FO_O6";

		internal const string FO_Description = "FO_Description";

		internal const string FO_FilterOptionDlg = "FO_FilterOptionDlg";

		internal const string CF_ExpressionHeader = "CF_ExpressionHeader";

		internal const string CF_OperationHeader = "CF_OperationHeader";

		internal const string CF_ValueHeader = "CF_ValueHeader";

		internal const string CF_ParameterHeader = "CF_ParameterHeader";

		internal const string CF_PTHeader = "CF_PTHeader";

		internal const string CF_DescriptionHeader = "CF_DescriptionHeader";

		internal const string CF_ACFTitle = "CF_ACFTitle";

		internal const string CF_Ibt = "CF_Ibt";

		internal const string CF_Ebt = "CF_Ebt";

		internal const string CF_Delete = "CF_Delete";

		internal const string CF_New = "CF_New";

		internal const string CF_NameH = "CF_NameH";

		internal const string CF_DescriptionH = "CF_DescriptionH";

		internal const string CF_XEH = "CF_XEH";

		internal const string CF_CFTitle = "CF_CFTitle";

		internal const string CF_FD = "CF_FD";

		internal const string CF_FN = "CF_FN";

		internal const string CF_XPathExpression = "CF_XPathExpression";

		internal const string CF_XPathExpression2 = "CF_XPathExpression2";

		internal const string CF_Namespaces = "CF_Namespaces";

		internal const string CF_Add = "CF_Add";

		internal const string CF_Remove = "CF_Remove";

		internal const string CF_PrefixHeader = "CF_PrefixHeader";

		internal const string CF_NamespaceHeader = "CF_NamespaceHeader";

		internal const string CF_ParametersGP = "CF_ParametersGP";

		internal const string CF_PFLL = "CF_PFLL";

		internal const string CF_NSLL = "CF_NSLL";

		internal const string CF_AFC = "CF_AFC";

		internal const string CF_Attr2 = "CF_Attr2";

		internal const string CF_IPError = "CF_IPError";

		internal const string CF_NewCustomFilterDlgName = "CF_NewCustomFilterDlgName";

		internal const string XML_MessageLogTraceRecordStart = "XML_MessageLogTraceRecordStart";

		internal const string XML_MessageLogTraceRecordStop = "XML_MessageLogTraceRecordStop";

		internal const string Btn_OK = "Btn_OK";

		internal const string Btn_Cancel = "Btn_Cancel";

		internal const string DlgTitleError = "DlgTitleError";

		internal const string SL_ATitle = "SL_ATitle";

		internal const string SL_ShowA = "SL_ShowA";

		internal const string SL_Options = "SL_Options";

		internal const string SL_XSmall = "SL_XSmall";

		internal const string SL_Small = "SL_Small";

		internal const string SL_Normal = "SL_Normal";

		internal const string SL_Zoom = "SL_Zoom";

		internal const string SL_DateTimeMinSep = "SL_DateTimeMinSep";

		internal const string SL_ExecutionSep = "SL_ExecutionSep";

		internal const string SL_ShowVerbose = "SL_ShowVerbose";

		internal const string SL_ERROR_LOAD_TRACE = "SL_ERROR_LOAD_TRACE";

		internal const string SL_InvalidTransfer = "SL_InvalidTransfer";

		internal const string SL_NOTANALYSIS = "SL_NOTANALYSIS";

		internal const string SL_TimeMillSecondSep = "SL_TimeMillSecondSep";

		internal const string SL_UnknownException = "SL_UnknownException";

		internal const string SL_BackwardTip = "SL_BackwardTip";

		internal const string SL_ForwardTip = "SL_ForwardTip";

		internal const string SL_FailExpandTransfer = "SL_FailExpandTransfer";

		internal const string SL_FailCollapseTransfer = "SL_FailCollapseTransfer";

		internal const string SL_ProcessList = "SL_ProcessList";

		internal const string SL_ProcessListTip = "SL_ProcessListTip";

		internal const string SL_ProcessList2 = "SL_ProcessList2";

		internal const string SL_ProcessListTip2 = "SL_ProcessListTip2";

		internal const string SL_HideProcess = "SL_HideProcess";

		internal const string SL_OutOfSize = "SL_OutOfSize";

		internal const string SL_ZoomTip = "SL_ZoomTip";

		internal const string SL_OptionTip = "SL_OptionTip";

		internal const string SL_NormalTip = "SL_NormalTip";

		internal const string SL_SmallTip = "SL_SmallTip";

		internal const string SL_XSmallTip = "SL_XSmallTip";

		internal const string SL_ShowATip = "SL_ShowATip";

		internal const string SL_ShowVerboseTip = "SL_ShowVerboseTip";

		internal const string SL_GraphEmpty = "SL_GraphEmpty";

		internal const string EI_ToString1 = "EI_ToString1";

		internal const string EI_ToString2 = "EI_ToString2";

		internal const string EI_ToString3 = "EI_ToString3";

		internal const string LE_Title = "LE_Title";

		internal const string LE_OffsetCol = "LE_OffsetCol";

		internal const string LE_SourceCol = "LE_SourceCol";

		internal const string LE_DescriptionCol = "LE_DescriptionCol";

		internal const string LE_GErrors = "LE_GErrors";

		internal const string LE_GPFError = "LE_GPFError";

		internal const string LE_GPTError = "LE_GPTError";

		internal const string LE_GPUError = "LE_GPUError";

		internal const string About_Title = "About_Title";

		internal const string About_FailSysInfo = "About_FailSysInfo";

		internal const string About_ProduceName = "About_ProduceName";

		internal const string About_Version2 = "About_Version2";

		internal const string About_Copyright = "About_Copyright";

		internal const string About_CName = "About_CName";

		internal const string About_Description = "About_Description";

		internal const string About_SysInfo = "About_SysInfo";

		internal const string About_OK = "About_OK";

		internal const string RegisterFail = "RegisterFail";

		internal const string UnhandledSysException = "UnhandledSysException";

		internal const string UnhandledException = "UnhandledException";

		internal const string STVClosing = "STVClosing";

		internal const string IgnoreException = "IgnoreException";

		internal const string BtnRestore = "BtnRestore";

		internal const string Filter_TP_Restore = "Filter_TP_Restore";

		internal const string MoreDataTag = "MoreDataTag";

		internal const string MsgCmdLineInfo_Body = "MsgCmdLineInfo_Body";

		internal const string MsgSearchPatternNotSupported = "MsgSearchPatternNotSupported";

		internal const string FV_ERROR = "FV_ERROR";

		internal const string FV_PROPERTY_HEADER = "FV_PROPERTY_HEADER";

		internal const string FV_EQUAL = "FV_EQUAL";

		internal const string FV_List_NameCol = "FV_List_NameCol";

		internal const string FV_List_ValueCol = "FV_List_ValueCol";

		internal const string FV_List_MethodCol = "FV_List_MethodCol";

		internal const string FV_List_TypeCol = "FV_List_TypeCol";

		internal const string FV_Basic_ActivityID = "FV_Basic_ActivityID";

		internal const string FV_Basic_ActivityName = "FV_Basic_ActivityName";

		internal const string FV_Basic_RelatedActivityID = "FV_Basic_RelatedActivityID";

		internal const string FV_Basic_RelatedActivityName = "FV_Basic_RelatedActivityName";

		internal const string FV_Basic_Time = "FV_Basic_Time";

		internal const string FV_Basic_Level = "FV_Basic_Level";

		internal const string FV_Basic_Source = "FV_Basic_Source";

		internal const string FV_Basic_Process = "FV_Basic_Process";

		internal const string FV_Basic_Thread = "FV_Basic_Thread";

		internal const string FV_Basic_Computer = "FV_Basic_Computer";

		internal const string FV_Basic_TraceIdentifier = "FV_Basic_TraceIdentifier";

		internal const string FV_Diag_Callstack = "FV_Diag_Callstack";

		internal const string FV_Diag_Method = "FV_Diag_Method";

		internal const string FV_Diag_Properties = "FV_Diag_Properties";

		internal const string FV_Exp_ExpTree = "FV_Exp_ExpTree";

		internal const string FV_Exp_ExpInfo = "FV_Exp_ExpInfo";

		internal const string FV_Exp_StackTrace = "FV_Exp_StackTrace";

		internal const string FV_Exp_NativeCode = "FV_Exp_NativeCode";

		internal const string FV_Exp_Message = "FV_Exp_Message";

		internal const string FV_Exp_ExpType = "FV_Exp_ExpType";

		internal const string FV_MSG_MSGHEADER = "FV_MSG_MSGHEADER";

		internal const string FV_MSG_MSGID = "FV_MSG_MSGID";

		internal const string FV_MSG_RelatesTo = "FV_MSG_RelatesTo";

		internal const string FV_MSG2_PROPERTY = "FV_MSG2_PROPERTY";

		internal const string FV_MSG2_HEADERS = "FV_MSG2_HEADERS";

		internal const string FV_MSG2_ACTION = "FV_MSG2_ACTION";

		internal const string FV_MSG2_TO = "FV_MSG2_TO";

		internal const string FV_MSG2_FROM = "FV_MSG2_FROM";

		internal const string FV_MSG2_ActivityId = "FV_MSG2_ActivityId";

		internal const string FV_MSG2_ActivityName = "FV_MSG2_ActivityName";

		internal const string FV_MSG2_ActivityName2 = "FV_MSG2_ActivityName2";

		internal const string FV_MSG2_HeaderTree = "FV_MSG2_HeaderTree";

		internal const string FV_MSG2_ReplyTo = "FV_MSG2_ReplyTo";

		internal const string FV_MSG2_LeftQ = "FV_MSG2_LeftQ";

		internal const string FV_MSG2_RightQ = "FV_MSG2_RightQ";

		internal const string FV_MSG2_GMsgInfo = "FV_MSG2_GMsgInfo";

		internal const string FV_MSG2_Properties = "FV_MSG2_Properties";

		internal const string FV_MSG2_MsgType = "FV_MSG2_MsgType";

		internal const string FV_MSG2_MsgSource = "FV_MSG2_MsgSource";

		internal const string FV_MSG2_MsgTime = "FV_MSG2_MsgTime";

		internal const string FV_MSG2_EnvelopeInfo = "FV_MSG2_EnvelopeInfo";

		internal const string FV_MSG2_Parameters = "FV_MSG2_Parameters";

		internal const string FV_MSG2_Method = "FV_MSG2_Method";

		internal const string FV_MSG2_Headers2 = "FV_MSG2_Headers2";

		internal const string FV_Basic_Title = "FV_Basic_Title";

		internal const string FV_Diag_Title = "FV_Diag_Title";

		internal const string FV_Exp_Title = "FV_Exp_Title";

		internal const string FV_List_Title = "FV_List_Title";

		internal const string FV_MSG_Title = "FV_MSG_Title";

		internal const string FV_MSG2_Title = "FV_MSG2_Title";

		internal const string FV_Options = "FV_Options";

		internal const string FV_OptionsTip = "FV_OptionsTip";

		internal const string FV_BasicInfoOption = "FV_BasicInfoOption";

		internal const string FV_BasicInfoOptionTip = "FV_BasicInfoOptionTip";

		internal const string FV_DiagInfoOption = "FV_DiagInfoOption";

		internal const string FV_DiagInfoOptionTip = "FV_DiagInfoOptionTip";

		internal const string FV_Error_Init = "FV_Error_Init";

		internal const string FV_AppDataText = "FV_AppDataText";

		internal const string FV_AppDataPartName = "FV_AppDataPartName";

		internal const string MsgSearchPatternAccessDenied = "MsgSearchPatternAccessDenied";

		internal const string MsgSearchPatternPathIsFile = "MsgSearchPatternPathIsFile";

		internal const string MsgSearchPatternDirectoryNotFound = "MsgSearchPatternDirectoryNotFound";

		internal const string FV_DateTimeFormat = "FV_DateTimeFormat";

		private static SR loader;

		private ResourceManager resources;

		private static CultureInfo Culture => null;

		public static ResourceManager Resources => GetLoader().resources;

		internal SR()
		{
			resources = new ResourceManager("SvcTraceViewer", GetType().Assembly);
		}

		private static SR GetLoader()
		{
			if (loader == null)
			{
				SR value = new SR();
				Interlocked.CompareExchange(ref loader, value, null);
			}
			return loader;
		}

		public static string GetString(string name, params object[] args)
		{
			SR sR = GetLoader();
			if (sR == null)
			{
				return null;
			}
			string @string = sR.resources.GetString(name, Culture);
			if (args != null && args.Length != 0)
			{
				for (int i = 0; i < args.Length; i++)
				{
					string text = args[i] as string;
					if (text != null && text.Length > 1024)
					{
						args[i] = text.Substring(0, 1021) + "...";
					}
				}
				return string.Format(CultureInfo.CurrentCulture, @string, args);
			}
			return @string;
		}

		public static string GetString(string name)
		{
			return GetLoader()?.resources.GetString(name, Culture);
		}

		public static string GetString(string name, out bool usedFallback)
		{
			usedFallback = false;
			return GetString(name);
		}

		public static object GetObject(string name)
		{
			return GetLoader()?.resources.GetObject(name, Culture);
		}
	}
}
