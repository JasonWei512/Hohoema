﻿using System;
using System.Collections.Generic;
using System.Linq;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Helpers;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Prism.Commands;
using Mntone.Nico2;
using System.Reactive.Linq;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.Models.Subscription;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Services;
using Prism.Navigation;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
    public class SearchResultKeywordPageViewModel : HohoemaListingPageViewModelBase<VideoInfoControlViewModel>, INavigatedAwareAsync
    {

        public SearchResultKeywordPageViewModel(
            NGSettings ngSettings,
            SearchProvider searchProvider,
            SubscriptionManager subscriptionManager,
            Services.HohoemaPlaylist hohoemaPlaylist,
            Services.PageManager pageManager,
            Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand
            )
        {
            FailLoading = new ReactiveProperty<bool>(false)
                .AddTo(_CompositeDisposable);

            LoadedPage = new ReactiveProperty<int>(1)
                .AddTo(_CompositeDisposable);

            SelectedSearchSort = new ReactiveProperty<SearchSortOptionListItem>(
                VideoSearchOptionListItems.First(),
                mode: ReactivePropertyMode.DistinctUntilChanged
                );

            SelectedSearchTarget = new ReactiveProperty<SearchTarget>();

            SelectedSearchSort
               .Subscribe(async _ =>
               {
                   var selected = SelectedSearchSort.Value;
                   if (SearchOption.Order == selected.Order
                       && SearchOption.Sort == selected.Sort
                   )
                   {
                       //                       return;
                   }

                   SearchOption.Sort = SelectedSearchSort.Value.Sort;
                   SearchOption.Order = SelectedSearchSort.Value.Order;

                   await ResetList();
               })
                .AddTo(_CompositeDisposable);
            NgSettings = ngSettings;
            SearchProvider = searchProvider;
            SubscriptionManager1 = subscriptionManager;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            CreateSubscriptionGroupCommand = createSubscriptionGroupCommand;
        }


        static private List<SearchSortOptionListItem> _VideoSearchOptionListItems = new List<SearchSortOptionListItem>()
        {
            new SearchSortOptionListItem()
            {
                Label = "投稿が新しい順",
                Order = Mntone.Nico2.Order.Descending,
                Sort = Sort.FirstRetrieve,
            },
            new SearchSortOptionListItem()
            {
                Label = "投稿が古い順",
                Order = Mntone.Nico2.Order.Ascending,
                Sort = Sort.FirstRetrieve,
            },

            new SearchSortOptionListItem()
            {
                Label = "コメントが新しい順",
                Order = Mntone.Nico2.Order.Descending,
                Sort = Sort.NewComment,
            },
            new SearchSortOptionListItem()
            {
                Label = "コメントが古い順",
                Order = Mntone.Nico2.Order.Ascending,
                Sort = Sort.NewComment,
            },

            new SearchSortOptionListItem()
            {
                Label = "再生数が多い順",
                Order = Mntone.Nico2.Order.Descending,
                Sort = Sort.ViewCount,
            },
            new SearchSortOptionListItem()
            {
                Label = "再生数が少ない順",
                Order = Mntone.Nico2.Order.Ascending,
                Sort = Sort.ViewCount,
            },

            new SearchSortOptionListItem()
            {
                Label = "コメント数が多い順",
                Order = Mntone.Nico2.Order.Descending,
                Sort = Sort.CommentCount,
            },
            new SearchSortOptionListItem()
            {
                Label = "コメント数が少ない順",
                Order = Mntone.Nico2.Order.Ascending,
                Sort = Sort.CommentCount,
            },


            new SearchSortOptionListItem()
            {
                Label = "再生時間が長い順",
                Order = Mntone.Nico2.Order.Descending,
                Sort = Sort.Length,
            },
            new SearchSortOptionListItem()
            {
                Label = "再生時間が短い順",
                Order = Mntone.Nico2.Order.Ascending,
                Sort = Sort.Length,
            },

            new SearchSortOptionListItem()
            {
                Label = "マイリスト数が多い順",
                Order = Mntone.Nico2.Order.Descending,
                Sort = Sort.MylistCount,
            },
            new SearchSortOptionListItem()
            {
                Label = "マイリスト数が少ない順",
                Order = Mntone.Nico2.Order.Ascending,
                Sort = Sort.MylistCount,
            },
			// V1APIだとサポートしてない
			/* 
			new SearchSortOptionListItem()
			{
				Label = "人気の高い順",
				Sort = Sort.Popurarity,
				Order = Mntone.Nico2.Order.Descending,
			},
			*/
		};

        public IReadOnlyList<SearchSortOptionListItem> VideoSearchOptionListItems => _VideoSearchOptionListItems;

        public ReactiveProperty<SearchSortOptionListItem> SelectedSearchSort { get; private set; }


        static public List<SearchTarget> SearchTargets { get; } = Enum.GetValues(typeof(SearchTarget)).Cast<SearchTarget>().ToList();

        public ReactiveProperty<SearchTarget> SelectedSearchTarget { get; }

        private DelegateCommand<SearchTarget?> _ChangeSearchTargetCommand;
        public DelegateCommand<SearchTarget?> ChangeSearchTargetCommand
        {
            get
            {
                return _ChangeSearchTargetCommand
                    ?? (_ChangeSearchTargetCommand = new DelegateCommand<SearchTarget?>(target =>
                    {
                        if (target.HasValue && target.Value != SearchOption.SearchTarget)
                        {
                            PageManager.Search(target.Value, SearchOption.Keyword);
                        }
                    }));
            }
        }

        public ReactiveProperty<bool> FailLoading { get; private set; }

		static public KeywordSearchPagePayloadContent SearchOption { get; private set; }
		public ReactiveProperty<int> LoadedPage { get; private set; }

        private string _SearchOptionText;
        public string SearchOptionText
        {
            get { return _SearchOptionText; }
            set { SetProperty(ref _SearchOptionText, value); }
        }


        public Models.Subscription.SubscriptionSource? SubscriptionSource => new Models.Subscription.SubscriptionSource(SearchOption.Keyword, Models.Subscription.SubscriptionSourceType.KeywordSearch, SearchOption.Keyword);
        

        public Database.Bookmark KeywordSearchBookmark { get; private set; }



		#region Commands


		private DelegateCommand _ShowSearchHistoryCommand;
		public DelegateCommand ShowSearchHistoryCommand
		{
			get
			{
				return _ShowSearchHistoryCommand
					?? (_ShowSearchHistoryCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.Search);
					}));
			}
		}

        public NGSettings NgSettings { get; }
        public SearchProvider SearchProvider { get; }
        public SubscriptionManager SubscriptionManager1 { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public Commands.Subscriptions.CreateSubscriptionGroupCommand CreateSubscriptionGroupCommand { get; }


        #endregion

        public override Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            var mode = parameters.GetNavigationMode();
            if (mode == NavigationMode.New)
            {
                SearchOption = new KeywordSearchPagePayloadContent()
                {
                    Keyword = System.Net.WebUtility.UrlDecode(parameters.GetValue<string>("keyword"))
                };
            }


            SelectedSearchTarget.Value = SearchTarget.Keyword;

            SelectedSearchSort.Value = VideoSearchOptionListItems.First(x => x.Sort == SearchOption.Sort && x.Order == SearchOption.Order);

            KeywordSearchBookmark = Database.BookmarkDb.Get(Database.BookmarkType.SearchWithKeyword, SearchOption.Keyword)
                ?? new Database.Bookmark()
                {
                    BookmarkType = Database.BookmarkType.SearchWithKeyword,
                    Label = SearchOption.Keyword,
                    Content = SearchOption.Keyword
                };
            RaisePropertyChanged(nameof(KeywordSearchBookmark));

            Database.SearchHistoryDb.Searched(SearchOption.Keyword, SearchOption.SearchTarget);


            return base.OnNavigatedToAsync(parameters);
        }

        #region Implement HohoemaVideListViewModelBase

        protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
            return new VideoSearchSource(SearchOption, SearchProvider, NgSettings);
		}

		protected override void PostResetList()
		{
            SearchOptionText = Services.Helpers.SortHelper.ToCulturizedText(SearchOption.Sort, SearchOption.Order);
        }
		

		protected override bool CheckNeedUpdateOnNavigateTo(NavigationMode mode)
		{
			if (ItemsView?.Source == null) { return true; }

            return base.CheckNeedUpdateOnNavigateTo(mode);
        }

        protected override bool TryGetHohoemaPin(out HohoemaPin pin)
        {
            pin = new HohoemaPin()
            {
                Label = SearchOption.Keyword,
                PageType = HohoemaPageType.SearchResultKeyword,
                Parameter = $"keyword={System.Net.WebUtility.UrlEncode(SearchOption.Keyword)}&target={SearchOption.SearchTarget}"
            };

            return true;
        }

        #endregion

    }
}
