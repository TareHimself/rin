using aerox.Runtime.Graphics;
using aerox.Runtime.Scene.Graphics.Commands;

namespace aerox.Runtime.Scene.Graphics;

public struct SceneFrame
{
    public readonly SceneDrawer Drawer;
    public Frame Raw;
    public readonly List<Command> DrawCommands = new();

    public SceneFrame(SceneDrawer drawer, Frame raw)
    {
        Drawer = drawer;
        Raw = raw;
        raw.OnDrawn += CleanupCommands;
    }

    public SceneFrame AddCommand(Command command)
    {
        DrawCommands.Add(command);
        return this;
    }
    public SceneFrame AddCommand(params Command[] commands)
    {
        DrawCommands.AddRange(commands);
        return this;
    }

    public void CleanupCommands(Frame frame)
    {
        foreach (var widgetFrameDrawCommand in DrawCommands) widgetFrameDrawCommand.Dispose();
        DrawCommands.Clear();
    }


    public static implicit operator Frame(SceneFrame frame)
    {
        return frame.Raw;
    }
}