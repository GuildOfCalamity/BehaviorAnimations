using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;

namespace BehaviorAnimations.Behaviors;

public class SnapButtonBehavior : Behavior<ButtonBase>
{
    #region [Props]
    bool attached;
    long paddingChangedEventToken;
    long contentChangedEventToken;
    VisualStateGroup? visualStateGroup;
    ContentPresenter? contentPresenter;
    Visual? contentVisual;
    Compositor compositor;
    CompositionPropertySet propSet;
    Vector3KeyFrameAnimation translationAnimation1;
    Vector3KeyFrameAnimation translationAnimation2;

    /// <summary>
    /// Gets or sets the direction for the animation.
    /// </summary>
    public ButtonContentSnapType SnapType
    {
        get { return (ButtonContentSnapType)GetValue(SnapTypeProperty); }
        set { SetValue(SnapTypeProperty, value); }
    }
    public static readonly DependencyProperty SnapTypeProperty = DependencyProperty.Register(
        nameof(SnapType),
        typeof(ButtonContentSnapType),
        typeof(SnapButtonBehavior),
        new PropertyMetadata(ButtonContentSnapType.None, (s, a) =>
        {
            if (s is SnapButtonBehavior sender && !Equals(a.NewValue, a.OldValue))
            {
                sender.UpdateSnapType();
            }
        }));

    /// <summary>
    /// Gets or sets the <see cref="TimeSpan"/> to run the animation for (default is 300 milliseconds).
    /// </summary>
    public double DurationSeconds
    {
        get => (double)GetValue(DurationSecondsProperty);
        set => SetValue(DurationSecondsProperty, value);
    }
    public static readonly DependencyProperty DurationSecondsProperty = DependencyProperty.Register(
        nameof(DurationSeconds),
        typeof(double),
        typeof(SnapButtonBehavior),
        new PropertyMetadata(0.3d, OnDurationPropertyChanged));

    /// <summary>
    /// Upon trigger, the <see cref="DependencyObject"/> will be the control itself (<see cref="SnapButtonBehavior"/>)
    /// and the <see cref="DependencyPropertyChangedEventArgs"/> will be the <see cref="Action"/> object contained within.
    /// </summary>
    static void OnDurationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = (SnapButtonBehavior)d;
        if (behavior != null && e.NewValue is double dbl)
        {
            behavior.DurationSeconds = dbl;
            behavior.UpdateDurations();
        }
    }
    #endregion

    /// <summary>
    /// Our trick/workaround for the static property changed problem.
    /// </summary>
    /// <remarks>
    /// The CTOR may have already been called at this point so we'll 
    /// need to update the translation animation durations again.
    /// </remarks>
    public void UpdateDurations()
    {
        if (translationAnimation1 != null)
            translationAnimation1.Duration = TimeSpan.FromSeconds(DurationSeconds);

        if (translationAnimation2 != null)
            translationAnimation2.Duration = TimeSpan.FromSeconds(DurationSeconds);
    }

    public SnapButtonBehavior()
    {
        compositor = CompositionTarget.GetCompositorForCurrentThread();

        propSet = compositor.CreatePropertySet();
        propSet.InsertVector3("Offset", Vector3.Zero);

        translationAnimation1 = compositor.CreateVector3KeyFrameAnimation();
        translationAnimation1.InsertKeyFrame(1, Vector3.Zero);
        translationAnimation1.Duration = TimeSpan.FromSeconds(DurationSeconds);
        translationAnimation1.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;

        translationAnimation2 = compositor.CreateVector3KeyFrameAnimation();
        translationAnimation2.InsertExpressionKeyFrame(1, "propSet.Offset");
        translationAnimation2.Duration = TimeSpan.FromSeconds(DurationSeconds);
        translationAnimation2.SetReferenceParameter("propSet", propSet);
        translationAnimation2.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;
    }

    void AssociatedObject_Loaded(object sender, RoutedEventArgs e) => TryLoadContent((ButtonBase)sender);
    void AssociatedObject_Unloaded(object sender, RoutedEventArgs e) => UnloadContent((ButtonBase)sender);
    void AssociatedObject_LayoutUpdated(object? sender, object e) => TryLoadContent(AssociatedObject);
    void TryLoadContent(ButtonBase? button)
    {
        if (button == null) 
            return;

        button.LayoutUpdated -= AssociatedObject_LayoutUpdated;

        if (button.IsLoaded)
        {
            if (VisualTreeHelper.GetChildrenCount(button) > 0)
                LoadContent(button);
            else
                button.LayoutUpdated += AssociatedObject_LayoutUpdated;
        }
        else
        {
            UnloadContent(button);
        }
    }

    void LoadContent(ButtonBase button)
    {
        if (attached) 
            return;

        // If the button's padding is changed we'll need to call UpdateSnapType().
        paddingChangedEventToken = button.RegisterPropertyChangedCallback(Control.PaddingProperty, OnPaddingPropertyChanged);
        visualStateGroup = VisualStateManager.GetVisualStateGroups((FrameworkElement)VisualTreeHelper.GetChild(button, 0)).FirstOrDefault(c => c.Name == "CommonStates");

        if (visualStateGroup != null)
            visualStateGroup.CurrentStateChanging += VisualStateGroup_CurrentStateChanging;

        contentPresenter = FindChild<ContentPresenter>(button);

        if (contentPresenter != null)
        {
            // If the button's content is changed we'll need to update the composition's SetIsTranslationEnabled and IsPixelSnappingEnabled.
            contentChangedEventToken = contentPresenter.RegisterPropertyChangedCallback(ContentPresenter.ContentProperty, OnContentChanged);
            var actualContent = VisualTreeHelper.GetChild(contentPresenter, 0) as UIElement;

            if (actualContent != null)
            {
                contentVisual = ElementCompositionPreview.GetElementVisual(actualContent);
                contentVisual.IsPixelSnappingEnabled = true;
                ElementCompositionPreview.SetIsTranslationEnabled(actualContent, true);
            }
        }

        attached = true;
        UpdateSnapType();
    }

    void UnloadContent(ButtonBase button)
    {
        if (!attached)
            return;

        attached = false;

        button.LayoutUpdated += AssociatedObject_LayoutUpdated;

        button.UnregisterPropertyChangedCallback(Control.PaddingProperty, paddingChangedEventToken);
        paddingChangedEventToken = 0;

        if (visualStateGroup != null)
            visualStateGroup.CurrentStateChanging -= VisualStateGroup_CurrentStateChanging;

        visualStateGroup = null;

        if (contentPresenter != null)
        {
            contentPresenter.UnregisterPropertyChangedCallback(ContentPresenter.ContentProperty, contentChangedEventToken);
            contentChangedEventToken = 0;
            contentPresenter = null;
        }

        if (contentVisual != null)
        {
            contentVisual.StopAnimation("Translation");
            contentVisual = null;
        }

        propSet.InsertVector3("Offset", Vector3.Zero);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Loaded += AssociatedObject_Loaded;
        AssociatedObject.Unloaded += AssociatedObject_Unloaded;

        TryLoadContent(AssociatedObject);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.Loaded -= AssociatedObject_Loaded;
        AssociatedObject.Unloaded -= AssociatedObject_Unloaded;

        UnloadContent(AssociatedObject);
    }

    void OnPaddingPropertyChanged(DependencyObject sender, DependencyProperty dp) => UpdateSnapType();

    void OnContentChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (attached && contentPresenter != null)
        {
            if (contentVisual != null)
            {
                contentVisual.StopAnimation("Translation");
                contentVisual = null;
            }

            var actualContent = VisualTreeHelper.GetChild(contentPresenter, 0) as UIElement;
            if (actualContent != null)
            {
                contentVisual = ElementCompositionPreview.GetElementVisual(actualContent);
                contentVisual.IsPixelSnappingEnabled = true;
                ElementCompositionPreview.SetIsTranslationEnabled(actualContent, true);
            }
        }
    }

    void VisualStateGroup_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
    {
        if (!attached || contentVisual == null)
            return;

        if (e.NewState?.Name == "PointerOver" || e.NewState?.Name == "Pressed")
            contentVisual.StartAnimation("Translation", translationAnimation1);
        else
            contentVisual.StartAnimation("Translation", translationAnimation2);
    }


    void UpdateSnapType()
    {
        var button = AssociatedObject;
        if (button == null) 
            return;

        if (attached)
        {
            bool hover = false;

            if (visualStateGroup != null)
                hover = visualStateGroup.CurrentState?.Name == "PointerOver" || visualStateGroup.CurrentState?.Name == "Pressed";

            var padding = button.Padding; // default is 11,5,11,6
            Debug.WriteLine($"[INFO] SnapButton will use thickness {padding}");

            var offset = SnapType switch
            {
                ButtonContentSnapType.Left => new Vector3((float)(-padding.Left), 0, 0),
                ButtonContentSnapType.Top => new Vector3(0, (float)(-padding.Top), 0),
                ButtonContentSnapType.Right => new Vector3((float)(padding.Right), 0, 0),
                ButtonContentSnapType.Bottom => new Vector3(0, (float)(padding.Bottom), 0),
                _ => Vector3.Zero // None
            };

            propSet.InsertVector3("Offset", offset);

            if (contentVisual != null)
            {
                contentVisual.StopAnimation("Translation");

                if (hover)
                    contentVisual.Properties.InsertVector3("Translation", Vector3.Zero);
                else
                    contentVisual.Properties.InsertVector3("Translation", offset);
            }
        }
        else
        {
            Debug.WriteLine($"[INFO] ButtonBase not attached yet.");
        }
    }

    private static T? FindChild<T>(DependencyObject obj) where T : DependencyObject
    {
        if (obj == null) 
            return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is T value) 
                return value;

            var value2 = FindChild<T>(child);
            if (value2 != null) 
                return value2;
        }

        return null;
    }
}

public enum ButtonContentSnapType
{
    None,
    Left,
    Top,
    Right,
    Bottom
}

