using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Graphics;

namespace Examples.ImageStrip.Views;

public class PrettyView : ContentView
{
    public BorderRadius BorderRadius { get; set; }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return availableSpace;
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        var size = GetContentSize();
        commands.Add(new CustomShaderCommand(transform, size, FocusPoint(transform, size), BorderRadius));
    }

    private Vector2 FocusPoint(in Matrix4x4 transform, Vector2 size)
    {
        var hovered = Parent?.IsHovered ?? false;
        if (!hovered || Surface is not { } surface || !Matrix4x4.Invert(transform, out var toLocal))
            return size / 2f;

        return Vector2.Transform(surface.GetCursorPosition(), toLocal);
    }
}