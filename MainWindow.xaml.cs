using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Xaml.Interactions.Media;

using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT; // required to support Window.As<ICompositionSupportsSystemBackdrop>()

namespace BehaviorAnimations;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    Microsoft.UI.Windowing.AppWindow? appWindow;
    SystemBackdropConfiguration? _configurationSource;
    DesktopAcrylicController? _acrylicController;
    public ICommand KeyDownCommand { get; }

    public MainWindow()
    {
        this.InitializeComponent();
        this.ExtendsContentIntoTitleBar = true;
        this.Title = $"{App.GetCurrentNamespace()}";
        this.Activated += MainWindow_Activated;
        this.Closed += MainWindow_Closed;
        SetTitleBar(CustomTitleBar);

        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this); // Retrieve the window handle (HWND) of the current (XAML) WinUI3 window.
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd); // Retrieve the WindowId that corresponds to hWnd.
        appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId); // Lastly, retrieve the AppWindow for the current (XAML) WinUI3 window.
        if (appWindow is not null)
        {
            if (App.IsPackaged)
                appWindow?.SetIcon(System.IO.Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, $"Assets/StoreLogo.ico"));
            else
                appWindow?.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, $"Assets/StoreLogo.ico"));
        }

        // https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/system-backdrop-controller
        if (DesktopAcrylicController.IsSupported())
        {
            // Hook up the policy object.
            _configurationSource = new SystemBackdropConfiguration();
            // Create the desktop controller.
            _acrylicController = new Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController();
            _acrylicController.TintOpacity = 0.3f; // Lower values may be too translucent vs light background.
            _acrylicController.LuminosityOpacity = 0.1f;
            _acrylicController.TintColor = Microsoft.UI.Colors.Gray;
            // Fall-back color is only used when the window state becomes deactivated.
            _acrylicController.FallbackColor = Microsoft.UI.Colors.Transparent;
            // Note: Be sure to have "using WinRT;" to support the Window.As<T>() call.
            _acrylicController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            _acrylicController.SetSystemBackdropConfiguration(_configurationSource);
        }
        else
        {
            //if (App.Current.Resources.TryGetValue("ApplicationPageBackgroundThemeBrush", out object _))
            //    root.Background = (Microsoft.UI.Xaml.Media.SolidColorBrush)App.Current.Resources["ApplicationPageBackgroundThemeBrush"];
            //else
            //    root.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 20));

            CreateGradientBackdrop(root);
        }

        KeyDownCommand = new RelayCommand<object>((obj) => 
        {
            if (obj is Microsoft.UI.Xaml.Controls.TextBox tb)
            {
                ShowMessage($"Got TextBox at {DateTime.Now.ToString("hh:mm:ss.fff tt")}", InfoBarSeverity.Informational);
            }
            else
            {
                ShowMessage($"Got {obj?.GetType().Name} at {DateTime.Now.ToString("hh:mm:ss.fff tt")}", InfoBarSeverity.Warning);
            }
        });
    }

    void CreateGradientBackdrop(FrameworkElement fe)
    {
        // Get the FrameworkElement's compositor.
        var compositor = ElementCompositionPreview.GetElementVisual(fe).Compositor;
        if (compositor == null) { return; }
        var gb = compositor.CreateLinearGradientBrush();
        
        // Define gradient stops.
        var gradientStops = gb.ColorStops;

        // If we found our App.xaml brushes then use them.
        if (App.Current.Resources.TryGetValue("GC1", out object clr1) &&
            App.Current.Resources.TryGetValue("GC2", out object clr2) &&
            App.Current.Resources.TryGetValue("GC3", out object clr3) &&
            App.Current.Resources.TryGetValue("GC4", out object clr4))
        {
            //var clr1 = (Windows.UI.Color)App.Current.Resources["GC1"];
            //var clr2 = (Windows.UI.Color)App.Current.Resources["GC2"];
            //var clr3 = (Windows.UI.Color)App.Current.Resources["GC3"];
            //var clr4 = (Windows.UI.Color)App.Current.Resources["GC4"];
            gradientStops.Insert(0, compositor.CreateColorGradientStop(0.0f, (Windows.UI.Color)clr1));
            gradientStops.Insert(1, compositor.CreateColorGradientStop(0.3f, (Windows.UI.Color)clr2));
            gradientStops.Insert(2, compositor.CreateColorGradientStop(0.6f, (Windows.UI.Color)clr3));
            gradientStops.Insert(3, compositor.CreateColorGradientStop(1.0f, (Windows.UI.Color)clr4));
        }
        else
        {
            gradientStops.Insert(0, compositor.CreateColorGradientStop(0.0f, Windows.UI.Color.FromArgb(55, 255, 0, 0)));   // Red
            gradientStops.Insert(1, compositor.CreateColorGradientStop(0.3f, Windows.UI.Color.FromArgb(55, 255, 216, 0))); // Yellow
            gradientStops.Insert(2, compositor.CreateColorGradientStop(0.6f, Windows.UI.Color.FromArgb(55, 0, 255, 0)));   // Green
            gradientStops.Insert(3, compositor.CreateColorGradientStop(1.0f, Windows.UI.Color.FromArgb(55, 0, 0, 255)));   // Blue
        }
    
        // Set the direction of the gradient.
        gb.StartPoint = new System.Numerics.Vector2(0, 0);
        gb.EndPoint = new System.Numerics.Vector2(1, 1);
    
        // Create a sprite visual and assign the gradient brush.
        var spriteVisual = Compositor.CreateSpriteVisual();
        spriteVisual.Brush = gb;
    
        // Set the size of the sprite visual to cover the entire window.
        spriteVisual.Size = new System.Numerics.Vector2((float)fe.ActualSize.X, (float)fe.ActualSize.Y);

        // Handle the SizeChanged event to adjust the size of the sprite visual when the window is resized.
        fe.SizeChanged += (s, e) =>
        {
            spriteVisual.Size = new System.Numerics.Vector2((float)fe.ActualWidth, (float)fe.ActualHeight);
        };
    
        // Set the sprite visual as the background of the FrameworkElement.
        ElementCompositionPreview.SetElementChildVisual(fe, spriteVisual);
    }

    void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        // Make sure the Acrylic controller is disposed so it doesn't try to access a closed window.
        if (_acrylicController is not null)
        {
            _acrylicController.Dispose();
            _acrylicController = null;
        }
    }

    void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            ShowMessage($"WindowActivationState is {args.WindowActivationState}", InfoBarSeverity.Informational);
        }
    }

    public void SomePublicMethodHere()
    {
        ShowMessage($"Executing public method at {DateTime.Now.ToString("hh:mm:ss.fff tt")}", InfoBarSeverity.Informational);
    }

    /// <summary>
    /// Thread-safe helper for <see cref="Microsoft.UI.Xaml.Controls.InfoBar"/>.
    /// </summary>
    /// <param name="message">text to show</param>
    /// <param name="severity"><see cref="Microsoft.UI.Xaml.Controls.InfoBarSeverity"/></param>
    public void ShowMessage(string message, InfoBarSeverity severity)
    {
        infoBar.DispatcherQueue?.TryEnqueue(() =>
        {
            infoBar.IsOpen = true;
            infoBar.Severity = severity;
            infoBar.Message = $"{message}";
        });
    }

 

}
