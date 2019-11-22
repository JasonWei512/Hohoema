using System;
using System.Diagnostics;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Services
{
    /// <summary>A service that manages app theme.</summary>
    public class ThemeManagerService
    {
        private const string SettingsKey = "Theme";

        private UISettings uiSettings = new UISettings();



        #region Properties

        private ElementTheme requestedAppTheme = ElementTheme.Default;

        /// <summary>The requested app theme. Can be "light", "dark", or "default" (following Windows 10's system theme).</summary>
        public ElementTheme RequestedAppTheme
        {
            get => requestedAppTheme;
            set
            {
                if (requestedAppTheme != value)
                {
                    if(ActualAppTheme != GetActualTheme(value))
                    {
                        ActualAppThemeChanged(this, null);
                    }

                    requestedAppTheme = value;
                    SaveThemeToSettings(requestedAppTheme);
                }
            }
        }

        /// <summary>How the app actually looks like. Can be "light" or "dark".</summary>
        public ElementTheme ActualAppTheme
        {
            get => GetActualTheme(RequestedAppTheme);
        }

        #endregion



        #region Methods

        /// <summary>Initialize ThemeManagerService. Do this before using this service.</summary>
        public async Task InitializeAsync()
        {
            requestedAppTheme = LoadThemeFromSettings();
            await ApplyTitleBarColorForAllWindowsAsync();

            ActualAppThemeChanged += async (sender, args) => 
            {
                await ApplyTitleBarColorForAllWindowsAsync();     
            };
            uiSettings.ColorValuesChanged += WindowsSystemThemeChanged;
        }

        private async void WindowsSystemThemeChanged(UISettings sender, object args)    //This may be called from a non-UI thread, so here I use a diapatcher.
        {
            if (LoadThemeFromSettings() == ElementTheme.Default)
            {
                await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ActualAppThemeChanged(this, null);
                });
            }
        }

        /// <summary>Change the current window's title bar color according to the actual app theme.</summary>
        public void ApplyTitleBarColorForCurrentWindow()
        {
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;

            if (ActualAppTheme == ElementTheme.Light)
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

        /// <summary>Change all windows' title bar colors according to the actual app theme.</summary>
        private async Task ApplyTitleBarColorForAllWindowsAsync()
        {
            foreach (var view in CoreApplication.Views)
            {
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ApplyTitleBarColorForCurrentWindow();
                });
            }
        }

        /// <summary>Change all windows' content theme and title bar color accoring to the requested app theme.</summary>
        public async Task ApplyAppThemeAsync()
        {
            foreach (var view in CoreApplication.Views)
            {
                await view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (Window.Current.Content is FrameworkElement frameworkElement)
                    {
                        frameworkElement.RequestedTheme = RequestedAppTheme;
                    }

                    ApplyTitleBarColorForCurrentWindow();
                });
            }
        }
        
        private void SaveThemeToSettings(ElementTheme theme)
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(SettingsKey))
            {
                ApplicationData.Current.LocalSettings.Values[SettingsKey] = theme.ToString();
            }
            else
            {
                ApplicationData.Current.LocalSettings.Values.Add(SettingsKey, theme.ToString());
            }
        }

        private ElementTheme LoadThemeFromSettings()
        {
            try
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey(SettingsKey))
                {
                    return (ElementTheme)Enum.Parse(typeof(ElementTheme), (string)ApplicationData.Current.LocalSettings.Values[SettingsKey]);
                }
            }
            catch { }

            return ElementTheme.Default;
        }

        private ElementTheme GetActualTheme(ElementTheme givenTheme)    //Returns how givenTheme will look like based on current app theme
        {
            if (givenTheme == ElementTheme.Dark || givenTheme == ElementTheme.Light)
            {
                return givenTheme;
            }
            else
            {
                if (uiSettings.GetColorValue(UIColorType.Background).ToString() == "#FF000000")    //Check if Windows 10 is in dark theme
                {
                    return ElementTheme.Dark;    //If the app theme is set to "default" and Windows 10's is in dark theme, then givenTheme will look "dark"
                }
                else
                {
                    return ElementTheme.Light;    
                }
            }
        }

        #endregion



        #region Events

        /// <summary>Fired when the app's actual look (light or dark) changes.</summary>
        public event EventHandler ActualAppThemeChanged;

        #endregion
    }
}
