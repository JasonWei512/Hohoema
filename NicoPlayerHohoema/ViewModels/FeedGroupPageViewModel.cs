﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using System.Collections.ObjectModel;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;

namespace NicoPlayerHohoema.ViewModels
{
	public class FeedGroupPageViewModel : HohoemaViewModelBase
	{
		public FeedGroup FeedGroup { get; private set; }

		public ReactiveProperty<bool> IsDeleted { get; private set; }

		public ObservableCollection<IFeedSource> MylistFeedSources { get; private set; }
		public ObservableCollection<IFeedSource> TagFeedSources { get; private set; }
		public ObservableCollection<IFeedSource> UserFeedSources { get; private set; }



		public ReactiveProperty<bool> SelectFromFavItems { get; private set; }


		public ReactiveProperty<FavInfo> SelectedFavInfo { get; private set; }

		public ObservableCollection<FavInfo> MylistFavItems { get; private set; }
		public ObservableCollection<FavInfo> TagFavItems { get; private set; }
		public ObservableCollection<FavInfo> UserFavItems { get; private set; }

		public ReactiveProperty<FavoriteItemType> FavItemType { get; private set; }
		public ReactiveProperty<string> FeedSourceId { get; private set; }
		public ReactiveProperty<bool> ExistFeedSource { get; private set; }
		public ReactiveProperty<bool> IsPublicFeedSource { get; private set; }
		
		private string _FeedSourceName;

		public FeedGroupPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager) 
			: base(hohoemaApp, pageManager, isRequireSignIn:true )
		{
			IsDeleted = new ReactiveProperty<bool>();

			MylistFeedSources = new ObservableCollection<IFeedSource>();
			TagFeedSources = new ObservableCollection<IFeedSource>();
			UserFeedSources = new ObservableCollection<IFeedSource>();

			MylistFavItems = new ObservableCollection<FavInfo>();
			TagFavItems = new ObservableCollection<FavInfo>();
			UserFavItems = new ObservableCollection<FavInfo>();

			SelectFromFavItems = new ReactiveProperty<bool>(true);
			SelectedFavInfo = new ReactiveProperty<FavInfo>();

			FavItemType = new ReactiveProperty<FavoriteItemType>();
			FeedSourceId = new ReactiveProperty<string>();
			ExistFeedSource = new ReactiveProperty<bool>();
			IsPublicFeedSource = new ReactiveProperty<bool>();



			FavItemType.Subscribe(x => 
			{
				FeedSourceId.Value = "";
				ExistFeedSource.Value = false;

				_FeedSourceName = "";
			});

			Observable.Merge(
				SelectFromFavItems.ToUnit(),
				
				SelectedFavInfo.ToUnit(),

				FavItemType.ToUnit(),
				FeedSourceId.ToUnit().Throttle(TimeSpan.FromSeconds(1))				
				)
				.Subscribe(async x => 
				{
					if (SelectFromFavItems.Value)
					{
						ExistFeedSource.Value = SelectedFavInfo.Value != null;
						IsPublicFeedSource.Value = true;
						return;
					}

					ExistFeedSource.Value = false;

					if (FavItemType.Value == FavoriteItemType.Tag)
					{
						ExistFeedSource.Value = !string.IsNullOrWhiteSpace(FeedSourceId.Value);
						IsPublicFeedSource.Value = true;
						_FeedSourceName = FeedSourceId.Value;
					}
					else
					{
						if (string.IsNullOrWhiteSpace(FeedSourceId.Value))
						{
							ExistFeedSource.Value = false;
						}
						else
						{
							if (FavItemType.Value == FavoriteItemType.Mylist)
							{
								try
								{
									var mylistRes = await HohoemaApp.ContentFinder.GetMylist(FeedSourceId.Value);
									var mylist = mylistRes?.Mylistgroup.ElementAtOrDefault(0);
									
									if (mylist != null)
									{
										ExistFeedSource.Value = true;
										IsPublicFeedSource.Value = mylist.IsPublic;
										_FeedSourceName = mylist.Name;
									}
								}
								catch
								{
									ExistFeedSource.Value = false;
								}

							}
							else if (FavItemType.Value == FavoriteItemType.User)
							{
								try
								{
									var user = await HohoemaApp.ContentFinder.GetUserDetail(FeedSourceId.Value);
									if (user != null)
									{
										ExistFeedSource.Value = true;
										IsPublicFeedSource.Value = !user.IsOwnerVideoPrivate;
										_FeedSourceName = user.Nickname;
									}
								}
								catch
								{
									ExistFeedSource.Value = false;
								}
							}

							if (!ExistFeedSource.Value)
							{
								IsPublicFeedSource.Value = false;
								_FeedSourceName = "";
							}
						}
					}
				});
			AddFeedCommand = 
				Observable.CombineLatest(
					ExistFeedSource,
					IsPublicFeedSource
					)
				.Select(x => x.All(y => y == true))
				.ToReactiveCommand();

			AddFeedCommand.Subscribe(_ =>
			{
				string name = "";
				string id = "";

				if (SelectFromFavItems.Value)
				{
					var favInfo = SelectedFavInfo.Value;
					name = favInfo.Name;
					id = favInfo.Id;

					if (favInfo.FavoriteItemType != FavItemType.Value)
					{
						throw new Exception();
					}
				}
				else
				{
					// idからMylistGroupを引く
					// 公開されていない場合にはエラー
					name = _FeedSourceName;
					id = FeedSourceId.Value;
				}

				var favManager = HohoemaApp.FavManager;
				var feedManager = HohoemaApp.FeedManager;
				IFeedSource feedSource;
				switch (FavItemType.Value)
				{
					case FavoriteItemType.Tag:

						feedSource = FeedGroup.AddTagFeedSource(id);
						if (feedSource != null)
						{
							var favInfo = favManager.Tag.FavInfoItems.SingleOrDefault(x => x.Id == id);
							if (favInfo != null)
							{
								TagFavItems.Remove(favInfo);
							}

							TagFeedSources.Add(feedSource);
						}

						break;
					case FavoriteItemType.Mylist:

						feedSource = FeedGroup.AddMylistFeedSource(name, id);
						if (feedSource != null)
						{
							var favInfo = favManager.Mylist.FavInfoItems.SingleOrDefault(x => x.Id == id);
							if (favInfo != null)
							{
								MylistFavItems.Remove(favInfo);
							}

							MylistFeedSources.Add(feedSource);
						}

						break;
					case FavoriteItemType.User:

						feedSource = FeedGroup.AddUserFeedSource(name, id);
						if (feedSource != null)
						{
							var favInfo = favManager.User.FavInfoItems.SingleOrDefault(x => x.Id == id);
							if (favInfo != null)
							{
								UserFavItems.Remove(favInfo);
							}

							UserFeedSources.Add(feedSource);
						}

						break;
					default:
						break;
				}

				HohoemaApp.FeedManager.SaveOne(FeedGroup);

			});
		}




		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			FeedGroup = null;

			if (e.Parameter is string)
			{
				var label = e.Parameter as string;

				FeedGroup = HohoemaApp.FeedManager.GetFeedGroup(label);
			}

			IsDeleted.Value = FeedGroup == null;

			if (FeedGroup != null)
			{
				UpdateTitle(FeedGroup.Label);

				MylistFeedSources.Clear();
				foreach (var mylistFeedSrouce in FeedGroup.FeedSourceList.Where(x => x.FavoriteItemType == FavoriteItemType.Mylist))
				{
					MylistFeedSources.Add(mylistFeedSrouce);
				}

				MylistFavItems.Clear();
				foreach (var mylistFavInfo in HohoemaApp.FavManager.Mylist.FavInfoItems.Where(x => MylistFeedSources.All(y => x.Id != y.Id)))
				{
					MylistFavItems.Add(mylistFavInfo);
				}

				TagFeedSources.Clear();
				foreach (var tagFeedSrouce in FeedGroup.FeedSourceList.Where(x => x.FavoriteItemType == FavoriteItemType.Tag))
				{
					TagFeedSources.Add(tagFeedSrouce);
				}

				TagFavItems.Clear();
				foreach (var tagFavInfo in HohoemaApp.FavManager.Tag.FavInfoItems.Where(x => TagFeedSources.All(y => x.Id != y.Id)))
				{
					TagFavItems.Add(tagFavInfo);
				}

				UserFeedSources.Clear();
				foreach (var userFeedSrouce in FeedGroup.FeedSourceList.Where(x => x.FavoriteItemType == FavoriteItemType.User))
				{
					UserFeedSources.Add(userFeedSrouce);
				}

				UserFavItems.Clear();
				foreach (var userFavInfo in HohoemaApp.FavManager.User.FavInfoItems.Where(x => UserFeedSources.All(y => x.Id != y.Id)))
				{
					UserFavItems.Add(userFavInfo);
				}
			}







			base.OnNavigatedTo(e, viewModelState);
		}

		private DelegateCommand _RemoveFeedGroupCommand;
		public DelegateCommand RemoveFeedGroupCommand
		{
			get
			{
				return _RemoveFeedGroupCommand
					?? (_RemoveFeedGroupCommand = new DelegateCommand(async () =>
					{
						if (await HohoemaApp.FeedManager.RemoveFeedGroup(FeedGroup))
						{
							if (PageManager.NavigationService.CanGoBack())
							{
								PageManager.NavigationService.GoBack();
							}
							else
							{
								PageManager.OpenPage(HohoemaPageType.FeedGroupManage);
							}
						}
					}));
			}
		}

		public ReactiveCommand AddFeedCommand { get; private set; }
		

		private DelegateCommand _ExistFeedSourceCommand;
		public DelegateCommand ExistFeedSourceCommand
		{
			get
			{
				return _ExistFeedSourceCommand
					?? (_ExistFeedSourceCommand = new DelegateCommand(() =>
					{

					}));
			}
		}

		

		private DelegateCommand<IFeedSource> _RemoveFeedSourceCommand;
		public DelegateCommand<IFeedSource> RemoveFeedSourceCommand
		{
			get
			{
				return _RemoveFeedSourceCommand
					?? (_RemoveFeedSourceCommand = new DelegateCommand<IFeedSource>((feedSource) =>
					{
						FeedGroup.RemoveUserFeedSource(feedSource);

						switch (feedSource.FavoriteItemType)
						{
							case FavoriteItemType.Tag:
								if (TagFeedSources.Remove(feedSource))
								{
									var favInfo = HohoemaApp.FavManager.Tag.FavInfoItems.SingleOrDefault(x => x.Id == feedSource.Id);
									if (favInfo != null)
									{
										TagFavItems.Add(favInfo);
									}
								}
								break;
							case FavoriteItemType.Mylist:
								if (MylistFeedSources.Remove(feedSource))
								{
									var favInfo = HohoemaApp.FavManager.Mylist.FavInfoItems.SingleOrDefault(x => x.Id == feedSource.Id);
									if (favInfo != null)
									{
										MylistFavItems.Add(favInfo);
									}
								}
								break;
							case FavoriteItemType.User:
								if (UserFeedSources.Remove(feedSource))
								{
									var favInfo = HohoemaApp.FavManager.User.FavInfoItems.SingleOrDefault(x => x.Id == feedSource.Id);
									if (favInfo != null)
									{
										UserFavItems.Add(favInfo);
									}
								}
								break;
							default:
								break;
						}

					}));
			}
		}
	}

	public class FeedItemSourceListItem
	{
		public FeedItemSourceListItem()
		{

		}


	}
}
