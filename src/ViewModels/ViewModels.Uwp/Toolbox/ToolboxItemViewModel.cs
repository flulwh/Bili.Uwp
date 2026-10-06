// Copyright (c) Richasy. All rights reserved.

using Bili.Models.Enums;
using Bili.Toolkit.Interfaces;
using Bili.ViewModels.Interfaces.Toolbox;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bili.ViewModels.Uwp.Toolbox
{
    /// <summary>
    /// 工具箱条目视图模型.
    /// </summary>
    public sealed partial class ToolboxItemViewModel : ViewModelBase, IToolboxItemViewModel
    {
        private readonly IResourceToolkit _resourceToolkit;

        [ObservableProperty]
        private ToolboxItemType _type;

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _description;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolboxItemViewModel"/> class.
        /// </summary>
        public ToolboxItemViewModel(IResourceToolkit resourceToolkit)
            => _resourceToolkit = resourceToolkit;

        /// <inheritdoc/>
        public void SetType(ToolboxItemType type)
        {
            Type = type;
            switch (type)
            {
                case ToolboxItemType.DanmakuExporter:
                    Title = _resourceToolkit.GetLocaleString(LanguageNames.DanmakuExporter);
                    Description = _resourceToolkit.GetLocaleString(LanguageNames.DanmakuExporterDescription);
                    break;
                default:
                    break;
            }
        }
    }
}
