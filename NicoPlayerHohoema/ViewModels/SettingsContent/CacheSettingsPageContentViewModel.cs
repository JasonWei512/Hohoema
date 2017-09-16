﻿using Reactive.Bindings.Extensions;
using Prism.Commands;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using Reactive.Bindings;
using Prism.Mvvm;
using NicoPlayerHohoema.Views.Service;
using NicoPlayerHohoema.Models;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace NicoPlayerHohoema.ViewModels
{
	public class CacheSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		// Note: ログインしていない場合は利用できない

		HohoemaApp _HohoemaApp;
		CacheSettings _CacheSettings;
		EditAutoCacheConditionDialogService _EditDialogService;
		AcceptCacheUsaseDialogService _AcceptCacheUsaseDialogService;

		public CacheSettingsPageContentViewModel(
			HohoemaApp hohoemaApp
			, EditAutoCacheConditionDialogService editDialogService
			
			)
			: base("キャッシュ", HohoemaSettingsKind.Cache)
		{
			_HohoemaApp = hohoemaApp;
			_CacheSettings = _HohoemaApp.UserSettings.CacheSettings;
			_EditDialogService = editDialogService;
			_AcceptCacheUsaseDialogService = cacheConfirmDialogService;

			

			
		}

        protected override async void OnEnter(ICollection<IDisposable> focusingDisposable)
        {
			await RefreshCacheSaveFolderStatus();
		}

        protected override void OnLeave()
        {
		}


		
		private async Task EditAutoCacheCondition(AutoCacheConditionViewModel conditionVM)
		{
			var serializedText = Newtonsoft.Json.JsonConvert.SerializeObject(conditionVM.TagCondition);

			if (!await _EditDialogService.ShowDialog(conditionVM))
			{
				// 編集前の状態に復帰
				try
				{
					var previousState = Newtonsoft.Json.JsonConvert.DeserializeObject<TagCondition>(serializedText);

					conditionVM.TagCondition.Label = previousState.Label;
					conditionVM.TagCondition.IncludeTags.Clear();
					foreach (var tag in previousState.IncludeTags)
					{
						conditionVM.TagCondition.IncludeTags.Add(tag);
					}
					conditionVM.TagCondition.ExcludeTags.Clear();
					foreach (var tag in previousState.ExcludeTags)
					{
						conditionVM.TagCondition.ExcludeTags.Add(tag);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.ToString());
				}
			}
			else
			{
//				await _CacheSettings.Save();
			}
		}

		

	}


	public class AutoCacheConditionViewModel : BindableBase
	{
		public AutoCacheConditionViewModel(CacheSettings cacheSettings, TagCondition tagCondition)
		{
			TagCondition = tagCondition;

			Label = tagCondition.ToReactivePropertyAsSynchronized(x => x.Label);

			IncludeTags = tagCondition.IncludeTags;
			ExcludeTags = tagCondition.ExcludeTags;

			IncludeTagText = new ReactiveProperty<string>("");
			ExcludeTagText = new ReactiveProperty<string>("");

			AddIncludeTagCommand = IncludeTagText.Select(x => x.Length > 0)
				.ToReactiveCommand();
			AddIncludeTagCommand.Subscribe(x =>
			{
				if (AddIncludeTag(IncludeTagText.Value))
				{
					IncludeTagText.Value = "";
				}
			});

			AddExcludeTagCommand = ExcludeTagText.Select(x => x.Length > 0)
				.ToReactiveCommand();
			AddExcludeTagCommand.Subscribe(x =>
			{
				if (AddExcludeTag(ExcludeTagText.Value))
				{
					ExcludeTagText.Value = "";
				}
			});

			RemoveIncludeTagCommand = new DelegateCommand<string>(x => RemoveIncludeTag(x));
			RemoveExcludeTagCommand = new DelegateCommand<string>(x => RemoveExcludeTag(x));
		}


		private bool AddIncludeTag(string tag)
		{
			if (IncludeTags.Contains(tag))
			{
				return false;
			}

			IncludeTags.Add(tag);

			return true;
		}

		private bool RemoveIncludeTag(string tag)
		{
			if (!IncludeTags.Contains(tag))
			{
				return false;
			}

			return IncludeTags.Remove(tag);
		}




		private bool AddExcludeTag(string tag)
		{
			if (ExcludeTags.Contains(tag))
			{
				return false;
			}

			ExcludeTags.Add(tag);

			return true;
		}

		private bool RemoveExcludeTag(string tag)
		{
			if (!ExcludeTags.Contains(tag))
			{
				return false;
			}

			return ExcludeTags.Remove(tag);
		}

		public ReactiveProperty<string> IncludeTagText { get; private set; }
		public ReactiveProperty<string> ExcludeTagText { get; private set; }

		public ReactiveCommand AddIncludeTagCommand { get; private set; }
		public ReactiveCommand AddExcludeTagCommand { get; private set; }
		public DelegateCommand<string> RemoveIncludeTagCommand { get; private set; }
		public DelegateCommand<string> RemoveExcludeTagCommand { get; private set; }

		public ReactiveProperty<string> Label { get; private set; }
		public ObservableCollection<string> IncludeTags { get; private set; }
		public ObservableCollection<string> ExcludeTags { get; private set; }


		public TagCondition TagCondition { get; private set; }
	}
}
