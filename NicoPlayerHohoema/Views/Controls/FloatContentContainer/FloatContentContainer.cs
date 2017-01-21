﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace NicoPlayerHohoema.Views.Controls
{
    // TODO: FloatContentのサイズ設定への対応
    // Float状態時にFloatContentをContentのサイズを基準にスケーリングした大きさとして設定したい
    // （コンパイル時レベルの話、GridSplitterのような実行時の可変は対応未定）


    public enum FloatContentDisplayMode
    {
        Hidden,
        Fill,
        Float,
    }

    public delegate void FloatContentDisplayModeChangedEventHandler(object sender, bool isFillFloatContent);

    [TemplateVisualState(Name = FillDisplayModeState, GroupName = FloatContentDisplayModeStates)]
    [TemplateVisualState(Name = FloatDisplayModeState, GroupName = FloatContentDisplayModeStates)]
    public partial class FloatContentContainer : Control
    {
        private const string FloatContentDisplayModeStates = "FloatContentDisplayState";
        private const string HiddenDisplayModeState = "Hidden";
        private const string FillDisplayModeState = "Fill";
        private const string FloatDisplayModeState = "Float";


        public FloatContentContainer()
        {
            this.DefaultStyleKey = typeof(FloatContentContainer);

            Loaded += FloatContentContainer_Loaded;
        }

        private void FloatContentContainer_Loaded(object sender, RoutedEventArgs e)
        {
            SetContentDisplayMode();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        private static void OnIsFillFloatContentPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var source = (FloatContentContainer)sender;

            source.SetContentDisplayMode();
        }

        private static void OnFloatContentVisiblityPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var source = (FloatContentContainer)sender;

            source.SetContentDisplayMode();
        }

        private void SetContentDisplayMode()
        {
            var isFill = IsFillFloatContent;
            VisualStateManager.GoToState(this, isFill ? FillDisplayModeState : FloatDisplayModeState, true);

            OnDisplayModeChanged(isFill);
        }
    }
}
