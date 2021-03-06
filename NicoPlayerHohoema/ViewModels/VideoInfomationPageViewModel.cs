﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Reactive.Bindings;
using Prism.Commands;
using NicoPlayerHohoema.Models.Helpers;
using Windows.ApplicationModel.DataTransfer;
using System.Threading;
using System.Diagnostics;
using Mntone.Nico2;
using Mntone.Nico2.Embed.Ichiba;
using Mntone.Nico2.Videos.WatchAPI;
using Mntone.Nico2.Videos.Dmc;
using System.Text.RegularExpressions;
using Windows.System;
using NicoPlayerHohoema.Models.Cache;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Models.LocalMylist;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Services;
using Prism.Navigation;
using NicoPlayerHohoema.Services.Page;

namespace NicoPlayerHohoema.ViewModels
{
    public class VideoInfomationPageViewModel : HohoemaViewModelBase, Interfaces.IVideoContent, INavigatedAwareAsync
    {
        public VideoInfomationPageViewModel(
            NGSettings ngSettings,
            Models.NiconicoSession niconicoSession,
            UserMylistManager userMylistManager,
            Services.HohoemaPlaylist hohoemaPlaylist,
            NicoVideoProvider nicoVideoProvider,
            LoginUserMylistProvider loginUserMylistProvider,
            VideoCacheManager videoCacheManager,
            Models.NicoVideoStreamingSessionProvider nicoVideo,
            Services.Helpers.MylistHelper mylistHelper,
            Services.PageManager pageManager,
            Services.NotificationService notificationService,
            Services.DialogService dialogService,
            Services.ExternalAccessService externalAccessService,
            Commands.AddMylistCommand addMylistCommand,
            Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand,
            Services.ThemeManagerService themeManagerService)
        {
            NgSettings = ngSettings;
            NiconicoSession = niconicoSession;
            UserMylistManager = userMylistManager;
            HohoemaPlaylist = hohoemaPlaylist;
            NicoVideoProvider = nicoVideoProvider;
            LoginUserMylistProvider = loginUserMylistProvider;
            VideoCacheManager = videoCacheManager;
            NicoVideo = nicoVideo;
            MylistHelper = mylistHelper;
            PageManager = pageManager;
            NotificationService = notificationService;
            DialogService = dialogService;
            ExternalAccessService = externalAccessService;
            AddMylistCommand = addMylistCommand;
            CreateSubscriptionGroupCommand = createSubscriptionGroupCommand;
            NowLoading = new ReactiveProperty<bool>(false);
            IsLoadFailed = new ReactiveProperty<bool>(false);
            ThemeManagerService = themeManagerService;

            ThemeManagerService.ActualAppThemeChanged += ThemeManagerService_ActualAppThemeChanged;
        }

        Database.NicoVideo _VideoInfo;

        public NicoVideoStreamingSessionProvider NicoVideo { get; private set; }

        public Uri DescriptionHtmlFileUri { get; private set; }

        public string VideoId { get; private set; }

        public string VideoTitle { get; private set; }

        public string ThumbnailUrl { get; private set; }

        public IList<TagViewModel> Tags { get; private set; }

        public bool IsChannelOwnedVideo { get; private set; }
        public string ProviderName { get; private set; }
        public string ProviderId { get; private set; }
        public string OwnerIconUrl { get; private set; }

        public TimeSpan VideoLength { get; private set; }
        
        public DateTime SubmitDate { get; private set; }

        public uint ViewCount { get; private set; }
        public uint CommentCount { get; private set; }
        public uint MylistCount { get; private set; }

        public ReactiveProperty<bool> NowLoading { get; private set; }
        public ReactiveProperty<bool> IsLoadFailed { get; private set; }

        public List<IchibaItem> IchibaItems { get; private set; }

        public bool IsSelfZoningContent { get; private set; }
        public NGResult SelfZoningInfo { get; private set; }

        
        private DelegateCommand _OpenFilterSettingPageCommand;
        public DelegateCommand OpenFilterSettingPageCommand
        {
            get
            {
                return _OpenFilterSettingPageCommand
                    ?? (_OpenFilterSettingPageCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPage(HohoemaPageType.Settings);
                    }
                    ));
            }
        }


        private DelegateCommand _OpenOwnerUserPageCommand;
        public DelegateCommand OpenOwnerUserPageCommand
        {
            get
            {
                return _OpenOwnerUserPageCommand
                    ?? (_OpenOwnerUserPageCommand = new DelegateCommand(() =>
                    {
                        if (_VideoInfo.Owner.UserType == Database.NicoVideoUserType.User)
                        {
                            PageManager.OpenPageWithId(HohoemaPageType.UserInfo, _VideoInfo.Owner.OwnerId);
                        }
                    }
                    , () => _VideoInfo?.Owner.UserType == Database.NicoVideoUserType.User
                    ));
            }
        }


        private DelegateCommand _OpenOwnerUserVideoPageCommand;
        public DelegateCommand OpenOwnerUserVideoPageCommand
        {
            get
            {
                return _OpenOwnerUserVideoPageCommand
                    ?? (_OpenOwnerUserVideoPageCommand = new DelegateCommand(() =>
                    {
                        if (_VideoInfo.Owner.UserType == Database.NicoVideoUserType.User)
                        {
                            PageManager.OpenPageWithId(HohoemaPageType.UserVideo, _VideoInfo.Owner.OwnerId);
                        }
                        else if (IsChannelOwnedVideo)
                        {
                            PageManager.OpenPageWithId(HohoemaPageType.ChannelVideo, _VideoInfo.Owner.OwnerId);
                        }
                    }
                    ));
            }
        }


        private DelegateCommand _PlayVideoCommand;
        public DelegateCommand PlayVideoCommand
        {
            get
            {
                return _PlayVideoCommand
                    ?? (_PlayVideoCommand = new DelegateCommand(() =>
                    {
                        HohoemaPlaylist.PlayVideo(VideoId);
                    }
                    ));
            }
        }

        private DelegateCommand _ShareCommand;
        public DelegateCommand ShareCommand
        {
            get
            {
                return _ShareCommand
                    ?? (_ShareCommand = new DelegateCommand(() =>
                    {
                        Services.Helpers.ShareHelper.Share(_VideoInfo);
                    }
                    , () => DataTransferManager.IsSupported()
                    ));
            }
        }

        private DelegateCommand _VideoInfoCopyToClipboardCommand;
        public DelegateCommand VideoInfoCopyToClipboardCommand
        {
            get
            {
                return _VideoInfoCopyToClipboardCommand
                    ?? (_VideoInfoCopyToClipboardCommand = new DelegateCommand(() =>
                    {
                        Services.Helpers.ClipboardHelper.CopyToClipboard(_VideoInfo);
                    }
                    ));
            }
        }

        private DelegateCommand<object> _ScriptNotifyCommand;
        public DelegateCommand<object> ScriptNotifyCommand
        {
            get
            {
                return _ScriptNotifyCommand
                    ?? (_ScriptNotifyCommand = new DelegateCommand<object>(async (parameter) =>
                    {
                        Uri url = parameter as Uri ?? (parameter as HyperlinkItem)?.Url;
                        if (url != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"script notified: {url}");

                            if (false == PageManager.OpenPage(url))
                            {
                                await Launcher.LaunchUriAsync(url);
                            }
                        }
                    }));
            }
        }


        private DelegateCommand<string> _AddPlaylistCommand;
        public DelegateCommand<string> AddPlaylistCommand
        {
            get
            {
                return _AddPlaylistCommand
                    ?? (_AddPlaylistCommand = new DelegateCommand<string>(async (playlistId) =>
                    {
                        var playlist = await MylistHelper.FindMylist(playlistId);

                        if (playlist is Interfaces.IUserOwnedMylist userOwnedMylist)
                        {
                            var result = await userOwnedMylist.AddMylistItem(VideoId);
                        }
                    }));
            }
        }


        private DelegateCommand _UpdateCommand;
        public DelegateCommand UpdateCommand
        {
            get
            {
                return _UpdateCommand
                    ?? (_UpdateCommand = new DelegateCommand(async () =>
                    {
                        await UpdateVideoDescription();
                    }));
            }
        }

        Regex GeneralUrlRegex = new Regex(@"https?:\/\/([a-zA-Z0-9.\/?=_-]*)");
        public List<HyperlinkItem> VideoDescriptionHyperlinkItems { get; } = new List<HyperlinkItem>();
       

        Models.Helpers.AsyncLock _UpdateLock = new AsyncLock();

        public List<VideoInfoControlViewModel> RelatedVideos { get; private set; }


        public Services.NotificationService NotificationService { get; }
        public NGSettings NgSettings { get; }
        public Models.NiconicoSession NiconicoSession { get; }
        public UserMylistManager UserMylistManager { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public LoginUserMylistProvider LoginUserMylistProvider { get; }
        public VideoCacheManager VideoCacheManager { get; }
        public Services.Helpers.MylistHelper MylistHelper { get; }
        public PageManager PageManager { get; }
        public Services.DialogService DialogService { get; }
        public Services.ExternalAccessService ExternalAccessService { get; }
        public Commands.AddMylistCommand AddMylistCommand { get; }
        public Commands.Subscriptions.CreateSubscriptionGroupCommand CreateSubscriptionGroupCommand { get; }

        string IVideoContent.ProviderId => _VideoInfo.Owner?.OwnerId;

        string IVideoContent.ProviderName => _VideoInfo.Owner?.ScreenName;

        Database.NicoVideoUserType IVideoContent.ProviderType => _VideoInfo.Owner.UserType;

        string INiconicoObject.Id => _VideoInfo.RawVideoId;

        string INiconicoObject.Label => _VideoInfo.Title;

        IMylist IVideoContent.OnwerPlaylist => null;

        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            NowLoading.Value = true;
            IsLoadFailed.Value = false;

            try
            {
                if (parameters.TryGetValue("id", out string videoId))
                {
                    VideoId = videoId;

                    if (VideoId == null)
                    {
                        IsLoadFailed.Value = true;
                        throw new Exception();
                    }

                    _VideoInfo = await NicoVideoProvider.GetNicoVideoInfo(VideoId);

                    await UpdateVideoDescription();

                    UpdateSelfZoning();

                    OpenOwnerUserPageCommand.RaiseCanExecuteChanged();
                    OpenOwnerUserVideoPageCommand.RaiseCanExecuteChanged();
                }                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                NowLoading.Value = false;
            }

        }


        bool _IsInitializedIchibaItems = false;
        public async void InitializeIchibaItems()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (_IsInitializedIchibaItems) { return; }

                var ichiba = await NiconicoSession.Context.Embed.GetIchiba(VideoId);
                IchibaItems = ichiba.GetMainIchibaItems();

                RaisePropertyChanged(nameof(IchibaItems));

                _IsInitializedIchibaItems = true;
            }
        }



        bool _IsInitializedRelatedVideos = false;
        public async void InitializeRelatedVideos()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (_IsInitializedRelatedVideos) { return; }

                var items = await NiconicoSession.Context.Video.GetRelatedVideoAsync(VideoId, 0, 10, Sort.Relation);
                
                RelatedVideos = items.Video_info?.Select(x =>
                {
                    var video = Database.NicoVideoDb.Get(x.Video.Id);
                    video.Title = x.Video.Title;
                    video.ThumbnailUrl = x.Video.Thumbnail_url;

                    var vm = new VideoInfoControlViewModel(video);
                    return vm;
                })
                .ToList();

                RaisePropertyChanged(nameof(RelatedVideos));

                _IsInitializedRelatedVideos = true;
            }
        }
        private async Task UpdateVideoDescription()
        {
            var videoDescriptionHtml = string.Empty;

            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (VideoId == null)
                {
                    return;
                }

                IsLoadFailed.Value = false;

                try
                {
                    var res = await NicoVideo.VisitWatchPage(VideoId);

                    if (res is WatchApiResponse)
                    {
                        var watchApi = res as WatchApiResponse;

                        VideoTitle = watchApi.videoDetail.title;
                        Tags = watchApi.videoDetail.tagList.Select(x => new TagViewModel(x.tag))
                            .ToList();
                        ThumbnailUrl = watchApi.videoDetail.thumbnail;
                        VideoLength = TimeSpan.FromSeconds(watchApi.videoDetail.length.Value);
                        SubmitDate = DateTime.Parse(watchApi.videoDetail.postedAt);
                        ViewCount = (uint)watchApi.videoDetail.viewCount.Value;
                        CommentCount = (uint)watchApi.videoDetail.commentCount.Value;
                        MylistCount = (uint)watchApi.videoDetail.mylistCount.Value;
                        ProviderName = watchApi.UserName;
                        OwnerIconUrl = watchApi.UploaderInfo?.icon_url ?? watchApi.channelInfo?.icon_url;
                        IsChannelOwnedVideo = watchApi.channelInfo != null;

                        videoDescriptionHtml = watchApi.videoDetail.description;
                    }
                    else if (res is DmcWatchData)
                    {
                        var dmcWatchApi = (res as DmcWatchData).DmcWatchResponse;

                        VideoTitle = dmcWatchApi.Video.Title;
                        Tags = dmcWatchApi.Tags.Select(x => new TagViewModel(x.Name))
                            .ToList();
                        ThumbnailUrl = dmcWatchApi.Video.ThumbnailURL;
                        VideoLength = TimeSpan.FromSeconds(dmcWatchApi.Video.Duration);
                        SubmitDate = DateTime.Parse(dmcWatchApi.Video.PostedDateTime);
                        ViewCount = (uint)dmcWatchApi.Video.ViewCount;
                        CommentCount = (uint)dmcWatchApi.Thread.CommentCount;
                        MylistCount = (uint)dmcWatchApi.Video.MylistCount;
                        ProviderId = dmcWatchApi.Owner?.Nickname ?? dmcWatchApi.Channel?.Name;
                        ProviderName = dmcWatchApi.Owner?.Nickname ?? dmcWatchApi.Channel?.Name;
                        OwnerIconUrl = dmcWatchApi.Owner?.IconURL ?? dmcWatchApi.Channel?.IconURL;
                        IsChannelOwnedVideo = dmcWatchApi.Channel != null;

                        videoDescriptionHtml = dmcWatchApi.Video.Description;
                    }
                }
                catch
                {
                    IsLoadFailed.Value = true;
                    return;
                }



                RaisePropertyChanged(nameof(VideoTitle));
                RaisePropertyChanged(nameof(Tags));
                RaisePropertyChanged(nameof(ThumbnailUrl));
                RaisePropertyChanged(nameof(VideoLength));
                RaisePropertyChanged(nameof(SubmitDate));
                RaisePropertyChanged(nameof(ViewCount));
                RaisePropertyChanged(nameof(CommentCount));
                RaisePropertyChanged(nameof(MylistCount));
                RaisePropertyChanged(nameof(ProviderName));
                RaisePropertyChanged(nameof(OwnerIconUrl));

            }

            try
            {
                DescriptionHtmlFileUri = await Models.Helpers.HtmlFileHelper.PartHtmlOutputToCompletlyHtml(VideoId, videoDescriptionHtml);
                RaisePropertyChanged(nameof(DescriptionHtmlFileUri));
            }
            catch
            {
                IsLoadFailed.Value = true;
                return;
            }


            try
            {
                var htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(videoDescriptionHtml);
                var root = htmlDocument.DocumentNode;
                var anchorNodes = root.Descendants("a");

                foreach (var anchor in anchorNodes)
                {
                    VideoDescriptionHyperlinkItems.Add(new HyperlinkItem()
                    {
                        Label = anchor.InnerText,
                        Url = new Uri(anchor.Attributes["href"].Value)
                    });

                    Debug.WriteLine($"{anchor.InnerText} : {anchor.Attributes["href"].Value}");
                }

                var matches = GeneralUrlRegex.Matches(videoDescriptionHtml);
                foreach (var match in matches.Cast<Match>())
                {
                    if (!VideoDescriptionHyperlinkItems.Any(x => x.Url.OriginalString == match.Value))
                    {
                        VideoDescriptionHyperlinkItems.Add(new HyperlinkItem()
                        {
                            Label = match.Value,
                            Url = new Uri(match.Value)
                        });

                        Debug.WriteLine($"{match.Value} : {match.Value}");
                    }
                }

                RaisePropertyChanged(nameof(VideoDescriptionHyperlinkItems));

            }
            catch
            {
                Debug.WriteLine("動画説明からリンクを抜き出す処理に失敗");
            }

            
        }

        private void UpdateSelfZoning()
        {
            try
            {
                if (_VideoInfo != null)
                {
                    SelfZoningInfo = NgSettings.IsNgVideo(_VideoInfo);
                    IsSelfZoningContent = SelfZoningInfo != null;

                    RaisePropertyChanged(nameof(SelfZoningInfo));
                    RaisePropertyChanged(nameof(IsSelfZoningContent));
                }
            }
            catch
            {
                IsLoadFailed.Value = true;
                return;
            }
        }

        protected override bool TryGetHohoemaPin(out HohoemaPin pin)
        {
            pin = new HohoemaPin()
            {
                Label = VideoTitle,
                PageType = HohoemaPageType.VideoInfomation,
                Parameter = $"id={VideoId}"
            };

            return true;
        }

        private Services.ThemeManagerService ThemeManagerService;

        private async void ThemeManagerService_ActualAppThemeChanged(object sender, EventArgs e)
        {
            //Regenerate html contents according to app's actual theme

            Uri regeneratedHtmlFileUri;

            if(DescriptionHtmlFileUri != null)
            {
                regeneratedHtmlFileUri = await Models.Helpers.HtmlFileHelper.RegenerateHtml(VideoId);

                if (regeneratedHtmlFileUri != null)
                {
                    DescriptionHtmlFileUri = regeneratedHtmlFileUri;
                    RaisePropertyChanged(nameof(DescriptionHtmlFileUri));
                }
                else
                {
                    await UpdateVideoDescription();
                }
            }
            else
            {
                await UpdateVideoDescription();
            }
        }
    }


    public class HyperlinkItem
    {
        public string Label { get; set; }
        public Uri Url { get; set; }
    }
}
