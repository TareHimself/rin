using Examples.Common.Views;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;

namespace Examples.AudioPlayer.Views;

/// <summary>
///     A row of pill buttons that picks which card the right column shows.
/// </summary>
public sealed class PaneTabsView : FlexBoxView
{
    private readonly List<ActionButton> _tabs = [];

    public PaneTabsView(IReadOnlyList<string> labels, float sidePadding = 16f)
    {
        Axis = Axis.Row;
        for (var i = 0; i < labels.Count; i++)
        {
            var index = i;
            var tab = new ActionButton(labels[i], Ui.Clear, Ui.Raised, Ui.Surface, Ui.Muted, 14f)
            {
                BorderRadius = BorderRadius.All(16f),
                Padding = new Padding(sidePadding, 7f)
            };
            tab.Clicked += () => Select(index);
            _tabs.Add(tab);
            if (i > 0) Add(Ui.Slot(Ui.Gap(0f, 6f)));
            Add(Ui.Slot(tab, fit: CrossFit.Desired, align: CrossAlign.Center));
        }

        Select(0);
    }

    public int Selected { get; private set; } = -1;

    public event Action<int>? Changed;

    public void Select(int index)
    {
        if (index == Selected) return;

        Selected = index;
        for (var i = 0; i < _tabs.Count; i++)
        {
            if (i == index)
                _tabs[i].Restyle(PlayerView.Glass, Ui.Raised, Ui.Surface, Ui.Text);
            else
                _tabs[i].Restyle(Ui.Clear, Ui.Raised, Ui.Surface, Ui.Muted);
        }

        Changed?.Invoke(index);
    }
}
