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

namespace BehaviorAnimations.Behaviors;

/// <summary>
/// <see cref="FrameworkElement"/> <see cref="Microsoft.Xaml.Interactivity.Behavior"/>.
/// </summary>
/// <remarks>
/// You can add this to any control, but it looks best on <see cref="Button"/>s.
/// </remarks>
public class SpringAnimationBehavior : Behavior<FrameworkElement>
{
    #region [Props]
    DispatcherTimer? _timer;

    /// <summary>
    /// Identifies the <see cref="Seconds"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty SecondsProperty = DependencyProperty.Register(
        nameof(Seconds),
        typeof(double),
        typeof(SpringAnimationBehavior),
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
        typeof(SpringAnimationBehavior),
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
    /// Identifies the <see cref="Damping"/> property for the animation.
    /// </summary>
    public static readonly DependencyProperty DampingProperty = DependencyProperty.Register(
        nameof(Damping),
        typeof(double),
        typeof(SpringAnimationBehavior),
        new PropertyMetadata(0.25f));

    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public double Damping
    {
        get => (double)GetValue(DampingProperty);
        set => SetValue(DampingProperty, value);
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
        AnimateUIElementSpring(Final, TimeSpan.FromSeconds(Seconds), (UIElement)sender, Damping);
    }

    /// <summary>
    /// <see cref="FrameworkElement"/> event.
    /// </summary>
    void AssociatedObject_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AnimateUIElementSpring(1.0, TimeSpan.FromSeconds(Seconds), (UIElement)sender, Damping);
    }

    #region [Composition Animations]
    /// <summary>
    /// Bounce animation using <see cref="Microsoft.UI.Composition.Vector3KeyFrameAnimation"/>
    /// </summary>
    void AnimateUIElementSpring(double to, TimeSpan duration, UIElement target, double damping)
    {
        var targetVisual = ElementCompositionPreview.GetElementVisual(target);
        if (targetVisual is null) { return; }
        var compositor = targetVisual.Compositor;
        var springAnimation = compositor.CreateSpringVector3Animation();
        springAnimation.StopBehavior = Microsoft.UI.Composition.AnimationStopBehavior.SetToFinalValue;
        springAnimation.FinalValue = new Vector3((float)to);
        springAnimation.Period = duration;
        springAnimation.DampingRatio = (float)damping;
        springAnimation.Target = "Scale";
        targetVisual.StartAnimation("Scale", springAnimation);
    }
    #endregion
}
