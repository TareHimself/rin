namespace Rin.Core.Views;

public enum Visibility
{
    /// <summary>
    ///     Visible, and the view and its children are hit tested.
    /// </summary>
    Visible,

    /// <summary>
    ///     Visible, but the view itself is not hit tested.
    /// </summary>
    VisibleNoHitTestSelf,

    /// <summary>
    ///     Visible, but the view's children are not hit tested.
    /// </summary>
    VisibleNoHitTestChildren,

    /// <summary>
    ///     Visible, but neither the view nor its children are hit tested.
    /// </summary>
    VisibleNoHitTestAll,

    /// <summary>
    ///     Takes up space but is not drawn or hit tested.
    /// </summary>
    Hidden,

    /// <summary>
    ///     Takes up no space.
    /// </summary>
    Collapsed
}
