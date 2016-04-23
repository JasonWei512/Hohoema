﻿using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.ViewModels
{
	public class MenuNavigatePageBaseViewModel : BindableBase
	{
		public PageManager PageManager { get; private set; }

		public List<MenuListItemViewModel> TopMenuItems { get; private set; }
		public List<MenuListItemViewModel> BottomMenuItems { get; private set; }

		public ReactiveProperty<bool> IsPaneOpen { get; private set; }
		
		public MenuNavigatePageBaseViewModel(PageManager pageManager)
		{
			PageManager = pageManager;

			IsPaneOpen = new ReactiveProperty<bool>(false);

			TopMenuItems = new List<MenuListItemViewModel>()
			{
				new MenuListItemViewModel(PageManager, this)
				{
					Title = "ランキング",
					PageType = HohoemaPageType.Ranking,
				}
				, new MenuListItemViewModel(PageManager, this)
				{
					Title = "購読",
					PageType = HohoemaPageType.Subscription,
				}
				, new MenuListItemViewModel(PageManager, this)
				{
					Title = "履歴",
					PageType = HohoemaPageType.History,
				}
				, new MenuListItemViewModel(PageManager, this)
				{
					Title = "検索",
					PageType = HohoemaPageType.Search,
				}
			};


			BottomMenuItems = new List<MenuListItemViewModel>()
			{
				new MenuListItemViewModel(PageManager, this)
				{
					Title = "設定",
					PageType = HohoemaPageType.Settings,
				}
				, new MenuListItemViewModel(PageManager, this)
				{
					Title = "アカウント",
					PageType = HohoemaPageType.Settings,
					PageParameter = "Account"
				}
			};

			ClosePaneCommand = new DelegateCommand(ClosePane);
		}


		public DelegateCommand ClosePaneCommand { get; private set; } 

		public void ClosePane()
		{
			IsPaneOpen.Value = false;
		}

	}

	public class MenuListItemViewModel : BindableBase
	{
		public MenuNavigatePageBaseViewModel ParentVM { get; private set; }
		public PageManager PageManager { get; private set; }

		public MenuListItemViewModel(PageManager pageManager, MenuNavigatePageBaseViewModel parentVM)
		{
			PageManager = pageManager;
			ParentVM = parentVM;

		}

		public string Title { get; set; }
		public HohoemaPageType PageType { get; set; }
		public string PageParameter { get; set; }

		private DelegateCommand<Visibility?> _SelectMenuItemCommand;
		public DelegateCommand<Visibility?> SelectMenuItemCommand
		{
			get
			{
				return _SelectMenuItemCommand
					?? (_SelectMenuItemCommand = new DelegateCommand<Visibility?>((paneToggleButtonVisiblity) =>
					{
						// ペインの切り替えボタンが使える場合は、ペインを閉じる
						if (paneToggleButtonVisiblity == Visibility.Visible)
						{
							ParentVM.ClosePane();
						}

						PageManager.OpenPage(PageType, PageParameter);
					}));
			}
		}
	}
}
