// Copyright (c) Richasy. All rights reserved.

using System;
using System.Linq;
using System.Threading.Tasks;
using Bili.Models.Data.Live;
using Bili.Models.Data.Player;
using Bili.Models.Enums;

namespace Bili.ViewModels.Uwp.Core
{
    /// <summary>
    /// 媒体播放器视图模型.
    /// </summary>
    public sealed partial class MediaPlayerViewModel
    {
        private void ResetLiveData()
            => _currentPlayline = default;

        private async Task LoadLiveAsync()
        {
            await InitializeLiveMediaInformationAsync();
            await InitializeOrginalLiveSourceAsync();
        }

        private async Task InitializeLiveMediaInformationAsync()
        {
            var view = _viewData as LivePlayerView;
            var quality = _currentPlayline != null
                ? _currentPlayline.Quality
                : _settingsToolkit.ReadLocalSetting(SettingNames.DefaultLiveFormat, 400);

            Cover = view.Information.Identifier.Cover.GetSourceUri().ToString();
            DanmakuViewModel.SetData(view.Information.Identifier.Id, default, _videoType);
            _liveMediaInformation = await _liveProvider.GetLiveMediaInformationAsync(view.Information.Identifier.Id, quality, IsLiveAudioOnly);

            var lines = _liveMediaInformation.Lines?.ToList() ?? new System.Collections.Generic.List<LivePlaylineInformation>();
            if (lines.Count == 0)
            {
                throw new System.InvalidOperationException(_resourceToolkit.GetLocaleString(LanguageNames.LiveStreamUnavailable));
            }

            if (_currentPlayline == null)
            {
                _currentPlayline = lines.FirstOrDefault(p => p.Quality == quality)
                    ?? lines.OrderByDescending(p => p.Quality).First();
            }
        }

        private async Task InitializeOrginalLiveSourceAsync()
        {
            var isVip = _accountViewModel.IsVip;
            if (isVip)
            {
                foreach (var item in _liveMediaInformation.Formats)
                {
                    item.IsLimited = false;
                }
            }

            foreach (var item in _liveMediaInformation.Formats)
            {
                if (!item.IsLimited)
                {
                    Formats.Add(item);
                }
            }

            var formatId = GetFormatId(true);
            var format = Formats.FirstOrDefault(p => p.Quality == formatId)
                ?? Formats.OrderByDescending(p => p.Quality).FirstOrDefault();
            if (format == null)
            {
                throw new System.InvalidOperationException(_resourceToolkit.GetLocaleString(LanguageNames.LiveStreamUnavailable));
            }

            await SelectLiveFormatAsync(format);
        }

        private async Task SelectLiveFormatAsync(FormatInformation format)
        {
            CurrentFormat = format;
            IsError = false;
            ErrorText = null;
            ResetPlayer();
            InitializePlayer();
            var view = _viewData as LivePlayerView;
            var codecId = GetLivePreferCodecId();
            var quality = format.Quality;
            _liveMediaInformation = await _liveProvider.GetLiveMediaInformationAsync(view.Information.Identifier.Id, quality, IsLiveAudioOnly);

            var lines = _liveMediaInformation.Lines?.ToList() ?? new System.Collections.Generic.List<LivePlaylineInformation>();
            var qualityLines = lines
                .Where(p => p.Quality == quality || (p.AcceptQualities?.Contains(quality) ?? false))
                .ToList();
            if (qualityLines.Count == 0)
            {
                qualityLines = lines;
            }

            var url = qualityLines
                .OrderBy(p => string.Equals(p.Name, codecId, System.StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(p => p.ProtocolName?.IndexOf("hls", System.StringComparison.OrdinalIgnoreCase) >= 0 ? 0 : 1)
                .ThenBy(p => string.Equals(p.FormatName, "ts", System.StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .SelectMany(p => p.Urls ?? Enumerable.Empty<LivePlayUrl>())
                .FirstOrDefault(p => Uri.TryCreate(p.ToString(), UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));

            if (url == null)
            {
                IsError = true;
                ErrorText = _resourceToolkit.GetLocaleString(LanguageNames.LiveStreamUnavailable);
                return;
            }

            _currentPlayline = qualityLines.FirstOrDefault(p => string.Equals(p.Name, codecId, System.StringComparison.OrdinalIgnoreCase))
                ?? qualityLines.FirstOrDefault();
            _settingsToolkit.WriteLocalSetting(SettingNames.DefaultLiveFormat, CurrentFormat.Quality);
            await InitializeLivePlayerAsync(url.ToString());
        }

        private async Task InitializeLivePlayerAsync(string url)
        {
            await _player.SetSourceAsync(url);
            StartTimers();
        }

        private async Task ChangeLiveAudioOnlyAsync(bool isAudioOnly)
        {
            IsLiveAudioOnly = isAudioOnly;
            _settingsToolkit.WriteLocalSetting(SettingNames.IsLiveAudioOnly, isAudioOnly);
            if (CurrentFormat != null)
            {
                await SelectLiveFormatAsync(CurrentFormat);
            }
        }

        private void FillLivePlaybackProperties()
        {
            var view = _viewData as LivePlayerView;
            SetDisplayProperties(
                view.Information.User.Avatar.GetSourceUri() + "@100w_100h_1c_100q.jpg",
                view.Information.Identifier.Title,
                string.Join(string.Empty, view.Information.Description.Take(20)),
                _videoType.ToString());
        }
    }
}
