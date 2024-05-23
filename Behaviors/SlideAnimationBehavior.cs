using System;
using System.Diagnostics;
using System.Numerics;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Xaml.Interactivity;

using Windows.Foundation;
using Windows.System;
using Windows.UI.Core.AnimationMetrics;

namespace BehaviorAnimations.Behaviors;

/// <summary>
/// When the <see cref="FrameworkElement"/> is loaded the translation animation will be performed.
/// We'll consider sliding up to be transitioning into a usable state, while sliding down means to put away.
/// </summary>
public class SlideAnimationBehavior : Behavior<FrameworkElement>
{
    #region [Props]
    DispatcherTimer? _timer;

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
    /// Identifies the <see cref="Down"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register(
        nameof(Direction),
        typeof(string),
        typeof(SlideAnimationBehavior),
        new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets the direction.
    /// </summary>
    public string Direction
    {
        get => (string)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
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

    /// <summary>
    /// Identifies the <see cref="CollapseOnFinish"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty CollapseOnFinishProperty = DependencyProperty.Register(
        nameof(CollapseOnFinish),
        typeof(bool),
        typeof(SlideAnimationBehavior),
        new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets the visibility for the compositor.
    /// </summary>
    public bool CollapseOnFinish
    {
        get => (bool)GetValue(CollapseOnFinishProperty);
        set => SetValue(CollapseOnFinishProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="FallbackAmount"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty FallbackAmountProperty = DependencyProperty.Register(
        nameof(FallbackAmount),
        typeof(double),
        typeof(SlideAnimationBehavior),
        new PropertyMetadata(200d));

    /// <summary>
    /// Gets or sets the default amount to slide is not control size can be acquired.
    /// </summary>
    public double FallbackAmount
    {
        get => (double)GetValue(FallbackAmountProperty);
        set => SetValue(FallbackAmountProperty, value);
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

    /// <summary>
    /// <see cref="FrameworkElement"/> event.
    /// </summary>
    void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] {sender.GetType().Name} loaded at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");

        var obj = sender as FrameworkElement;
        if (obj is null) { return; }

        if (obj.ActualHeight != double.NaN && obj.ActualHeight != 0)
        {
            Debug.WriteLine($"[INFO] Reported {sender.GetType().Name} width by height: {obj.ActualHeight} pixels by {obj.ActualWidth} pixels");

            if (Direction.Equals("up", StringComparison.CurrentCultureIgnoreCase))
                AnimateUIElementOffset(new Point(0, obj.ActualHeight), TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode);
            else if(Direction.Equals("down", StringComparison.CurrentCultureIgnoreCase))
                AnimateUIElementOffset(new Point(0, obj.ActualHeight), TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode, Microsoft.UI.Composition.AnimationDirection.Reverse);
            else if (Direction.Equals("left", StringComparison.CurrentCultureIgnoreCase))
                AnimateUIElementOffset(new Point(obj.ActualWidth, 0), TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode);
            else if (Direction.Equals("right", StringComparison.CurrentCultureIgnoreCase))
                AnimateUIElementOffset(new Point(obj.ActualWidth, 0), TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode, Microsoft.UI.Composition.AnimationDirection.Reverse);
            else // default is up
                AnimateUIElementOffset(new Point(0, obj.ActualHeight), TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode);
        }
        else
        {
            if (Direction.Equals("up", StringComparison.CurrentCultureIgnoreCase))
                AnimateUIElementOffset(new Point(0, (float)FallbackAmount), TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode);
            else if (Direction.Equals("down", StringComparison.CurrentCultureIgnoreCase))
                AnimateUIElementOffset(new Point(0, (float)FallbackAmount), TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode, Microsoft.UI.Composition.AnimationDirection.Reverse);
            else if (Direction.Equals("left", StringComparison.CurrentCultureIgnoreCase))
                AnimateUIElementOffset(new Point((float)FallbackAmount, 0), TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode);
            else if (Direction.Equals("right", StringComparison.CurrentCultureIgnoreCase))
                AnimateUIElementOffset(new Point((float)FallbackAmount, 0), TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode, Microsoft.UI.Composition.AnimationDirection.Reverse);
            else // default is up
                AnimateUIElementOffset(new Point(0, (float)FallbackAmount), TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode);
        }

        #region [using DispatcherTimer for collapse]
        //if (CollapseOnFinish)
        //{   // If the control happens to slide down over another control then the pointer's
        //    // hit test may become a problem, so we'll set the visibility property.
        //    _timer = new DispatcherTimer();
        //    _timer.Interval = TimeSpan.FromSeconds(Seconds);
        //    _timer.Tick += (_, _) =>
        //    {
        //        _timer.Stop();
        //        ((UIElement)sender).Visibility = Visibility.Collapsed;
        //    };
        //    _timer.Start();
        //}
        #endregion
    }

    /// <summary>
    /// <see cref="FrameworkElement"/> event.
    /// </summary>
    void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] {sender.GetType().Name} unloaded at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
    }

    #region [Composition Animations]
    /// <summary>
    /// Offset animation using <see cref="Microsoft.UI.Composition.Vector3KeyFrameAnimation"/>
    /// </summary>
    void AnimateUIElementOffset(Point to, TimeSpan duration, UIElement target, string ease, Microsoft.UI.Composition.AnimationDirection direction = Microsoft.UI.Composition.AnimationDirection.Normal)
    {
        Microsoft.UI.Composition.CompositionEasingFunction easer;
        var targetVisual = ElementCompositionPreview.GetElementVisual(target);
        if (targetVisual is null) { return; }
        var compositor = targetVisual.Compositor;
        var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
        offsetAnimation.StopBehavior = Microsoft.UI.Composition.AnimationStopBehavior.SetToFinalValue;
        offsetAnimation.Direction = direction;
        offsetAnimation.Duration = duration;
        offsetAnimation.Target = "Offset";

        if (string.IsNullOrEmpty(ease) || ease.Contains("linear", StringComparison.CurrentCultureIgnoreCase))
            easer = compositor.CreateLinearEasingFunction();
        else
            easer = CreatePennerEquation(compositor, ease);

        offsetAnimation.InsertKeyFrame(0.0f, new Vector3((float)to.X, (float)to.Y, 0), easer);
        offsetAnimation.InsertKeyFrame(1.0f, new Vector3(0), easer);

        // Create a scoped batch so we can setup a completed event.
        var batch = targetVisual.Compositor.CreateScopedBatch(Microsoft.UI.Composition.CompositionBatchTypes.Animation);
        batch.Completed += (s, e) => 
        { 
            Debug.WriteLine($"[INFO] Animation completed for {target.GetType().Name}");
            if (CollapseOnFinish)
            {   // If the control happens to slide down over another control then the pointer's
                // hit test may become a problem, so we'll set the visibility property.
                target.Visibility = Visibility.Collapsed;
            }
        };

        //ElementCompositionPreview.SetIsTranslationEnabled(target, true);

        targetVisual.StartAnimation("Offset", offsetAnimation);

        // You must call End to get the completed event to fire.
        batch.End();
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
