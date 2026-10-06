// Copyright (c) Richasy. All rights reserved.

using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bili.ViewModels.Interfaces.Toolbox
{
    /// <summary>
    /// 弹幕导出工具视图模型.
    /// </summary>
    public interface IDanmakuExporterViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// 视频 Id 或链接.
        /// </summary>
        string Input { get; set; }

        /// <summary>
        /// 是否正在导出.
        /// </summary>
        bool IsExporting { get; }

        /// <summary>
        /// 导出状态.
        /// </summary>
        string ProgressText { get; }

        /// <summary>
        /// 错误消息.
        /// </summary>
        string ErrorMessage { get; }

        /// <summary>
        /// 是否显示错误.
        /// </summary>
        bool IsShowError { get; }

        /// <summary>
        /// 导出弹幕命令.
        /// </summary>
        IAsyncRelayCommand ExportCommand { get; }
    }
}
