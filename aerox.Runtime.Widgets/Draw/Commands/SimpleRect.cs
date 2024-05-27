using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Draw.Commands;

public class SimpleRect(Matrix3 transform, Vector2<float> size) : Command
{
    public Vector4<float>? BorderRadius;
    public Color? Color;

    public override void Bind(WidgetFrame frame)
    {
        frame.WidgetSurface.SimpleRectMat.BindTo(frame);
    }

    public override void Run(WidgetFrame frame)
    {
        var constants = new SimpleRectPush
        {
            Transform = transform,
            Size = size,
            BorderRadius = BorderRadius ?? new Vector4<float>(1.0f),
            Color = Color ?? Color.White
        };
        frame.WidgetSurface.SimpleRectMat.Push(frame.Raw.GetCommandBuffer(), "pRect", constants);
        CmdDrawQuad(frame);
    }
}