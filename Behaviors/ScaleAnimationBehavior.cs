using System;
using System.Diagnostics;
using System.Numerics;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Xaml.Interactivity;


namespace BehaviorAnimations.Behaviors;

/// <summary>
/// <see cref="FrameworkElement"/> <see cref="Microsoft.Xaml.Interactivity.Behavior"/>.
/// </summary>
public class ScaleAnimationBehavior : Behavior<FrameworkElement>
{
    #region [Props]
    /// <summary>
    /// Identifies the <see cref="Seconds"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty SecondsProperty = DependencyProperty.Register(
        nameof(Seconds),
        typeof(double),
        typeof(ScaleAnimationBehavior),
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
    /// Identifies the <see cref="Final"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty FinalProperty = DependencyProperty.Register(
        nameof(Final),
        typeof(double),
        typeof(ScaleAnimationBehavior),
        new PropertyMetadata(1d));

    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public double Final
    {
        get => (double)GetValue(FinalProperty);
        set => SetValue(FinalProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="EaseMode"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty EaseModeProperty = DependencyProperty.Register(
        nameof(EaseMode),
        typeof(string),
        typeof(ScaleAnimationBehavior),
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

    /// <summary>
    /// <see cref="FrameworkElement"/> event.
    /// </summary>
    void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] {sender.GetType().Name} loaded at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");

        var obj = sender as FrameworkElement;
        if (obj is null) { return; }

        Debug.WriteLine($"[INFO] Scale animation will run for {Seconds} seconds.");
        AnimateUIElementScale(Final, TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode);
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
    /// Scale animation using <see cref="Microsoft.UI.Composition.Vector3KeyFrameAnimation"/>
    /// </summary>
    void AnimateUIElementScale(double to, TimeSpan duration, UIElement target, string ease, Microsoft.UI.Composition.AnimationDirection direction = Microsoft.UI.Composition.AnimationDirection.Normal)
    {
        Microsoft.UI.Composition.CompositionEasingFunction easer;
        var targetVisual = ElementCompositionPreview.GetElementVisual(target);
        if (targetVisual is null) { return; }

        //targetVisual.Size = new Vector2(target.ActualSize.X, target.ActualSize.Y);
        //targetVisual.AnchorPoint = new Vector2(-1f, -1f); // used mostly in rotation angle, not scaling
        //targetVisual.RelativeOffsetAdjustment = new Vector3(1f, 0f, 0f);

        // This is very important for the effect to work properly.
        targetVisual.CenterPoint = new Vector3(target.ActualSize.X / 2f, target.ActualSize.Y / 2f, 0f);

        var compositor = targetVisual.Compositor;
        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.StopBehavior = Microsoft.UI.Composition.AnimationStopBehavior.SetToFinalValue;
        scaleAnimation.Direction = direction;
        scaleAnimation.Duration = duration;
        scaleAnimation.Target = "Scale";

        if (string.IsNullOrEmpty(ease) || ease.Contains("linear", StringComparison.CurrentCultureIgnoreCase))
            easer = compositor.CreateLinearEasingFunction();
        else
            easer = CreatePennerEquation(compositor, ease);

        scaleAnimation.InsertKeyFrame(0.0f, new Vector3(0), easer);
        scaleAnimation.InsertKeyFrame(1.0f, new Vector3((float)to), easer);
        targetVisual.StartAnimation("Scale", scaleAnimation);
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
