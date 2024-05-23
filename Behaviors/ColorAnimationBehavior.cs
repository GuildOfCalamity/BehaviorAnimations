using System;
using System.Diagnostics;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.Xaml.Interactivity;

namespace BehaviorAnimations.Behaviors;

/// <summary>
/// <see cref="FrameworkElement"/> <see cref="Microsoft.Xaml.Interactivity.Behavior"/>.
/// </summary>
/// <remarks>
/// These are the bound events:
///  - Loaded..........: Applies From/To color.
///  - Unloaded........: No color change.
///  - PointerEntered..: Applies To/From color.
///  - PointerExited...: Applies From/To color.
/// </remarks>
public class ColorAnimationBehavior : Behavior<FrameworkElement>
{
    #region [Props]
    /// <summary>
    /// Identifies the <see cref="Seconds"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty SecondsProperty = DependencyProperty.Register(
        nameof(Seconds),
        typeof(double),
        typeof(ColorAnimationBehavior),
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
        typeof(Windows.UI.Color),
        typeof(ColorAnimationBehavior),
        new PropertyMetadata(Microsoft.UI.Colors.Transparent));

    /// <summary>
    /// Gets or sets the from color.
    /// </summary>
    public Windows.UI.Color From
    {
        get => (Windows.UI.Color)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="To"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty ToProperty = DependencyProperty.Register(
        nameof(To),
        typeof(Windows.UI.Color),
        typeof(ColorAnimationBehavior),
        new PropertyMetadata(Microsoft.UI.Colors.Transparent));

    /// <summary>
    /// Gets or sets the to color.
    /// </summary>
    public Windows.UI.Color To
    {
        get => (Windows.UI.Color)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="EaseMode"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty EaseModeProperty = DependencyProperty.Register(
        nameof(EaseMode),
        typeof(string),
        typeof(ColorAnimationBehavior),
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
    /// Identifies the <see cref="Interpolation"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty InterpolationProperty = DependencyProperty.Register(
        nameof(Interpolation),
        typeof(Microsoft.UI.Composition.CompositionColorSpace),
        typeof(ColorAnimationBehavior),
        new PropertyMetadata(Microsoft.UI.Composition.CompositionColorSpace.Rgb));

    /// <summary>
    /// Gets or sets the easing type for the compositor.
    /// </summary>
    public Microsoft.UI.Composition.CompositionColorSpace Interpolation
    {
        get => (Microsoft.UI.Composition.CompositionColorSpace)GetValue(InterpolationProperty);
        set => SetValue(InterpolationProperty, value);
    }
    #endregion

    protected override void OnAttached()
    {
        base.OnAttached();

        if (!App.AnimationsEffectsEnabled)
            return;

        AssociatedObject.Loaded += AssociatedObject_Loaded;
        AssociatedObject.Unloaded += AssociatedObject_Unloaded;
        AssociatedObject.PointerEntered += AssociatedObject_PointerEntered;
        AssociatedObject.PointerExited += AssociatedObject_PointerExited;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (!App.AnimationsEffectsEnabled)
            return;

        AssociatedObject.Loaded -= AssociatedObject_Loaded;
        AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
        AssociatedObject.PointerEntered -= AssociatedObject_PointerEntered;
        AssociatedObject.PointerExited -= AssociatedObject_PointerExited;
    }

    /// <summary>
    /// <see cref="FrameworkElement"/> event.
    /// </summary>
    void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] {sender.GetType().Name} loaded at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
        AnimateUIElementColor(From, To, TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode);
    }

    /// <summary>
    /// <see cref="FrameworkElement"/> event.
    /// </summary>
    void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] {sender.GetType().Name} unloaded at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
    }

    /// <summary>
    /// <see cref="FrameworkElement"/> event.
    /// </summary>
    void AssociatedObject_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AnimateUIElementColor(To, From, TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode);
    }

    /// <summary>
    /// <see cref="FrameworkElement"/> event.
    /// </summary>
    void AssociatedObject_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AnimateUIElementColor(From, To, TimeSpan.FromSeconds(Seconds), (UIElement)sender, EaseMode);
    }

    #region [Composition Animations]
    /// <summary>
    /// Only a <see cref="Microsoft.UI.Composition.CompositionColorBrush"/> can be animated.
    /// https://github.com/MicrosoftDocs/winrt-api/blob/docs/windows.ui.composition/compositionobject_startanimation_709050842.md
    /// </summary>
    void AnimateUIElementColor(Windows.UI.Color from, Windows.UI.Color to, TimeSpan duration, UIElement element, string ease)
    {
        Microsoft.UI.Composition.CompositionEasingFunction easer;
        var targetVisual = ElementCompositionPreview.GetElementVisual(element);
        if (targetVisual is null) { return; }
        var compositor = targetVisual.Compositor;
        var colorAnimation = compositor.CreateColorKeyFrameAnimation();
        colorAnimation.StopBehavior = Microsoft.UI.Composition.AnimationStopBehavior.LeaveCurrentValue;
        colorAnimation.Duration = duration;

        if (string.IsNullOrEmpty(ease) || ease.Contains("linear", StringComparison.CurrentCultureIgnoreCase))
            easer = compositor.CreateLinearEasingFunction();
        else
            easer = CreatePennerEquation(compositor, ease);

        colorAnimation.InsertKeyFrame(0, from, easer);
        colorAnimation.InsertKeyFrame(1, to, easer);
        // Set the interpolation to go through the RGB/HSL space.
        colorAnimation.InterpolationColorSpace = Interpolation;
        //colorAnimation.Target = "Color";
        var spriteVisual = compositor.CreateSpriteVisual();
        if (spriteVisual is null) { return; }
        /*
            The ColorKeyFrameAnimation class is one of the supported types of KeyFrameAnimations 
            that is used to animate the Color property off of the Brush property on a SpriteVisual. 
            When working with ColorKeyFrameAnimation s, utilize Windows.UI.Color objects for the 
            values of keyframes. Utilize the InterpolationColorSpace property to define which color 
            space the system will interpolate through for the animation.
            https://github.com/MicrosoftDocs/winrt-api/blob/docs//windows.ui.composition/colorkeyframeanimation.md
        */
        var ccb = compositor.CreateColorBrush();
        spriteVisual.CompositeMode = Microsoft.UI.Composition.CompositionCompositeMode.MinBlend;
        // Set the size of the sprite visual to cover the element.
        spriteVisual.RelativeSizeAdjustment = System.Numerics.Vector2.One;
        // Or you can be more specific:
        //spriteVisual.Offset = new System.Numerics.Vector3(1, 1, 0);
        //spriteVisual.Size = new System.Numerics.Vector2((float)element.ActualSize.X-2, (float)element.ActualSize.Y-2);
        spriteVisual.Brush = ccb;
        //ccb.StopAnimation("Color");
        // When using a sprite, the animation will not work unless the child visual is set.
        ElementCompositionPreview.SetElementChildVisual(element, spriteVisual);
        ccb.StartAnimation("Color", colorAnimation);
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

/// <summary>
/// <see cref="FrameworkElement"/> <see cref="Microsoft.Xaml.Interactivity.Behavior"/>.
/// </summary>
/// <remarks>
/// These are the bound events:
///  - Loaded..........: Applies From/To gradient brush.
///  - Unloaded........: No brush change.
///  - PointerEntered..: Applies To/From gradient brush.
///  - PointerExited...: Applies From/To gradient brush.
/// </remarks>
public class ColorGradientBehavior : Behavior<FrameworkElement>
{
    #region [Props]
    /// <summary>
    /// Identifies the <see cref="From"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty FromProperty = DependencyProperty.Register(
        nameof(From),
        typeof(Windows.UI.Color),
        typeof(ColorGradientBehavior),
        new PropertyMetadata(Microsoft.UI.Colors.Transparent));

    /// <summary>
    /// Gets or sets the from color.
    /// </summary>
    public Windows.UI.Color From
    {
        get => (Windows.UI.Color)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="To"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty ToProperty = DependencyProperty.Register(
        nameof(To),
        typeof(Windows.UI.Color),
        typeof(ColorGradientBehavior),
        new PropertyMetadata(Microsoft.UI.Colors.Transparent));

    /// <summary>
    /// Gets or sets the to color.
    /// </summary>
    public Windows.UI.Color To
    {
        get => (Windows.UI.Color)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }
    #endregion

    protected override void OnAttached()
    {
        base.OnAttached();

        if (!App.AnimationsEffectsEnabled)
            return;

        AssociatedObject.Loaded += AssociatedObject_Loaded;
        AssociatedObject.Unloaded += AssociatedObject_Unloaded;
        AssociatedObject.PointerEntered += AssociatedObject_PointerEntered;
        AssociatedObject.PointerExited += AssociatedObject_PointerExited;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (!App.AnimationsEffectsEnabled)
            return;

        AssociatedObject.Loaded -= AssociatedObject_Loaded;
        AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
        AssociatedObject.PointerEntered -= AssociatedObject_PointerEntered;
        AssociatedObject.PointerExited -= AssociatedObject_PointerExited;
    }

    /// <summary>
    /// <see cref="FrameworkElement"/> event.
    /// </summary>
    void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] {sender.GetType().Name} loaded at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
        AnimateUIElementColor(From, To, (UIElement)sender);
    }

    /// <summary>
    /// <see cref="FrameworkElement"/> event.
    /// </summary>
    void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[INFO] {sender.GetType().Name} unloaded at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
    }

    /// <summary>
    /// <see cref="FrameworkElement"/> event.
    /// </summary>
    void AssociatedObject_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AnimateUIElementColor(To, From, (UIElement)sender);
    }

    /// <summary>
    /// <see cref="FrameworkElement"/> event.
    /// </summary>
    void AssociatedObject_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AnimateUIElementColor(From, To, (UIElement)sender);
    }

    #region [Composition Animations]
    /// <summary>
    /// This provides a gradient brush effect, but it's not an animation.
    /// Only a <see cref="Microsoft.UI.Composition.CompositionColorBrush"/> can be animated.
    /// https://github.com/MicrosoftDocs/winrt-api/blob/docs/windows.ui.composition/compositionobject_startanimation_709050842.md
    /// </summary>
    void AnimateUIElementColor(Windows.UI.Color from, Windows.UI.Color to, UIElement element)
    {
        var targetVisual = ElementCompositionPreview.GetElementVisual(element);
        if (targetVisual is null) { return; }
        var compositor = targetVisual.Compositor;
        var spriteVisual = compositor.CreateSpriteVisual();
        if (spriteVisual is null) { return; }

        var gb = compositor.CreateLinearGradientBrush();
        // Define gradient stops.
        var gradientStops = gb.ColorStops;
        gradientStops.Insert(0, compositor.CreateColorGradientStop(0.0f, from));
        gradientStops.Insert(1, compositor.CreateColorGradientStop(1.0f, to));

        // Set the direction of the gradient (top to bottom).
        gb.StartPoint = new System.Numerics.Vector2(0, 0);
        gb.EndPoint = new System.Numerics.Vector2(0, 1);

        // Bitmap and clip edges are antialiased.
        spriteVisual.BorderMode = Microsoft.UI.Composition.CompositionBorderMode.Soft;

        // Subtract color channels in background.
        spriteVisual.CompositeMode = Microsoft.UI.Composition.CompositionCompositeMode.MinBlend;

        // Set the size of the sprite visual to cover the element.
        spriteVisual.RelativeSizeAdjustment = System.Numerics.Vector2.One;
        // Or you can be more specific:
        //spriteVisual.Offset = new System.Numerics.Vector3(1, 1, 0);
        //spriteVisual.Size = new System.Numerics.Vector2((float)element.ActualSize.X-2, (float)element.ActualSize.Y-2);

        spriteVisual.Brush = gb;
        // This throws an exception:
        //spriteVisual.Brush.StartAnimation("Color", colorAnimation);

        // Set the sprite visual as the background of the FrameworkElement.
        ElementCompositionPreview.SetElementChildVisual(element, spriteVisual);
    }
    #endregion
}

