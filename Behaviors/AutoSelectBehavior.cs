using Microsoft.UI.Xaml.Controls;

namespace BehaviorAnimations.Behaviors;

// <summary>
/// This behavior automatically selects the entire content of the associated <see cref="TextBox"/> when it is loaded.
/// </summary>
public sealed class AutoSelectBehavior : BehaviorBase<TextBox>
{
    /// <inheritdoc/>
    protected override void OnAssociatedObjectLoaded() => AssociatedObject.SelectAll();
}
