﻿using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Util;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Models
{
	/// <summary>
	/// ニコニコ動画の動画やサムネイル画像、
	/// 動画情報など動画に関わるメディアを管理します
	/// </summary>
	public class NiconicoMediaManager : BindableBase, IDisposable
	{
		const string CACHE_REQUESTED_FILENAME = "cache_requested.json";


		static internal async Task<NiconicoMediaManager> Create(HohoemaApp app)
		{
			var man = new NiconicoMediaManager(app);


			// キャッシュリクエストファイルのアクセサーを初期化
			var videoSaveFolder = await app.GetApplicationLocalDataFolder();
			man._CacheRequestedItemsFileAccessor = new FileAccessor<IList<NicoVideoCacheRequest>>(videoSaveFolder, CACHE_REQUESTED_FILENAME);

			// ダウンロードコンテキストを作成
			man.Context = await NicoVideoDownloadContext.Create(app, man);

			// 初期化をバックグラウンドタスクに登録
			var updater = new SimpleBackgroundUpdate("NicoMediaManager", () => man.Initialize());
			await app.BackgroundUpdater.Schedule(updater);

			return man;
		}



		private NiconicoMediaManager(HohoemaApp app)
		{
			_HohoemaApp = app;

			VideoIdToNicoVideo = new Dictionary<string, NicoVideo>();

			_Lock = new AsyncLock();

			_CacheRequestedItemsStack = new ObservableCollection<NicoVideoCacheRequest>();
			CacheRequestedItemsStack = new ReadOnlyObservableCollection<NicoVideoCacheRequest>(_CacheRequestedItemsStack);

		}


		public void Dispose()
		{
			Context.Dispose();
		}



		private async Task Initialize()
		{

			Debug.Write($"ダウンロードリクエストの復元を開始");


			// ダウンロードリクエストされたアイテムのNicoVideoオブジェクトの作成
			// 及び、リクエストの再構築
			var list = await LoadDownloadRequestItems();
			foreach (var req in list)
			{
				var nicoVideo = await GetNicoVideoAsync(req.RawVideoid);
				_CacheRequestedItemsStack.Add(req);
				await nicoVideo.CheckCacheStatus();
				Debug.Write(".");
			}

			Debug.WriteLine("");
			Debug.WriteLine($"{list.Count} 件のダウンロードリクエストを復元");


			// 前回削除を防止した動画を復旧させる
			var recentPreventDeleteVideoId = NicoVideoDownloadContext.ReadAndClearOncePreventDeleteVideoId();
			if (false == string .IsNullOrWhiteSpace(recentPreventDeleteVideoId))
			{
				await GetNicoVideoAsync(recentPreventDeleteVideoId);
			}
		}





		


		


		public async Task<NicoVideo> GetNicoVideoAsync(string rawVideoId, bool withInitialize = true)
		{
			NicoVideo nicoVideo = null;
			bool isFirstGet = false;
			using (var releaser = await _Lock.LockAsync())
			{
				if (false == VideoIdToNicoVideo.ContainsKey(rawVideoId))
				{
					nicoVideo = new NicoVideo(_HohoemaApp, rawVideoId, Context);
					VideoIdToNicoVideo.Add(rawVideoId, nicoVideo);
					isFirstGet = true;
				}
				else
				{
					nicoVideo = VideoIdToNicoVideo[rawVideoId];
				}
			}

			if (isFirstGet && withInitialize)
			{
				using (var releaser = await _Lock.LockAsync())
				{
					await nicoVideo.Initialize();
				}
			}

			return nicoVideo;
		}


		public async Task<List<NicoVideo>> GetNicoVideoItemsAsync(params string[] idList)
		{
			List<NicoVideo> videos = new List<NicoVideo>();

			using (var releaser = await _Lock.LockAsync())
			{
				foreach (var id in idList)
				{
					NicoVideo nicoVideo = null;
					if (false == VideoIdToNicoVideo.ContainsKey(id))
					{
						nicoVideo = new NicoVideo(_HohoemaApp, id, Context);
						VideoIdToNicoVideo.Add(id, nicoVideo);
					}
					else
					{
						nicoVideo = VideoIdToNicoVideo[id];
					}

					videos.Add(nicoVideo);
				}
			}

			foreach (var video in videos.AsParallel())
			{
				await video.Initialize().ConfigureAwait(false);
			}

			return videos;
		}


		public async Task RestoreCacheRequestFromCurrentVideoCacheFolder()
		{
			var dlFolder = await Context.GetVideoCacheFolder();
			if (dlFolder != null)
			{
				var files = await dlFolder.GetFilesAsync();

				var videoIdList = files.Where(x => x.FileType == ".mp4")
					.Select<StorageFile, Tuple<string, NicoVideoQuality>>(x =>
					{
						// ファイル名の最後方にある[]の中身の文字列を取得
						// (動画タイトルに[]が含まれる可能性に配慮)
						return new Tuple<string, NicoVideoQuality>(new String(x.Name
								.Reverse()
								.SkipWhile(y => y != ']')
								.TakeWhile(y => y != '[')
								.Reverse()
								.ToArray()
								),
							x.Name.EndsWith(".low.mp4") ? NicoVideoQuality.Low : NicoVideoQuality.Original
						);
					});

				foreach (var req in videoIdList)
				{
					var alreadyAdded = _CacheRequestedItemsStack.Any(x => x.RawVideoid == req.Item1 && x.Quality == req.Item2);
					if (!alreadyAdded)
					{
						var nicoVideo = await GetNicoVideoAsync(req.Item1);
						_CacheRequestedItemsStack.Add(new NicoVideoCacheRequest()
						{
							RawVideoid = req.Item1,
							Quality = req.Item2
						});
						await nicoVideo.CheckCacheStatus();
						Debug.Write(".");
					}
				}
			}
		}


		#region Download Queue management


		// TODO: キャッシュ対象の検索が低速にならないように対策

		public bool HasDownloadQueue
		{
			get
			{
				return CacheRequestedItemsStack.Count > 0;
			}
		}


		/// <summary>
		/// 次のキャッシュリクエストを取得します
		/// </summary>
		/// <returns></returns>
		internal async Task<NicoVideoCacheRequest> GetNextCacheRequest()
		{
			foreach (var req in _CacheRequestedItemsStack)
			{
				var nicoVideo = await GetNicoVideoAsync(req.RawVideoid);

				await nicoVideo.CheckCacheStatus();

				if (req.Quality == NicoVideoQuality.Original)
				{
					if (nicoVideo.OriginalQuality.IsCacheRequested
						&& nicoVideo.OriginalQuality.CanRequestDownload
						)
					{
						return req;
					}
				}
				else
				{
					if (nicoVideo.LowQuality.IsCacheRequested
						&& nicoVideo.LowQuality.CanRequestDownload
						)
					{
						return req;
					}
				}
			}

			return null;
		}


		/// <summary>
		/// キャッシュリクエストをキューの最後尾に積みます
		/// 通常のダウンロードリクエストではこちらを利用します
		/// </summary>
		/// <param name="req"></param>
		/// <returns></returns>
		internal async Task AddCacheRequest(string rawVideoId, NicoVideoQuality quality)
		{
			await RemoveCacheRequest(rawVideoId, quality);

			_CacheRequestedItemsStack.Add(new NicoVideoCacheRequest()
			{
				RawVideoid = rawVideoId,
				Quality = quality,
			});

			await SaveDownloadRequestItems();
		}


		public async Task<bool> RemoveCacheRequest(string rawVideoId, NicoVideoQuality quality)
		{
			var removeTarget = _CacheRequestedItemsStack.SingleOrDefault(x => x.RawVideoid == rawVideoId && x.Quality == quality);
			if (removeTarget != null)
			{
				_CacheRequestedItemsStack.Remove(removeTarget);
				await SaveDownloadRequestItems();

				return true;
			}
			else
			{
				return false;
			}
		}

		public bool CheckHasCacheRequest(string rawVideoId, NicoVideoQuality quality)
		{
			return _CacheRequestedItemsStack.Any(x => x.RawVideoid == rawVideoId && x.Quality == quality);
		}


		

		public async Task SaveDownloadRequestItems()
		{
			if (HasDownloadQueue)
			{
				await _CacheRequestedItemsFileAccessor.Save(_CacheRequestedItemsStack);

				Debug.WriteLine("ダウンロード待ち状況を保存しました。");
			}
			else
			{
				if (await _CacheRequestedItemsFileAccessor.Delete())
				{
					Debug.WriteLine("ダウンロード待ちがないため、状況ファイルを削除しました。");
				}
			}
		}

		public async Task<IList<NicoVideoCacheRequest>> LoadDownloadRequestItems()
		{
			if (await _CacheRequestedItemsFileAccessor.ExistFile())
			{
				return await _CacheRequestedItemsFileAccessor.Load();
			}
			else
			{
				return new List<NicoVideoCacheRequest>();
			}

		}

		#endregion


		


		public async Task OnCacheFolderChanged()
		{
			foreach (var nicoVideo in VideoIdToNicoVideo.Values)
			{
				await nicoVideo.LowQuality.SetupDownloadProgress();
				await nicoVideo.OriginalQuality.SetupDownloadProgress();
				await nicoVideo.CheckCacheStatus();
			}
		}


		private FileAccessor<IList<NicoVideoCacheRequest>> _CacheRequestedItemsFileAccessor;
		private ObservableCollection<NicoVideoCacheRequest> _CacheRequestedItemsStack;
		public ReadOnlyObservableCollection<NicoVideoCacheRequest> CacheRequestedItemsStack { get; private set; }

		private AsyncLock _Lock;
		private AsyncLock _NicoVideoUpdateLock = new AsyncLock();

		public Dictionary<string, NicoVideo> VideoIdToNicoVideo { get; private set; }

		public NicoVideoDownloadContext Context { get; private set; }
		HohoemaApp _HohoemaApp;
	}








}