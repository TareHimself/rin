using rin.Runtime.Graphics;
using rin.Scene.Graphics.Commands;

namespace rin.Scene.Graphics;

public struct SceneFrame
{
    public readonly SceneDrawer Drawer;
    public Frame Original;
    public readonly List<Command> DrawCommands = new();

    public SceneFrame(SceneDrawer drawer, Frame original)
    {
        Drawer = drawer;
        Original = original;
        original.OnReset += CleanupCommands;
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
        return frame.Original;
    }
}