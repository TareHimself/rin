using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;
using experiments.Docking.Model;

namespace experiments.Docking.Views;

/// <summary>
///     Renders a <see cref="DockSplitNode" />: its child views laid out along one axis with a
///     draggable <see cref="SplitterHandleView" /> between each pair. Built on <see cref="FlexBoxView" />
///     so pane sizing, weight distribution and window-resize reflow come for free.
/// </summary>
public sealed class DockSplitView : FlexBoxView
{
    private const float MinPanePixels = 32f;

    private readonly DockSplitNode _node;
    private readonly List<FlexBoxSlot> _paneSlots = [];

    public DockSplitView(DockSplitNode node, IReadOnlyList<IView> children)
        : base(node.Orientation == DockOrientation.Horizontal ? Axis.Row : Axis.Column)
    {
        _node = node;

        for (var i = 0; i < children.Count; i++)
        {
            if (i > 0)
            {
                var handleIndex = i - 1;
                var handle = new SplitterHandleView(node.Orientation)
                {
                    OnDrag = delta => OnHandleDrag(handleIndex, delta)
                };
                Add(new FlexBoxSlot { Child = handle, Fit = CrossFit.Fill });
            }

            var paneSlot = new FlexBoxSlot
            {
                Child = children[i],
                Fit = CrossFit.Fill,
                Flex = node.Weights.Count > i ? float.Max(node.Weights[i], 0.0001f) : 1f
            };
            _paneSlots.Add(paneSlot);
            Add(paneSlot);
        }
    }

    private void OnHandleDrag(int handleIndex, float deltaPixels)
    {
        if (handleIndex < 0 || handleIndex + 1 >= _paneSlots.Count) return;

        var size = GetContentSize();
        var mainAxisPx = Axis == Axis.Row ? size.X : size.Y;

        // FlexLayout hands each pane  flex_i / sumAllFlex  of the space left after the fixed
        // handle strips. Convert the pixel drag through that same ratio so the handle tracks
        // the cursor 1:1 regardless of how many panes share the split.
        var handlePx = (_paneSlots.Count - 1) * SplitterHandleView.Thickness;
        var usablePx = mainAxisPx - handlePx;
        if (usablePx <= 1f) return;

        var sumAllFlex = 0f;
        foreach (var slot in _paneSlots) sumAllFlex += slot.Flex ?? 1f;

        var a = _paneSlots[handleIndex];
        var b = _paneSlots[handleIndex + 1];
        var flexA = a.Flex ?? 1f;
        var flexB = b.Flex ?? 1f;
        var pairFlex = flexA + flexB;

        var deltaFlex = deltaPixels / usablePx * sumAllFlex;
        var minFlex = MinPanePixels / usablePx * sumAllFlex;
        if (pairFlex <= minFlex * 2f) return; // pair too small to redistribute

        var newA = float.Clamp(flexA + deltaFlex, minFlex, pairFlex - minFlex);
        a.Flex = newA;
        b.Flex = pairFlex - newA;

        WriteBackWeights();
        InvalidateLayout();
    }

    private void WriteBackWeights()
    {
        if (_node.Weights.Count != _paneSlots.Count) return;
        for (var i = 0; i < _paneSlots.Count; i++) _node.Weights[i] = _paneSlots[i].Flex ?? 1f;
        _node.NormalizeWeights();
    }
}
