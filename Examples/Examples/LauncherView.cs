using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples;

public sealed class LauncherView : PanelView
{
    private const int Columns = 2;
    private static readonly Color Background = new(0.11f, 0.12f, 0.15f, 1f);

    public LauncherView(IReadOnlyList<Example> examples, Action<Example> onPicked)
    {
        var list = new ListView { Axis = Axis.Column };
        list.Add(new ListSlot
        {
            Child = new TextBoxView { Content = "Rin examples", FontSize = 28f, Padding = new Padding(0f, 14f) },
            Align = CrossAlign.Center
        });
        list.Add(new ListSlot { Child = MakeColumns(examples, onPicked), Align = CrossAlign.Center });

        Add(new PanelSlot
        {
            Child = new RectView { Color = Background },
            MinAnchor = Vector2.Zero,
            MaxAnchor = Vector2.One
        });
        Add(new PanelSlot
        {
            Child = list,
            MinAnchor = new Vector2(0.5f),
            MaxAnchor = new Vector2(0.5f),
            Alignment = new Vector2(0.5f),
            SizeToContent = true
        });
    }

    private static ListView MakeColumns(IReadOnlyList<Example> examples, Action<Example> onPicked)
    {
        var rows = (examples.Count + Columns - 1) / Columns;
        var columns = new ListView { Axis = Axis.Row };
        for (var first = 0; first < examples.Count; first += rows)
        {
            var column = new ListView { Axis = Axis.Column, Padding = new Padding(8f, 0f) };
            for (var i = first; i < int.Min(first + rows, examples.Count); i++)
                column.Add(MakeButton(examples[i], onPicked));
            columns.Add(column);
        }

        return columns;
    }

    private static IView MakeButton(Example example, Action<Example> onPicked)
    {
        return new SizerView
        {
            Padding = new Padding(0f, 5f),
            InitChild = new LauncherButton(example.Title, () => onPicked(example))
        };
    }
}
