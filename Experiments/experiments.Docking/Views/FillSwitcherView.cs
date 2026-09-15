using System.Numerics;
using Rin.Core.Views.Composite;

namespace experiments.Docking.Views;

/// <summary>A <see cref="SwitcherView" /> whose active child fills the pane instead of shrink-wrapping.</summary>
public sealed class FillSwitcherView : SwitcherView
{
    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        if (SelectedView is { } view)
        {
            view.Offset = default;
            view.Layout(availableSpace, true);
        }

        return availableSpace;
    }
}
