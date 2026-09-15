namespace experiments.Docking.Model;

/// <summary>
///     Axis a <see cref="DockSplitNode" /> arranges its children along.
/// </summary>
public enum DockOrientation
{
    /// <summary>Children sit left-to-right (a row of columns).</summary>
    Horizontal,

    /// <summary>Children sit top-to-bottom (a column of rows).</summary>
    Vertical
}

/// <summary>
///     Where a dragged panel will land relative to the tab group it is dropped on.
/// </summary>
public enum DockRegion
{
    /// <summary>Add as another tab in the target group.</summary>
    Center,
    Left,
    Right,
    Top,
    Bottom
}
