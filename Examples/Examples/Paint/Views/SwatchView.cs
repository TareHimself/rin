using System.Numerics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace Examples.Paint.Views;

/// <summary>
///     A round colour button. The selected one gets a white ring, the hovered one grows a little.
/// </summary>
public sealed class SwatchView(Color color, Func<bool> isSelected, Action onSelected) : ContentView
{
    private const float Diameter = 30f;

    public Color Color => color;

    public override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2(Diameter);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return new Vector2(Diameter);
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        var center = new Vector2(Diameter / 2f);
        var radius = IsHovered ? Diameter / 2f : Diameter / 2f - 3f;
        if (isSelected()) commands.AddCircle(transform, center, radius + 3f, Color.White);
        commands.AddCircle(transform, center, radius, color);
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        if (e.Button != CursorButton.One) return;
        onSelected();
        e.Target = this;
    }
}
