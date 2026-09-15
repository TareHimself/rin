using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;
using experiments.Docking.Model;

namespace experiments.Docking.Views;

/// <summary>
///     Renders a <see cref="DockTabGroupNode" />: a tab strip on top, the active panel's
///     content filling the rest.
/// </summary>
public sealed class DockTabGroupView : FlexBoxView
{
    private static readonly Color StripColor = new(0.09f, 0.09f, 0.10f, 1f);
    private static readonly Color PanelColor = new(0.13f, 0.13f, 0.15f, 1f);

    private readonly FillSwitcherView _content = new();
    private readonly ListView _tabs = new(Axis.Row);

    public DockTabGroupView(DockTabGroupNode node, DockSpaceView space) : base(Axis.Column)
    {
        Node = node;
        Space = space;

        for (var i = 0; i < node.Panels.Count; i++)
            _tabs.Add(new DockTabView(this, node.Panels[i], i));

        foreach (var panel in node.Panels) _content.Add(panel.Content);
        _content.SelectedIndex = node.ActiveIndex;

        Add(new FlexBoxSlot
        {
            Child = new RectView { Color = StripColor, InitChild = _tabs },
            Fit = CrossFit.Fill
        });
        Add(new FlexBoxSlot
        {
            Child = new RectView { Color = PanelColor, InitChild = _content },
            Flex = 1f,
            Fit = CrossFit.Fill
        });
    }

    public DockTabGroupNode Node { get; }
    public DockSpaceView Space { get; }

    public void SetActive(int index)
    {
        Node.ActiveIndex = index;
        _content.SelectedIndex = Node.ActiveIndex;
    }
}
