using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace P2PChat.Views;

public sealed class StatusDot : ContentView
{
    private const float Radius = 5f;

    public Color Color { get; set; } = Ui.Success;

    public override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2(Radius * 2f);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return ComputeDesiredContentSize();
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        commands.AddCircle(transform, Radius, Color);
    }
}
