using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples;

public sealed class LauncherView : PanelView
{
    private static readonly Color Background = new(0.11f, 0.12f, 0.15f, 1f);
    private static readonly Color ButtonColor = new(0.2f, 0.24f, 0.33f, 1f);

    public LauncherView(IReadOnlyList<Example> examples, Action<Example> onPicked)
    {
        var list = new ListView { Axis = Axis.Column };
        list.Add(new TextBoxView { Content = "Rin examples", FontSize = 28f, Padding = new Padding(6f, 14f) });
        foreach (var example in examples)
            list.Add(new ListSlot { Child = MakeButton(example, onPicked), Fit = CrossFit.Fill });

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

    private static IView MakeButton(Example example, Action<Example> onPicked)
    {
        var button = new ButtonView
        {
            Color = ButtonColor,
            BorderRadius = new Vector4(10f),
            Padding = new Padding(20f, 12f),
            InitChild = new TextBoxView { Content = example.Title, FontSize = 18f }
        };
        button.OnReleased += (_, _) => onPicked(example);

        return new SizerView { Padding = new Padding(0f, 5f), InitChild = button };
    }
}
