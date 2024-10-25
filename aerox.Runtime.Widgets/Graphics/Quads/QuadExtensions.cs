namespace aerox.Runtime.Widgets.Graphics.Quads;

public static class QuadExtensions
{
    public static DrawCommands AddQuads(this DrawCommands drawCommands, params Quad[] quads) =>
        drawCommands.Add(new QuadDrawCommand(quads));
}