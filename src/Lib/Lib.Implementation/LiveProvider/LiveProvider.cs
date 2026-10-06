// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bili.Adapter.Interfaces;
using Bili.Lib.Interfaces;
using Bili.Models.App.Args;
using Bili.Models.App.Other;
using Bili.Models.BiliBili;
using Bili.Models.Data.Live;
using Bili.Models.Data.Player;
using Bili.Models.Enums;
using Bili.Models.Enums.App;
using Newtonsoft.Json;
using static Bili.Models.App.Constants.ApiConstants;
using static Bili.Models.App.Constants.ServiceConstants;

namespace Bili.Lib
{
    /// <summary>
    /// 提供直播相关的数据操作.
    /// </summary>
    public partial class LiveProvider : ILiveProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LiveProvider"/> class.
        /// </summary>
        /// <param name="httpProvider">网络请求处理工具.</param>
        /// <param name="accountProvider">账户工具.</param>
        /// <param name="liveAdapter">直播数据适配工具.</param>
        /// <param name="communityAdapter">社区数据适配工具.</param>
        public LiveProvider(
            IHttpProvider httpProvider,
            IAccountProvider accountProvider,
            ILiveAdapter liveAdapter,
            ICommunityAdapter communityAdapter)
        {
            _httpProvider = httpProvider;
            _accountProvider = accountProvider;
            _liveAdapter = liveAdapter;
            _communityAdapter = communityAdapter;

            _feedPageNumber = 1;
            _partitionPageNumber = 1;
        }

        /// <inheritdoc/>
        public event EventHandler<LiveMessageEventArgs> MessageReceived;

        /// <inheritdoc/>
        public async Task<LiveFeedView> GetLiveFeedsAsync()
        {
            var queryParameters = new Dictionary<string, string>
            {
                { Query.Page, _feedPageNumber.ToString() },
                { Query.RelationPage, _feedPageNumber.ToString() },
                { Query.Scale, "2" },
                { Query.LoginEvent, "1" },
                { Query.Device, "phone" },
            };
            var request = await _httpProvider.GetRequestMessageAsync(HttpMethod.Get, Live.LiveFeed, queryParameters, RequestClientType.IOS);
            var response = await _httpProvider.SendAsync(request);
            var result = await _httpProvider.ParseAsync<ServerResponse<LiveFeedResponse>>(response);
            _feedPageNumber += 1;

            return _liveAdapter.ConvertToLiveFeedView(result.Data);
        }

        /// <inheritdoc/>
        public async Task<bool> EnterLiveRoomAsync(string roomId)
        {
            var queryParameters = new Dictionary<string, string>
            {
                { Query.RoomId, roomId },
                { Query.ActionKey, Query.AppKey },
            };

            var request = await _httpProvider.GetRequestMessageAsync(HttpMethod.Post, Live.EnterRoom, queryParameters, RequestClientType.IOS);
            var response = await _httpProvider.SendAsync(request);
            var data = await _httpProvider.ParseAsync<ServerResponse>(response);
            if (data.IsSuccess())
            {
                ConnectLiveSocket();
                if (_liveConnectionTask != null)
                {
                    await _liveConnectionTask.ContinueWith(async result =>
                    {
                        if (result.IsCompleted)
                        {
                            await SendLiveMessageAsync(
                                new
                                {
                                    roomid = Convert.ToInt32(roomId),
                                    uid = _accountProvider.UserId,
                                },
                                7);
                        }
                    });
                }

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Models.Data.Community.Partition>> GetLiveAreaIndexAsync()
        {
            var queryParameters = new Dictionary<string, string>
            {
                { Query.Device, "phone" },
            };
            var request = await _httpProvider.GetRequestMessageAsync(HttpMethod.Get, Live.LiveArea, queryParameters, RequestClientType.IOS);
            var response = await _httpProvider.SendAsync(request);
            var result = await _httpProvider.ParseAsync<ServerResponse<LiveAreaResponse>>(response);

            return result.Data.List.Select(p => _communityAdapter.ConvertToPartition(p));
        }

        /// <inheritdoc/>
        public async Task<LivePartitionView> GetLiveAreaDetailAsync(string areaId, string parentId, string sortType)
        {
            var queryParameters = new Dictionary<string, string>
            {
                { Query.Page, _partitionPageNumber.ToString() },
                { Query.PageSizeUnderline, "40" },
                { Query.AreaId, areaId.ToString() },
                { Query.ParentAreaId, parentId.ToString() },
                { Query.Device, "phone" },
            };

            if (!string.IsNullOrEmpty(sortType))
            {
                queryParameters.Add(Query.SortType, sortType);
            }

            var request = await _httpProvider.GetRequestMessageAsync(HttpMethod.Get, Live.AreaDetail, queryParameters, RequestClientType.IOS);
            var response = await _httpProvider.SendAsync(request);
            var result = await _httpProvider.ParseAsync<ServerResponse<LiveAreaDetailResponse>>(response);
            var data = _liveAdapter.ConvertToLivePartitionView(result.Data);
            data.Id = areaId.ToString();
            _partitionPageNumber += 1;

            return data;
        }

        /// <inheritdoc/>
        public async Task<LiveMediaInformation> GetLiveMediaInformationAsync(string roomId, int quality, bool audioOnly)
        {
            var queryParameter = new Dictionary<string, string>
            {
                { Query.RoomId, roomId.ToString() },
                { Query.NoPlayUrl, "0" },
                { Query.Qn, quality.ToString() },
                { Query.Codec, Uri.EscapeDataString("0,1") },
                { Query.Device, "phone" },
                { Query.DeviceName, Uri.EscapeDataString("iPhone 13") },
                { Query.Dolby, "1" },
                { Query.Format, Uri.EscapeDataString("0,2") },
                { Query.Http, "1" },
                { Query.OnlyAudio, audioOnly ? "1" : "0" },
                { Query.OnlyVideo, "0" },
                { Query.Protocol, Uri.EscapeDataString("0,1") },
                { Query.NeedHdr, "0" },
                { Query.Mask, "0" },
                { Query.PlayType, "0" },
            };

            try
            {
                var request = await _httpProvider.GetRequestMessageAsync(HttpMethod.Get, Live.AppPlayInformation, queryParameter, RequestClientType.IOS);
                var response = await _httpProvider.SendAsync(request);
                var result = await _httpProvider.ParseAsync<ServerResponse<LiveAppPlayInformation>>(response);
                if (HasLivePlaybackData(result?.Data))
                {
                    return _liveAdapter.ConvertToLiveMediaInformation(result.Data);
                }
            }
            catch (ServiceException)
            {
            }

            return await GetLegacyLiveMediaInformationAsync(roomId, quality);
        }

        /// <inheritdoc/>
        public async Task<LivePlayerView> GetLiveRoomDetailAsync(string roomId)
        {
            var queryParameter = new Dictionary<string, string>
            {
                { Query.RoomId, roomId },
                { Query.Device, "phone" },
            };

            try
            {
                var request = await _httpProvider.GetRequestMessageAsync(
                    HttpMethod.Get,
                    Live.RoomDetail,
                    new Dictionary<string, string>(queryParameter),
                    RequestClientType.IOS,
                    needToken: false);
                var response = await _httpProvider.SendAsync(request);
                var result = await _httpProvider.ParseAsync<ServerResponse<LiveRoomDetail>>(response);
                if (HasLiveRoomDetail(result?.Data))
                {
                    return _liveAdapter.ConvertToLivePlayerView(result.Data);
                }
            }
            catch (ServiceException)
            {
            }

            try
            {
                var request = await _httpProvider.GetRequestMessageAsync(
                    HttpMethod.Get,
                    Live.WebRoomDetail,
                    new Dictionary<string, string> { { Query.RoomId, roomId } },
                    RequestClientType.Web,
                    needToken: false);
                var response = await _httpProvider.SendAsync(request);
                var webResult = await _httpProvider.ParseAsync<ServerResponse<LiveRoomDetail>>(response);
                if (HasLiveRoomDetail(webResult?.Data))
                {
                    return _liveAdapter.ConvertToLivePlayerView(webResult.Data);
                }
            }
            catch (ServiceException)
            {
            }

            return await GetLegacyLiveRoomDetailAsync(roomId);
        }

        /// <inheritdoc/>
        public async Task SendHeartBeatAsync()
        {
            if (_isLiveSocketConnected)
            {
                await SendLiveMessageAsync(string.Empty, 2);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SendDanmakuAsync(string roomId, string message, string color, bool isStandardSize, DanmakuLocation location)
        {
            var queryParameter = new Dictionary<string, string>
            {
                { Query.Cid, roomId.ToString() },
                { Query.MyId, _accountProvider.UserId.ToString() },
                { Query.MessageSlim, message },
                { Query.Rnd, DateTimeOffset.Now.ToLocalTime().ToUnixTimeMilliseconds().ToString() },
                { Query.Mode, ((int)location).ToString() },
                { Query.Pool, "0" },
                { Query.Type, "json" },
                { Query.Color, color },
                { Query.FontSize, isStandardSize ? "25" : "18" },
                { Query.PlayTime, "0.0" },
            };

            try
            {
                var request = await _httpProvider.GetRequestMessageAsync(HttpMethod.Post, Live.SendMessage, queryParameter, RequestClientType.IOS, needToken: true);
                var response = await _httpProvider.SendAsync(request);
                var result = await _httpProvider.ParseAsync<ServerResponse>(response);
                return result.IsSuccess();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"直播弹幕发送失败：{ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public void ResetPartitionDetailState()
            => _partitionPageNumber = 1;

        /// <inheritdoc/>
        public void ResetFeedState()
            => _feedPageNumber = 1;

        /// <inheritdoc/>
        public void ResetLiveConnection()
        {
            if (_liveCancellationToken != null)
            {
                _liveCancellationToken.Cancel();
            }

            _liveCancellationToken = new CancellationTokenSource();

            _isLiveSocketConnected = false;
            _liveWebSocket?.Stop(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, string.Empty);
            _liveWebSocket?.Dispose();
            _liveWebSocket = null;
        }

        private static bool HasLiveRoomDetail(LiveRoomDetail detail)
            => detail?.RoomInformation != null
                && detail.AnchorInformation?.UserBasicInformation != null;

        private static bool HasLivePlaybackData(LiveAppPlayInformation information)
            => information?.PlayUrlInfo?.PlayUrl?.Descriptions?.Count > 0
                && information.PlayUrlInfo.PlayUrl.StreamList?.Any(stream =>
                    stream.FormatList?.Any(format => format.CodecList?.Any(codec => codec.Urls?.Count > 0) == true) == true) == true;

        private static int GetLegacyQuality(int quality)
            => quality >= 400 ? 4 : quality >= 250 ? 3 : quality >= 150 ? 2 : 1;

        private async Task<LivePlayerView> GetLegacyLiveRoomDetailAsync(string roomId)
        {
            var roomRequest = await GetLegacyLiveRequestAsync(
                Live.LegacyRoomDetail,
                new Dictionary<string, string> { { Query.RoomId, roomId } });
            var roomResponse = await _httpProvider.SendAsync(roomRequest);
            var roomResult = await _httpProvider.ParseAsync<ServerResponse<LegacyLiveRoomInformation>>(roomResponse);
            if (roomResult?.IsSuccess() != true || roomResult.Data == null || roomResult.Data.UserId == 0)
            {
                throw new ServiceException(roomResult ?? new ServerResponse
                {
                    Code = -1,
                    Message = "无法获取直播间详情。",
                });
            }

            var anchorRequest = await GetLegacyLiveRequestAsync(
                Live.LegacyAnchorInformation,
                new Dictionary<string, string> { { "uid", roomResult.Data.UserId.ToString(CultureInfo.InvariantCulture) } });
            var anchorResponse = await _httpProvider.SendAsync(anchorRequest);
            var anchorResult = await _httpProvider.ParseAsync<ServerResponse<LegacyLiveAnchorResponse>>(anchorResponse);
            var userInfo = anchorResult?.Data?.Information;
            if (anchorResult?.IsSuccess() != true || userInfo == null)
            {
                throw new ServiceException(anchorResult ?? new ServerResponse
                {
                    Code = -1,
                    Message = "无法获取主播信息。",
                });
            }

            var liveStartTime = DateTimeOffset.TryParse(
                roomResult.Data.LiveTime,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var parsedLiveTime)
                ? (int)parsedLiveTime.ToUnixTimeSeconds()
                : 0;
            var detail = new LiveRoomDetail
            {
                RoomInformation = new LiveRoomInformation
                {
                    UserId = roomResult.Data.UserId,
                    RoomId = roomResult.Data.RoomId,
                    Title = roomResult.Data.Title,
                    Description = roomResult.Data.Description,
                    ViewerCount = roomResult.Data.ViewerCount,
                    LiveStatus = roomResult.Data.LiveStatus,
                    LiveStartTime = liveStartTime,
                    AreaName = roomResult.Data.AreaName,
                    ParentAreaName = roomResult.Data.ParentAreaName,
                    Cover = roomResult.Data.UserCover,
                    Keyframe = roomResult.Data.Keyframe,
                },
                AnchorInformation = new LiveAnchorInformation
                {
                    UserBasicInformation = new LiveUserBasicInformation
                    {
                        UserName = userInfo.UserName,
                        Avatar = userInfo.Avatar,
                    },
                },
            };

            return _liveAdapter.ConvertToLivePlayerView(detail);
        }

        private async Task<LiveMediaInformation> GetLegacyLiveMediaInformationAsync(string roomId, int quality)
        {
            var request = await GetLegacyLiveRequestAsync(
                Live.LegacyPlayUrl,
                new Dictionary<string, string>
                {
                    { "cid", roomId },
                    { "quality", GetLegacyQuality(quality).ToString(CultureInfo.InvariantCulture) },
                    { "platform", "web" },
                });
            var response = await _httpProvider.SendAsync(request);
            var result = await _httpProvider.ParseAsync<ServerResponse<LegacyLivePlayInformation>>(response);
            if (result?.IsSuccess() != true || result.Data == null || result.Data.Urls?.Count == 0)
            {
                throw new ServiceException(result ?? new ServerResponse
                {
                    Code = -1,
                    Message = "无法获取直播播放地址。",
                });
            }

            var formats = result.Data.QualityDescriptions?
                .Select(item => new FormatInformation(item.Quality, item.Description, false))
                .ToList() ?? new List<FormatInformation>();
            var urls = result.Data.Urls
                .Select(item => Uri.TryCreate(item.Url, UriKind.Absolute, out var uri)
                    ? new LivePlayUrl(uri.GetLeftPart(UriPartial.Authority), uri.PathAndQuery, string.Empty)
                    : null)
                .Where(item => item != null)
                .ToList();
            if (urls.Count == 0)
            {
                throw new ServiceException(new ServerResponse
                {
                    Code = -1,
                    Message = "直播播放地址格式无效。",
                });
            }

            var acceptQualities = formats.Select(item => item.Quality).ToList();
            var line = new LivePlaylineInformation(
                "avc",
                result.Data.CurrentQuality,
                acceptQualities,
                urls,
                "http_stream",
                "flv");
            return new LiveMediaInformation(roomId, formats, new[] { line });
        }

        private async Task<HttpRequestMessage> GetLegacyLiveRequestAsync(string url, Dictionary<string, string> parameters)
        {
            var request = await _httpProvider.GetRequestMessageAsync(
                HttpMethod.Get,
                url,
                parameters,
                RequestClientType.Web,
                needToken: false,
                forceNoToken: true);
            request.Headers.Referrer = new Uri("https://live.bilibili.com/");
            request.Headers.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            return request;
        }

        private sealed class LegacyLiveRoomInformation
        {
            [JsonProperty("uid")]
            public long UserId { get; set; }

            [JsonProperty("room_id")]
            public int RoomId { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("online")]
            public int ViewerCount { get; set; }

            [JsonProperty("live_status")]
            public int LiveStatus { get; set; }

            [JsonProperty("live_time")]
            public string LiveTime { get; set; }

            [JsonProperty("area_name")]
            public string AreaName { get; set; }

            [JsonProperty("parent_area_name")]
            public string ParentAreaName { get; set; }

            [JsonProperty("user_cover")]
            public string UserCover { get; set; }

            [JsonProperty("keyframe")]
            public string Keyframe { get; set; }
        }

        private sealed class LegacyLiveAnchorResponse
        {
            [JsonProperty("info")]
            public LegacyLiveUserInformation Information { get; set; }
        }

        private sealed class LegacyLiveUserInformation
        {
            [JsonProperty("uname")]
            public string UserName { get; set; }

            [JsonProperty("face")]
            public string Avatar { get; set; }
        }

        private sealed class LegacyLivePlayInformation
        {
            [JsonProperty("current_qn")]
            public int CurrentQuality { get; set; }

            [JsonProperty("quality_description")]
            public List<LegacyLiveQualityDescription> QualityDescriptions { get; set; }

            [JsonProperty("durl")]
            public List<LegacyLivePlayUrl> Urls { get; set; }
        }

        private sealed class LegacyLiveQualityDescription
        {
            [JsonProperty("qn")]
            public int Quality { get; set; }

            [JsonProperty("desc")]
            public string Description { get; set; }
        }

        private sealed class LegacyLivePlayUrl
        {
            [JsonProperty("url")]
            public string Url { get; set; }
        }
    }
}
