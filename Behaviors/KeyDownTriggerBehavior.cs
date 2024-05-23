using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.Xaml.Interactivity;
using System;
using System.Diagnostics;
using Windows.System;

namespace BehaviorAnimations.Behaviors;

/// <summary>
/// This behavior listens to a key down event on the associated <see cref="UIElement"/> when it is loaded and executes an action.
/// </summary>
/// <remarks>
/// This is similar to EventTriggerBehavior EventName="KeyDown" but allows you to specify the key that will cause the trigger.
/// Original version: https://github.com/CommunityToolkit/WindowsCommunityToolkit/blob/main/Microsoft.Toolkit.Uwp.UI.Behaviors/Keyboard/KeyDownTriggerBehavior.cs
/// </remarks>
[TypeConstraint(typeof(FrameworkElement))]
public class KeyDownTriggerBehavior : Trigger<FrameworkElement>
{
    /// <summary>
    /// Identifies the <see cref="Key"/> property.
    /// </summary>
    public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
        nameof(Key),
        typeof(VirtualKey),
        typeof(KeyDownTriggerBehavior),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the key to listen when the associated object is loaded.
    /// </summary>
    public VirtualKey Key
    {
        get => (VirtualKey)GetValue(KeyProperty);
        set => SetValue(KeyProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        ((FrameworkElement)AssociatedObject).KeyDown += OnAssociatedObjectKeyDown;
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        ((FrameworkElement)AssociatedObject).KeyDown -= OnAssociatedObjectKeyDown;
    }

    /// <summary>
    /// Invokes the current actions when the <see cref="Windows.System.VirtualKey"/> is pressed.
    /// </summary>
    /// <param name="sender">The source <see cref="UIElement"/> instance.</param>
    /// <param name="keyRoutedEventArgs">The arguments for the event (unused).</param>
    void OnAssociatedObjectKeyDown(object sender, KeyRoutedEventArgs keyRoutedEventArgs)
    {
        Debug.WriteLine($"[INFO] Received behavior key: {keyRoutedEventArgs.Key}");

        if (keyRoutedEventArgs.Key == Key)
        {
            keyRoutedEventArgs.Handled = true;
            try
            {
                Interaction.ExecuteActions(sender, Actions, keyRoutedEventArgs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] ExecuteActions: {ex.Message}");
            }
        }
    }
}