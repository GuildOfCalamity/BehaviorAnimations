using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;
using System;
using Windows.ApplicationModel;

namespace BehaviorAnimations.Behaviors;

/// <summary>
/// Base class for behaviors that solves 2 problems:
///   1. Prevent duplicate initialization that can happen (prevent multiple OnAttached calls);
///   2. Whenever <see cref="Initialize"/> initially fails, this method will subscribe to <see cref="FrameworkElement.SizeChanged"/> to allow lazy initialization.
/// </summary>
/// <typeparam name="T">The type of the associated object.</typeparam>
/// <seealso cref="Microsoft.Xaml.Interactivity.Behavior{T}" />
/// <remarks>
/// For more info, see https://github.com/CommunityToolkit/WindowsCommunityToolkit/issues/1008.
/// </remarks>
public abstract class BehaviorBase<T> : Behavior<T> where T : UIElement
{
    private bool _isAttaching;
    private bool _isAttached;

    /// <summary>
    /// Gets a value indicating whether this behavior is attached.
    /// </summary>
    /// <value>
    /// <c>true</c> if this behavior is attached; otherwise, <c>false</c>.
    /// </value>
    protected bool IsAttached
    {
        get => _isAttached;
    }

    /// <summary>
    /// Called after the behavior is attached to the <see cref="Microsoft.Xaml.Interactivity.Behavior.AssociatedObject" />.
    /// </summary>
    /// <remarks>
    /// Override this to hook up functionality to the <see cref="Microsoft.Xaml.Interactivity.Behavior.AssociatedObject" />
    /// </remarks>
    protected override void OnAttached()
    {
        base.OnAttached();

        HandleAttach();

        var frameworkElement = AssociatedObject as FrameworkElement;
        if (frameworkElement != null)
        {
            frameworkElement.Loaded += OnAssociatedObjectLoaded;
            frameworkElement.Unloaded += OnAssociatedObjectUnloaded;
            frameworkElement.SizeChanged += OnAssociatedObjectSizeChanged;
        }
    }

    /// <summary>
    /// Called when the behavior is being detached from its <see cref="Microsoft.Xaml.Interactivity.Behavior.AssociatedObject" />.
    /// </summary>
    /// <remarks>
    /// Override this to unhook functionality from the <see cref="Microsoft.Xaml.Interactivity.Behavior.AssociatedObject" />
    /// </remarks>
    protected override void OnDetaching()
    {
        base.OnDetaching();

        var frameworkElement = AssociatedObject as FrameworkElement;
        if (frameworkElement != null)
        {
            frameworkElement.Loaded -= OnAssociatedObjectLoaded;
            frameworkElement.Unloaded -= OnAssociatedObjectUnloaded;
            frameworkElement.SizeChanged -= OnAssociatedObjectSizeChanged;
        }

        HandleDetach();
    }

    #region [Overridable Methods]
    /// <summary>
    /// Called when the associated object has been loaded.
    /// </summary>
    protected virtual void OnAssociatedObjectLoaded()
    {
    }

    /// <summary>
    /// Called when the associated object has been unloaded.
    /// </summary>
    protected virtual void OnAssociatedObjectUnloaded()
    {
    }

    /// <summary>
    /// Initializes the behavior to the associated object.
    /// </summary>
    /// <returns><c>true</c> if the initialization succeeded; otherwise <c>false</c>.</returns>
    protected virtual bool Initialize()
    {
        return true;
    }

    /// <summary>
    /// Uninitializes the behavior from the associated object.
    /// </summary>
    /// <returns><c>true</c> if uninitialization succeeded; otherwise <c>false</c>.</returns>
    protected virtual bool Uninitialize()
    {
        return true;
    }
    #endregion

    void OnAssociatedObjectLoaded(object sender, RoutedEventArgs e)
    {
        if (!_isAttached)
            HandleAttach();

        OnAssociatedObjectLoaded();
    }

    void OnAssociatedObjectUnloaded(object sender, RoutedEventArgs e)
    {
        OnAssociatedObjectUnloaded();

        // Note: don't detach here, we'll let the behavior implementation take care of that
    }

    void OnAssociatedObjectSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_isAttached)
            HandleAttach();
    }

    void HandleAttach()
    {
        if (_isAttaching || _isAttached)
            return;

        _isAttaching = true;

        var attached = Initialize();
        if (attached)
            _isAttached = true;

        _isAttaching = false;
    }

    void HandleDetach()
    {
        if (!_isAttached)
            return;

        var detached = Uninitialize();
        if (detached)
            _isAttached = false;
    }
}

/// <summary>
/// WinUI3 currently (as of WindowsAppSdk 1.5) has a bug where Unloaded can fire out of order with Loaded, 
/// causing OnDetaching to be called on a behavior without OnAttached being called, but the object is still in use. 
/// A detailed explanation of what's going on in WinUI3 can be found here https://github.com/microsoft/microsoft-ui-xaml/issues/8342#issuecomment-2031017667
/// and a fix is expected  in the WindowsAppSdk 2.0 time-frame (undecided). This Loaded/Unloaded issue crops up 
/// frequently in areas that use control virtualization, particularly things like ListView and AutoSuggestBox.
/// The current workaround I've added is to implement a custom Behavior class to use instead of using the one built 
/// in to the framework, which checks AssociatedObject.IsLoaded and only detaches if the object is no longer loaded.
/// https://github.com/microsoft/XamlBehaviors/issues/251
/// </summary>
/// <typeparam name="T"><see cref="DependencyObject"/></typeparam>
public abstract class BehaviorWorkaround<T> : DependencyObject, IBehavior where T : DependencyObject
{
    protected BehaviorWorkaround()
    {
        AssociatedObject = null;
    }

    DependencyObject IBehavior.AssociatedObject => AssociatedObject;

    public T AssociatedObject
    {
        get;
        private set;
    }

    public void Attach(DependencyObject? associatedObject)
    {
        if (associatedObject == null || ReferenceEquals(associatedObject, AssociatedObject) || DesignMode.DesignModeEnabled)
        {
            // do nothing, object is already attached
        }
        else if (associatedObject is T typedObject)
        {
            AssociatedObject = typedObject;
            OnAttached();
        }
        else
        {
            throw new InvalidOperationException($"AssociatedObject is expected to be type {typeof(T).FullName} but was {associatedObject.GetType().FullName}");
        }
    }

    public void Detach()
    {
        if (AssociatedObject is FrameworkElement { IsLoaded: true } element)
        {
            // This case happens when the control is removed from the visual tree and added back before the Unloaded event is fired.
            // Details on why this happens can be found here: https://github.com/microsoft/microsoft-ui-xaml/issues/8342#issuecomment-2031017667
            // This bug is expected to be fixed in the WindowsAppSdk 2.0 time-frame, at which point this workaround can be removed.
        }
        else
        {
            OnDetaching();
            AssociatedObject = default!;
        }
    }

    #region [Overridable Methods]
    protected virtual void OnAttached()
    {
    }

    protected virtual void OnDetaching()
    {
    }
    #endregion
}
