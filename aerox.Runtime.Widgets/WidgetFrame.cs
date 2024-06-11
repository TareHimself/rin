using System.Runtime.InteropServices;
using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Draw.Commands;

namespace aerox.Runtime.Widgets;

[StructLayout(LayoutKind.Sequential)]
public struct SimpleRectPush
{
    public Matrix3 Transform;

    public Vector2<float> Size;

    public Vector4<float> BorderRadius;

    public Vector4<float> Color;
}

public class WidgetFrame
{
    public readonly List<Command> DrawCommands = [];
    public readonly Frame Raw;
    public readonly WidgetSurface Surface;
    public bool IsMainPassActive = false;

    public WidgetFrame(WidgetSurface surface, Frame raw)
    {
        Surface = surface;
        Raw = raw;
        raw.OnDrawn += CleanupCommands;
    }

    public WidgetFrame AddRect(Matrix3 transform, Vector2<float> size, Vector4<float>? borderRadius = null,
        Color? color = null)
    {
        return AddCommand(new SimpleRect(transform, size)
        {
            BorderRadius = borderRadius,
            Color = color
        });
    }

    public WidgetFrame AddMaterialRect(MaterialInstance materialInstance, WidgetPushConstants pushConstants)
    {
        return AddCommand(new MaterialRect(materialInstance, pushConstants));
    }

    public WidgetFrame AddCommand(Command command)
    {
        DrawCommands.Add(command);
        return this;
    }

    public WidgetFrame AddCommands(params Command[] commands)
    {
        DrawCommands.AddRange(commands);
        return this;
    }

    public void CleanupCommands(Frame frame)
    {
        foreach (var widgetFrameDrawCommand in DrawCommands) widgetFrameDrawCommand.Dispose();
        DrawCommands.Clear();
    }


    public static implicit operator Frame(WidgetFrame frame)
    {
        return frame.Raw;
    }
}