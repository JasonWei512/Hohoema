using System;
using System.Diagnostics;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Services.Helpers
{
    public static class ThemeHelper
    {
        const string ThemeTypeKey = "Theme";

        static UISettings uiSettings = new UISettings();

        public static void SaveThemeToSettings(ElementTheme theme)
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(ThemeTypeKey))
            {
                ApplicationData.Current.LocalSettings.Values[ThemeTypeKey] = theme.ToString();
            }
            else
            {
                ApplicationData.Current.LocalSettings.Values.Add(ThemeTypeKey, theme.ToString());
            }
        }

        public static ElementTheme LoadThemeFromSettings()
        {
            try
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey(ThemeTypeKey))
                {
                    return (ElementTheme)Enum.Parse(typeof(ElementTheme), (string)ApplicationData.Current.LocalSettings.Values[ThemeTypeKey]);
                }
            }
            catch { }

            return ElementTheme.Default;
        }

        public static async Task ApplyAppThemeAsync(ElementTheme theme)
        {
            foreach (var view in CoreApplication.Views)
            {
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (Window.Current.Content is FrameworkElement frameworkElement)
                    {
                        frameworkElement.RequestedTheme = theme;
                    }
                });
            }
        }

        public static ElementTheme GetActualTheme()    
        {
            ElementTheme theme = LoadThemeFromSettings();

            if (theme == ElementTheme.Dark || theme == ElementTheme.Light)
            {
                return theme;
            }
            else
            {
                if (uiSettings.GetColorValue(UIColorType.Background).ToString() == "#FF000000")
                {
                    return ElementTheme.Dark;
                }
                else 
                {
                    return ElementTheme.Light;
                }
            }
        }

        public static void InitializeTitleBarColor()
        {
            ApplyTitleBarColor();
            uiSettings.ColorValuesChanged += UISettings_ColorValuesChanged;
        }

        private static void UISettings_ColorValuesChanged(UISettings sender, object args)
        {
            if (LoadThemeFromSettings() == ElementTheme.Default)
            {
                ApplyTitleBarColor();
            }
        }

        public static void ApplyTitleBarColor()
        {
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;

            if (GetActualTheme() == ElementTheme.Light)
            {
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Colors.DarkGray;
                titleBar.ButtonHoverForegroundColor = Colors.Black;
                titleBar.ButtonInactiveForegroundColor = Colors.Gray;
            }
            else
            {
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Colors.DimGray;
                titleBar.ButtonHoverForegroundColor = Colors.White;
                titleBar.ButtonInactiveForegroundColor = Colors.DarkGray;
            }
        }
    }
}
