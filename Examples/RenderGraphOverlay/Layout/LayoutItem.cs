namespace RenderGraphOverlay.Layout;

internal sealed class LayoutItem
{
    public required int Row { get; init; }
    public required float Width { get; init; }
    public uint PassId { get; init; }
    public bool IsDummy { get; init; }
    public float X { get; set; }
    public int Order { get; set; }
    public List<LayoutItem> Up { get; } = [];
    public List<LayoutItem> Down { get; } = [];

    public float CenterX => X + Width / 2f;
}
