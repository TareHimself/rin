using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;
using Rin.Core.Shared.Math;
using experiments.Docking.Model;

namespace experiments.Docking.Views;

/// <summary>
///     Root of a docking layout. Owns the <see cref="DockTree" />, builds the view subtree
///     from it, orchestrates tab drag-and-drop, and paints the drop-zone / drag-ghost overlay.
/// </summary>
public sealed class DockSpaceView : SingleSlotCompositeView
{
    private static readonly Color DropFill = new(0.20f, 0.55f, 0.95f, 0.28f);
    private static readonly Color DropBorder = new(0.35f, 0.70f, 1f, 0.9f);
    private static readonly Color GhostFill = new(0f, 0f, 0f, 0.75f);

    private readonly List<DockTabGroupView> _groups = [];

    private DockPanel? _dragPanel;
    private Vector2 _dragCursor;
    private DockTabGroupView? _dropTarget;
    private DockRegion _dropRegion;

    public DockSpaceView(DockTree tree)
    {
        Tree = tree;
        Rebuild();
    }

    public DockTree Tree { get; private set; }

    /// <summary>Swap in a completely new layout (used by the demo's "reset" key).</summary>
    public void ResetTo(DockTree tree)
    {
        CancelDrag();
        Tree = tree;
        Rebuild();
    }

    /// <summary>Tear down the view subtree and rebuild it from the current model.</summary>
    public void Rebuild()
    {
        _groups.Clear();
        _dropTarget = null;
        SetChild(BuildNode(Tree.Root));
    }

    private IView BuildNode(DockNode node)
    {
        switch (node)
        {
            case DockTabGroupNode group:
            {
                var view = new DockTabGroupView(group, this);
                _groups.Add(view);
                return view;
            }
            case DockSplitNode split:
            {
                var children = split.Children.Select(BuildNode).ToList();
                return new DockSplitView(split, children);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(node));
        }
    }

    // ---- drag orchestration (driven by DockTabView) ---------------------------------------

    public void BeginDrag(DockPanel panel, Vector2 cursorSurfacePos)
    {
        _dragPanel = panel;
        UpdateDrag(cursorSurfacePos);
    }

    public void UpdateDrag(Vector2 cursorSurfacePos)
    {
        _dragCursor = cursorSurfacePos;
        _dropTarget = null;

        foreach (var group in _groups)
        {
            var groupTransform = group.ComputeAbsoluteTransform();
            if (!group.PointWithin(groupTransform, cursorSurfacePos, true)) continue;

            var local = cursorSurfacePos.Transform(groupTransform.Inverse());
            _dropTarget = group;
            _dropRegion = ResolveRegion(local, group.GetSize());
            break;
        }
    }

    public void EndDrag(Vector2 cursorSurfacePos)
    {
        UpdateDrag(cursorSurfacePos);

        var panel = _dragPanel;
        var target = _dropTarget?.Node;
        var region = _dropRegion;

        _dragPanel = null;
        _dropTarget = null;

        if (panel is null || target is null) return;

        Tree.MovePanel(panel, target, region);
        Rebuild();
    }

    public void CancelDrag()
    {
        _dragPanel = null;
        _dropTarget = null;
    }

    public void ClosePanel(DockPanel panel)
    {
        panel.Content.Dispose();
        Tree.ClosePanel(panel);
        Rebuild();
    }

    private static DockRegion ResolveRegion(Vector2 local, Vector2 size)
    {
        var margin = float.Min(float.Min(size.X, size.Y) * 0.3f, 80f);
        if (local.X < margin) return DockRegion.Left;
        if (local.X > size.X - margin) return DockRegion.Right;
        if (local.Y < margin) return DockRegion.Top;
        if (local.Y > size.Y - margin) return DockRegion.Bottom;
        return DockRegion.Center;
    }

    private static (Vector2 offset, Vector2 size) RegionRect(DockRegion region, Vector2 size)
    {
        return region switch
        {
            DockRegion.Left => (Vector2.Zero, size with { X = size.X * 0.5f }),
            DockRegion.Right => (new Vector2(size.X * 0.5f, 0f), size with { X = size.X * 0.5f }),
            DockRegion.Top => (Vector2.Zero, size with { Y = size.Y * 0.5f }),
            DockRegion.Bottom => (new Vector2(0f, size.Y * 0.5f), size with { Y = size.Y * 0.5f }),
            _ => (Vector2.Zero, size)
        };
    }

    // ---- layout / paint ------------------------------------------------------------------

    protected override Vector2 ArrangeContent(in Vector2 availableSpace)
    {
        if (GetChild() is { } child)
        {
            child.Offset = default;
            child.Layout(availableSpace, true);
        }

        return availableSpace;
    }

    public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
    {
        base.Collect(transform, clip, commands);

        if (_dragPanel is not { } panel) return;

        if (_dropTarget is { } target)
        {
            var (offset, size) = RegionRect(_dropRegion, target.GetSize());
            var basis = target.ComputeAbsoluteTransform().Translate(offset);
            commands.AddRect(basis, size, DropFill);
            commands.AddRect(basis, new Vector2(size.X, 2f), DropBorder);
            commands.AddRect(basis, new Vector2(2f, size.Y), DropBorder);
        }

        var chip = new Vector2(float.Max(80f, panel.Title.Length * 8f + 20f), 24f);
        var ghost = Matrix4x4.Identity.Translate(_dragCursor + new Vector2(12f, 10f));
        commands.AddRect(ghost, chip, GhostFill, new Vector4(4f));
        commands.AddText(ghost.Translate(new Vector2(8f, 17f)), "Noto Sans", panel.Title, 13f, Color.White);
    }
}
