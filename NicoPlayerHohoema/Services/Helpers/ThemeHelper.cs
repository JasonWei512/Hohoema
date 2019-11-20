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

        public static async Task ApplyAppThemeAsync(ElementTheme theme)    //App theme can be default (following Windows 10 system theme), light and dark
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

        public static ElementTheme GetActualAppTheme()    //Get the "actual" (light or dark) app theme
        {
            ElementTheme theme = LoadThemeFromSettings();

            if (theme == ElementTheme.Dark || theme == ElementTheme.Light)
            {
                return theme;
            }
            else
            {
                if (uiSettings.GetColorValue(UIColorType.Background).ToString() == "#FF000000")    //Check if Windows 10 is in dark theme
                {
                    return ElementTheme.Dark;    //If the app theme is set to "default" and Windows 10's is in dark theme, then the app will look "dark"
                }
                else 
                {
                    return ElementTheme.Light;    //If the app theme is set to "default" and Windows 10's is in light theme, then the app will look "light"
                }
            }
        }

        public static void InitializeTitleBarColor()
        {
            ApplyTitleBarColor();
            uiSettings.ColorValuesChanged += UISettings_ColorValuesChanged;    //When app theme is "default" and Windows 10's system theme is changed, change title bar color
        }

        private static async void UISettings_ColorValuesChanged(UISettings sender, object args)    //This event may be fired from a non-UI thread...
        {
            if (LoadThemeFromSettings() == ElementTheme.Default)
            {
                await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, ApplyTitleBarColor);    //...So use a dispathcer to do UI-related things
            }
        }

        public static void ApplyTitleBarColor()    //Change title bar color according to the "actual" app theme
        {
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;

            if (GetActualAppTheme() == ElementTheme.Light)
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
