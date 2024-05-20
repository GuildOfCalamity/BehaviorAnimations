using System;
using System.Diagnostics;
using System.Numerics;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Xaml.Interactivity;

using Windows.Foundation;

namespace BehaviorAnimations.Behaviors;


/// <summary>
/// <see cref="FrameworkElement"/> <see cref="Microsoft.Xaml.Interactivity.Behavior"/>.
/// </summary>
public class OpacityAnimationBehavior : Behavior<FrameworkElement>
{
    #region [Props]
    /// <summary>
    /// Identifies the <see cref="Seconds"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty SecondsProperty = DependencyProperty.Register(
        nameof(Seconds),
        typeof(double),
        typeof(SlideAnimationBehavior),
        new PropertyMetadata(1.25d));

    /// <summary>
    /// Gets or sets the <see cref="TimeSpan"/> to run the animation for.
    /// </summary>
    public double Seconds
    {
        get => (double)GetValue(SecondsProperty);
        set => SetValue(SecondsProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="From"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty FromProperty = DependencyProperty.Register(
        nameof(From),
        typeof(double),
        typeof(SlideAnimationBehavior),
        new PropertyMetadata(0d));

    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public double From
    {
        get => (double)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="To"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty ToProperty = DependencyProperty.Register(
        nameof(To),
        typeof(double),
        typeof(SlideAnimationBehavior),
        new PropertyMetadata(1d));

    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public double To
    {
        get => (double)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="EaseMode"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty EaseModeProperty = DependencyProperty.Register(
        nameof(EaseMode),
        typeof(string),
        typeof(SlideAnimationBehavior),
        new PropertyMetadata("Linear"));

    /// <summary>
    /// Gets or sets the easing type for the compositor.
    /// </summary>
    public string EaseMode
    {
        get => (string)GetValue(EaseModeProperty);
        set => SetValue(EaseModeProperty, value);
    }
    #endregion

    protected override void OnAttached()
    {
        base.OnAttached();

        if (!App.AnimationsEffectsEnabled)
            return;

        AssociatedObject.Loaded += AssociatedObject_Loaded;
        AssociatedObject.Unloaded += AssociatedObject_Unloaded;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (!App.AnimationsEffectsEnabled)
            return;

        AssociatedObject.Loaded -= AssociatedObject_Loaded;
        AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
    }

    void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] {sender.GetType().Name} loaded at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");

        var obj = sender as FrameworkElement;
        if (obj is null) { return; }

        if (obj.ActualHeight != double.NaN && obj.ActualHeight != 0)
            Debug.WriteLine($"[INFO] Reported {sender.GetType().Name} height is {obj.ActualHeight} pixels");

        Debug.WriteLine($"[INFO] Opacity animation will run for {Seconds} seconds.");
        AnimateUIElementOpacity(From, To, TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode);
    }

    /// <summary>
    /// Mock disposal routine.
    /// </summary>
    void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] {sender.GetType().Name} unloaded at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
    }

    #region [Composition Animations]
    /// <summary>
    /// Opacity animation using <see cref="Microsoft.UI.Composition.ScalarKeyFrameAnimation"/>
    /// </summary>
    void AnimateUIElementOpacity(double from, double to, TimeSpan duration, UIElement target, string ease, Microsoft.UI.Composition.AnimationDirection direction = Microsoft.UI.Composition.AnimationDirection.Normal)
    {
        Microsoft.UI.Composition.CompositionEasingFunction easer;
        var targetVisual = ElementCompositionPreview.GetElementVisual(target);
        if (targetVisual is null) { return; }
        var compositor = targetVisual.Compositor;
        var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
        opacityAnimation.StopBehavior = Microsoft.UI.Composition.AnimationStopBehavior.SetToFinalValue;
        opacityAnimation.Direction = direction;
        opacityAnimation.Duration = duration;
        opacityAnimation.Target = "Opacity";

        if (string.IsNullOrEmpty(ease) || ease.Contains("linear", StringComparison.CurrentCultureIgnoreCase))
            easer = compositor.CreateLinearEasingFunction();
        else
        {
            //easer = compositor.CreateCubicBezierEasingFunction(new(1f, 0.3f), new(0.6f, 0.7f));
            easer = CreatePennerEquation(compositor, ease);
        }

        opacityAnimation.InsertKeyFrame(0.0f, (float)from, easer);
        opacityAnimation.InsertKeyFrame(1.0f, (float)to, easer);
        targetVisual.StartAnimation("Opacity", opacityAnimation);
    }

    /// <summary>
    /// This should be moved to a shared module, but I want to keep these behaviors portable.
    /// </summary>
    static Microsoft.UI.Composition.CompositionEasingFunction CreatePennerEquation(Microsoft.UI.Composition.Compositor compositor, string pennerType = "SineEaseInOut")
    {
        System.Numerics.Vector2 controlPoint1;
        System.Numerics.Vector2 controlPoint2;
        switch (pennerType)
        {
            case "SineEaseIn":
                controlPoint1 = new System.Numerics.Vector2(0.47f, 0.0f);
                controlPoint2 = new System.Numerics.Vector2(0.745f, 0.715f);
                break;
            case "SineEaseOut":
                controlPoint1 = new System.Numerics.Vector2(0.39f, 0.575f);
                controlPoint2 = new System.Numerics.Vector2(0.565f, 1.0f);
                break;
            case "SineEaseInOut":
                controlPoint1 = new System.Numerics.Vector2(0.445f, 0.05f);
                controlPoint2 = new System.Numerics.Vector2(0.55f, 0.95f);
                break;
            case "QuadEaseIn":
                controlPoint1 = new System.Numerics.Vector2(0.55f, 0.085f);
                controlPoint2 = new System.Numerics.Vector2(0.68f, 0.53f);
                break;
            case "QuadEaseOut":
                controlPoint1 = new System.Numerics.Vector2(0.25f, 0.46f);
                controlPoint2 = new System.Numerics.Vector2(0.45f, 0.94f);
                break;
            case "QuadEaseInOut":
                controlPoint1 = new System.Numerics.Vector2(0.445f, 0.03f);
                controlPoint2 = new System.Numerics.Vector2(0.515f, 0.955f);
                break;
            case "CubicEaseIn":
                controlPoint1 = new System.Numerics.Vector2(0.55f, 0.055f);
                controlPoint2 = new System.Numerics.Vector2(0.675f, 0.19f);
                break;
            case "CubicEaseOut":
                controlPoint1 = new System.Numerics.Vector2(0.215f, 0.61f);
                controlPoint2 = new System.Numerics.Vector2(0.355f, 1.0f);
                break;
            case "CubicEaseInOut":
                controlPoint1 = new System.Numerics.Vector2(0.645f, 0.045f);
                controlPoint2 = new System.Numerics.Vector2(0.355f, 1.0f);
                break;
            case "QuarticEaseIn":
                controlPoint1 = new System.Numerics.Vector2(0.895f, 0.03f);
                controlPoint2 = new System.Numerics.Vector2(0.685f, 0.22f);
                break;
            case "QuarticEaseOut":
                controlPoint1 = new System.Numerics.Vector2(0.165f, 0.84f);
                controlPoint2 = new System.Numerics.Vector2(0.44f, 1.0f);
                break;
            case "QuarticEaseInOut":
                controlPoint1 = new System.Numerics.Vector2(0.77f, 0.0f);
                controlPoint2 = new System.Numerics.Vector2(0.175f, 1.0f);
                break;
            case "QuinticEaseIn":
                controlPoint1 = new System.Numerics.Vector2(0.755f, 0.05f);
                controlPoint2 = new System.Numerics.Vector2(0.855f, 0.06f);
                break;
            case "QuinticEaseOut":
                controlPoint1 = new System.Numerics.Vector2(0.23f, 1.0f);
                controlPoint2 = new System.Numerics.Vector2(0.32f, 1.0f);
                break;
            case "QuinticEaseInOut":
                controlPoint1 = new System.Numerics.Vector2(0.86f, 0.0f);
                controlPoint2 = new System.Numerics.Vector2(0.07f, 1.0f);
                break;
            case "ExponentialEaseIn":
                controlPoint1 = new System.Numerics.Vector2(0.95f, 0.05f);
                controlPoint2 = new System.Numerics.Vector2(0.795f, 0.035f);
                break;
            case "ExponentialEaseOut":
                controlPoint1 = new System.Numerics.Vector2(0.19f, 1.0f);
                controlPoint2 = new System.Numerics.Vector2(0.22f, 1.0f);
                break;
            case "ExponentialEaseInOut":
                controlPoint1 = new System.Numerics.Vector2(1.0f, 0.0f);
                controlPoint2 = new System.Numerics.Vector2(0.0f, 1.0f);
                break;
            case "CircleEaseIn":
                controlPoint1 = new System.Numerics.Vector2(0.6f, 0.04f);
                controlPoint2 = new System.Numerics.Vector2(0.98f, 0.335f);
                break;
            case "CircleEaseOut":
                controlPoint1 = new System.Numerics.Vector2(0.075f, 0.82f);
                controlPoint2 = new System.Numerics.Vector2(0.165f, 1.0f);
                break;
            case "CircleEaseInOut":
                controlPoint1 = new System.Numerics.Vector2(0.785f, 0.135f);
                controlPoint2 = new System.Numerics.Vector2(0.15f, 0.86f);
                break;
            case "BackEaseIn":
                controlPoint1 = new System.Numerics.Vector2(0.6f, -0.28f);
                controlPoint2 = new System.Numerics.Vector2(0.735f, 0.045f);
                break;
            case "BackEaseOut":
                controlPoint1 = new System.Numerics.Vector2(0.175f, 0.885f);
                controlPoint2 = new System.Numerics.Vector2(0.32f, 1.275f);
                break;
            case "BackEaseInOut":
                controlPoint1 = new System.Numerics.Vector2(0.68f, -0.55f);
                controlPoint2 = new System.Numerics.Vector2(0.265f, 1.55f);
                break;
            default:
                controlPoint1 = new System.Numerics.Vector2(0.0f);
                controlPoint2 = new System.Numerics.Vector2(0.0f);
                break;
        }
        Microsoft.UI.Composition.CompositionEasingFunction pennerEquation = compositor.CreateCubicBezierEasingFunction(controlPoint1, controlPoint2);
        return pennerEquation;
    }
    #endregion
}


#region [Early Experiments]
/// <summary>
/// This <see cref="Microsoft.Xaml.Interactivity.Behavior"/> pattern still needs some fine tuning.
/// When the <see cref="FrameworkElement"/> receives the focus an opacity and translation animation will be performed.
/// </summary>
public class OpacityAnimationBehaviorExperimental : Behavior<FrameworkElement>
{
    DispatcherTimer? _timer;
    static bool _hasFocus = false;
    Storyboard? _storyboardOn;
    Storyboard? _storyboardOff;

    protected override void OnAttached()
    {
        base.OnAttached();

        if (!App.AnimationsEffectsEnabled)
            return;

        AssociatedObject.Loaded += AssociatedObject_Loaded;
        AssociatedObject.Unloaded += AssociatedObject_Unloaded;
        AssociatedObject.GotFocus += AssociatedObject_GotFocus;
        AssociatedObject.LostFocus += AssociatedObject_LostFocus;
        AssociatedObject.GettingFocus += AssociatedObject_GettingFocus;
        AssociatedObject.LosingFocus += AssociatedObject_LosingFocus;

        _timer = new DispatcherTimer();
        _timer.Tick += Timer_Tick;
        _timer.Interval = TimeSpan.FromMilliseconds(1000);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (!App.AnimationsEffectsEnabled)
            return;

        AssociatedObject.Loaded -= AssociatedObject_Loaded;
        AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
        AssociatedObject.GotFocus -= AssociatedObject_GotFocus;
        AssociatedObject.LostFocus -= AssociatedObject_LostFocus;
        AssociatedObject.GettingFocus -= AssociatedObject_GettingFocus;
        AssociatedObject.LosingFocus -= AssociatedObject_LosingFocus;

        if (_timer != null)
        {
            _timer.Stop();
            _timer = null;
        }
    }

    void Timer_Tick(object? sender, object e)
    {
        if (!_hasFocus)
        {
            Debug.WriteLine($"[INFO] Running StoryboardOff at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
            _storyboardOff?.Begin();
        }
        _timer?.Stop();
    }

    void AssociatedObject_GettingFocus(UIElement sender, Microsoft.UI.Xaml.Input.GettingFocusEventArgs args)
    {
        Debug.WriteLine($"[INFO] Getting focus at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
        _hasFocus = true;
    }

    void AssociatedObject_LosingFocus(UIElement sender, Microsoft.UI.Xaml.Input.LosingFocusEventArgs args)
    {
        Debug.WriteLine($"[INFO] Losing focus at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");

        if (_timer != null && _timer.IsEnabled)
            return;

        _hasFocus = false;
        _timer?.Start();
    }

    void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
        var obj = sender as FrameworkElement;
        if (obj is null) { return; }

        //AnimateUIElementScale(0.2, TimeSpan.FromSeconds(0.1), (UIElement)sender);

        var daOn = new DoubleAnimation
        {
            From = 0.2d,
            To = 1d,
            AutoReverse = false,
            Duration = TimeSpan.FromSeconds(1.3),
            EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut }
        };

        var daOff = new DoubleAnimation
        {
            From = 1d,
            To = 0.2d,
            AutoReverse = false,
            Duration = TimeSpan.FromSeconds(1.3),
            EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut }
        };

        _storyboardOn = new Storyboard();
        Storyboard.SetTarget(daOn, obj);
        Storyboard.SetTargetName(daOn, obj.Name);
        Storyboard.SetTargetProperty(daOn, "Opacity");
        _storyboardOn.Children.Add(daOn);

        _storyboardOff = new Storyboard();
        Storyboard.SetTarget(daOff, obj);
        Storyboard.SetTargetName(daOff, obj.Name);
        Storyboard.SetTargetProperty(daOff, "Opacity");
        _storyboardOff.Children.Add(daOff);
        _storyboardOff.Completed += StoryboardOffCompleted;
    }

    void StoryboardOffCompleted(object? sender, object e)
    {
        _timer?.Stop();
    }

    void AssociatedObject_GotFocus(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] {sender.GetType()} got focus.");

        if (_timer != null && _timer.IsEnabled)
        {
            Debug.WriteLine($"[INFO] Skipping StoryBoardOn since timer is running.");
            return;
        }

        var obj = sender as FrameworkElement;
        if (obj is null) { return; }

        if (obj.Visibility == Visibility.Visible)
            _storyboardOn?.Begin();
        else
            _storyboardOn?.SkipToFill(); //_storyboard.Stop();

        //AnimateUIElementOpacity(0.1, 1.0, TimeSpan.FromSeconds(2.0), obj);
        //AnimateUIElementScale(1.0, TimeSpan.FromSeconds(1.0), (UIElement)sender);
        if (obj.ActualHeight != double.NaN && obj.ActualHeight != 0)
            AnimateUIElementOffset(new Point(0, obj.ActualHeight), TimeSpan.FromSeconds(0.8), (UIElement)sender);
        else
            AnimateUIElementOffset(new Point(0, 600), TimeSpan.FromSeconds(0.8), (UIElement)sender);
    }

    void AssociatedObject_LostFocus(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] {sender.GetType()} lost focus.");

        //var obj = sender as FrameworkElement;
        //if (obj is null) { return; }
        //
        //if (obj.Visibility == Visibility.Visible)
        //    _storyboardOff?.Begin();
        //else
        //    _storyboardOff?.SkipToFill();
    }

    void AssociatedObject_Activated(object sender, WindowActivatedEventArgs args)
    {
    }

    /// <summary>
    /// Mock disposal routine.
    /// </summary>
    void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_storyboardOn != null)
        {
            _storyboardOn.Stop();
            _storyboardOn = null;
        }

        if (_storyboardOff != null)
        {
            _storyboardOff.Stop();
            _storyboardOff = null;
        }

        if (_timer != null)
        {
            _timer.Stop();
            _timer = null;
        }
    }

    /// <summary>
    /// Opacity animation using <see cref="Microsoft.UI.Composition.ScalarKeyFrameAnimation"/>
    /// </summary>
    void AnimateUIElementOpacity(double from, double to, TimeSpan duration, UIElement target, Microsoft.UI.Composition.AnimationDirection direction = Microsoft.UI.Composition.AnimationDirection.Normal)
    {
        var targetVisual = ElementCompositionPreview.GetElementVisual(target);
        var compositor = targetVisual.Compositor;
        var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
        opacityAnimation.Direction = direction;
        opacityAnimation.Duration = duration;
        opacityAnimation.Target = "Opacity";
        opacityAnimation.InsertKeyFrame(0.0f, (float)from);
        opacityAnimation.InsertKeyFrame(1.0f, (float)to);
        targetVisual.StartAnimation("Opacity", opacityAnimation);
    }

    /// <summary>
    /// Offset animation using <see cref="Microsoft.UI.Composition.Vector3KeyFrameAnimation"/>
    /// </summary>
    void AnimateUIElementOffset(Point to, TimeSpan duration, UIElement target)
    {
        var targetVisual = ElementCompositionPreview.GetElementVisual(target);
        var compositor = targetVisual.Compositor;
        var linear = compositor.CreateLinearEasingFunction();
        var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
        offsetAnimation.Duration = duration;
        offsetAnimation.Target = "Offset";
        offsetAnimation.InsertKeyFrame(0.0f, new Vector3((float)to.X, (float)to.Y, 0), linear);
        offsetAnimation.InsertKeyFrame(1.0f, new Vector3(0), linear);
        targetVisual.StartAnimation("Offset", offsetAnimation);
    }

    /// <summary>
    /// Scale animation using <see cref="Microsoft.UI.Composition.Vector3KeyFrameAnimation"/>
    /// </summary>
    void AnimateUIElementScale(double to, TimeSpan duration, UIElement target)
    {
        var targetVisual = ElementCompositionPreview.GetElementVisual(target);
        var compositor = targetVisual.Compositor;
        var linear = compositor.CreateLinearEasingFunction();
        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.Duration = duration;
        scaleAnimation.Target = "Scale";
        scaleAnimation.InsertKeyFrame(0.0f, new Vector3(0), linear);
        scaleAnimation.InsertKeyFrame(1.0f, new Vector3((float)to), linear);
        targetVisual.StartAnimation("Scale", scaleAnimation);
    }
}
#endregion