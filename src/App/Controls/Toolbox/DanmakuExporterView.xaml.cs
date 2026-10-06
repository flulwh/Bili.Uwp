// Copyright (c) Richasy. All rights reserved.

using Bili.DI.Container;
using Bili.ViewModels.Interfaces.Toolbox;

namespace Bili.App.Controls
{
    /// <summary>
    /// 弹幕导出工具.
    /// </summary>
    public sealed partial class DanmakuExporterView : CenterPopup
    {
        private readonly IDanmakuExporterViewModel _viewModel = Locator.Instance.GetService<IDanmakuExporterViewModel>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DanmakuExporterView"/> class.
        /// </summary>
        public DanmakuExporterView() => InitializeComponent();

        private void OnQuerySubmitted(Windows.UI.Xaml.Controls.AutoSuggestBox sender, Windows.UI.Xaml.Controls.AutoSuggestBoxQuerySubmittedEventArgs args)
            => _viewModel.ExportCommand.ExecuteAsync(null);
    }
}
