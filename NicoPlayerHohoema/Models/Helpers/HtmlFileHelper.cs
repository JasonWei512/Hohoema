﻿using NicoPlayerHohoema.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Prism.Ioc;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Models.Helpers
{
	public static class HtmlFileHelper
	{
		

		public static async Task<Uri> PartHtmlOutputToCompletlyHtml(string id, string html)
		{
			// Note: WebViewに渡すHTMLファイルをテンポラリフォルダを経由してアクセスします。
			// WebView.Sourceの仕様上、テンポラリフォルダにサブフォルダを作成し、そのサブフォルダにコンテンツを配置しなければなりません。

			const string VideDescHTMLFolderName = "html";
			// ファイルとして動画説明HTMLを書き出す
			var outputFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(VideDescHTMLFolderName, CreationCollisionOption.OpenIfExists);




			string descJoinedHtmlText = "";

			// ファイルのテンプレートになるHTMLテキストを取得して
			var templateHtmlFileStorage = await StorageFile.GetFileFromApplicationUriAsync(
				new Uri("ms-appx:///Assets/html/template.html")
				);


			// テンプレートHTMLに動画説明を埋め込んだテキストを作成
			using (var stream = await templateHtmlFileStorage.OpenAsync(FileAccessMode.Read))
			using (var textReader = new StreamReader(stream.AsStream()))
			{
                var themeManagerService = Prism.PrismApplicationBase.Current.Container.Resolve<ThemeManagerService>();

                var templateText = textReader.ReadToEnd();
                descJoinedHtmlText = templateText
                    .Replace("{content}", html)
                    .Replace("http://", "https://")
                    .Replace("{foreground-color}", themeManagerService.ActualAppTheme == ElementTheme.Dark ? "#EFEFEF" : "000000");
			}


			// テンポラリストレージ空間に動画説明HTMLファイルを書き込み
			var filename = id + ".html";
			var savedVideoDescHtmlFile = await outputFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
			using (var stream = await savedVideoDescHtmlFile.OpenStreamForWriteAsync())
			using (var writer = new StreamWriter(stream))
			{
				writer.Write(descJoinedHtmlText);
			}

			var folderName = Path.GetFileName(outputFolder.Path);

			// 
			return new Uri($"ms-appdata:///temp/{VideDescHTMLFolderName}/{filename}");
		}

        public static async Task<Uri> RegenerateHtml(string id)    
        {
            const string VideDescHTMLFolderName = "html";
            const string darkStyle = @"
        body {
            line-height: 1.5;
            font-size: 14px;
            letter-spacing: 1px;
            color: #EFEFEF;
        }";
            const string lightStyle = @"
        body {
            line-height: 1.5;
            font-size: 14px;
            letter-spacing: 1px;
            color: 000000;
        }";

            try    //Open the exsisting html file, and replace the foreground color part according to app's actual theme
            {
                var outputFolder = await ApplicationData.Current.TemporaryFolder.GetFolderAsync(VideDescHTMLFolderName);
                var filename = id + ".html";
                var savedVideoDescHtmlFile = await outputFolder.GetFileAsync(filename);
                string regeneratedHtmlContent = "";

                using (var stream = await savedVideoDescHtmlFile.OpenAsync(FileAccessMode.Read))
                using (var textReader = new StreamReader(stream.AsStream()))
                {
                    var themeManagerService = Prism.PrismApplicationBase.Current.Container.Resolve<ThemeManagerService>();
                    var savedText = textReader.ReadToEnd();

                    if (themeManagerService.ActualAppTheme == ElementTheme.Light)
                    {
                        regeneratedHtmlContent = savedText.Replace(darkStyle, lightStyle);
                    }
                    else
                    {
                        regeneratedHtmlContent = savedText.Replace(lightStyle, darkStyle);
                    }
                }

                savedVideoDescHtmlFile = await outputFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                using (var stream = await savedVideoDescHtmlFile.OpenStreamForWriteAsync())
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(regeneratedHtmlContent);
                }

                return new Uri($"ms-appdata:///temp/{VideDescHTMLFolderName}/{filename}");
            }
            catch
            {
                return null;    //Returns null if html file doesn't exist
            }
        }
	}
}
