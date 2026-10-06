// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Bili.Lib.Interfaces;
using Bili.Models.Data.Player;
using Bili.Models.Enums;
using Bili.Toolkit.Interfaces;
using Bili.ViewModels.Interfaces.Toolbox;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Bili.ViewModels.Uwp.Toolbox
{
    /// <summary>
    /// 导出视频弹幕为 XML.
    /// </summary>
    public sealed partial class DanmakuExporterViewModel : ViewModelBase, IDanmakuExporterViewModel
    {
        private readonly IPlayerProvider _playerProvider;
        private readonly IVideoToolkit _videoToolkit;
        private readonly IResourceToolkit _resourceToolkit;
        private readonly CoreDispatcher _dispatcher;

        [ObservableProperty]
        private string _input;

        [ObservableProperty]
        private bool _isExporting;

        [ObservableProperty]
        private string _progressText;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private bool _isShowError;

        /// <summary>
        /// Initializes a new instance of the <see cref="DanmakuExporterViewModel"/> class.
        /// </summary>
        public DanmakuExporterViewModel(
            IPlayerProvider playerProvider,
            IVideoToolkit videoToolkit,
            IResourceToolkit resourceToolkit)
        {
            _playerProvider = playerProvider;
            _videoToolkit = videoToolkit;
            _resourceToolkit = resourceToolkit;
            _dispatcher = Window.Current.CoreWindow.Dispatcher;
            ExportCommand = new AsyncRelayCommand(ExportAsync);
            AttachIsRunningToAsyncCommand(p => IsExporting = p, ExportCommand);
            AttachExceptionHandlerToAsyncCommand(DisplayExAsync, ExportCommand);
        }

        /// <inheritdoc/>
        public IAsyncRelayCommand ExportCommand { get; }

        private async Task ExportAsync()
        {
            ErrorMessage = null;
            IsShowError = false;
            ProgressText = string.Empty;
            var input = ExtractVideoId(Input);
            var idType = _videoToolkit.GetVideoIdType(input, out var avid);
            if (idType == VideoIdType.Invalid)
            {
                throw new ArgumentException(_resourceToolkit.GetLocaleString(LanguageNames.InvalidVideoId));
            }

            var view = await _playerProvider.GetVideoDetailAsync(idType == VideoIdType.Bv ? input : avid);
            var parts = view.SubVideos?.ToList() ?? new List<Bili.Models.Data.Video.VideoIdentifier>();
            if (parts.Count == 0)
            {
                throw new InvalidDataException("未找到可导出的分P。");
            }

            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads,
            };
            folderPicker.FileTypeFilter.Add("*");
            var folder = await folderPicker.PickSingleFolderAsync().AsTask();
            if (folder == null)
            {
                return;
            }

            for (var partIndex = 0; partIndex < parts.Count; partIndex++)
            {
                var part = parts[partIndex];
                ProgressText = $"{partIndex + 1}/{parts.Count}: {part.Title}";
                var segmentCount = Math.Max(1, (int)Math.Ceiling(part.Duration / 360d));
                var danmakus = new List<DanmakuInformation>();
                for (var segment = 1; segment <= segmentCount; segment++)
                {
                    var items = await _playerProvider.GetSegmentDanmakuAsync(
                        view.Information.Identifier.Id,
                        part.Id,
                        segment);
                    danmakus.AddRange(items);
                }

                var fileName = SanitizeFileName($"{view.Information.Identifier.Title}_P{partIndex + 1}.xml");
                var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                var xml = CreateDanmakuXml(danmakus);
                await FileIO.WriteTextAsync(file, xml);
            }

            ProgressText = _resourceToolkit.GetLocaleString(LanguageNames.DanmakuExportCompleted);
        }

        private string ExtractVideoId(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var value = input.Trim();
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                var segments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                var videoIndex = Array.FindIndex(segments, p => string.Equals(p, "video", StringComparison.OrdinalIgnoreCase));
                if (videoIndex >= 0 && videoIndex + 1 < segments.Length)
                {
                    return segments[videoIndex + 1];
                }
            }

            return value;
        }

        private string CreateDanmakuXml(IEnumerable<DanmakuInformation> danmakus)
        {
            var builder = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                OmitXmlDeclaration = true,
            };
            using (var writer = XmlWriter.Create(builder, settings))
            {
                writer.WriteStartElement("i");
                writer.WriteElementString("chatserver", "chat.bilibili.com");
                writer.WriteElementString("chatid", "0");
                writer.WriteElementString("mission", "0");
                writer.WriteElementString("maxlimit", danmakus.Count().ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("state", "0");
                writer.WriteElementString("real_name", "0");
                writer.WriteElementString("source", "k-v");
                foreach (var item in danmakus)
                {
                    writer.WriteStartElement("d");
                    var rowId = long.TryParse(item.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId)
                        ? parsedId
                        : 0;
                    var attributes = string.Join(
                        ",",
                        item.StartPosition.ToString("0.###", CultureInfo.InvariantCulture),
                        item.Mode.ToString(CultureInfo.InvariantCulture),
                        item.FontSize.ToString(CultureInfo.InvariantCulture),
                        item.Color.ToString(CultureInfo.InvariantCulture),
                        "0",
                        "0",
                        "0",
                        rowId.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("p", attributes);
                    writer.WriteString(item.Content ?? string.Empty);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" + builder;
        }

        private string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars().Concat("<>:\"/\\|?*".ToCharArray()).ToArray();
            var name = string.Concat((value ?? string.Empty).Select(p => char.IsControl(p) || invalid.Contains(p) ? '_' : p)).Trim().TrimEnd('.');
            return string.IsNullOrWhiteSpace(name) ? "danmaku.xml" : name;
        }

        private async void DisplayExAsync(Exception ex)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ErrorMessage = ex.Message;
                IsShowError = true;
                ProgressText = string.Empty;
            });
        }
    }
}
