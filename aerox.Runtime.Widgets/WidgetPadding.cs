using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Containers;

namespace aerox.Runtime.Widgets;

public class WidgetPadding : ICloneable<WidgetPadding>
{
    public float Left = 0.0f;
    public float Top = 0.0f;
    public float Right = 0.0f;
    public float Bottom = 0.0f;

    public static implicit operator Vector4<float>(WidgetPadding p) =>
        new Vector4<float>(p.Left, p.Top, p.Right, p.Bottom);

    public WidgetPadding Clone() => new WidgetPadding()
    {
        Left = Left,
        Right = Right,
        Top = Top,
        Bottom = Bottom
    };

    public static implicit operator WidgetPadding(float val) => new WidgetPadding(val);

    public WidgetPadding()
    {
    }
    
    public WidgetPadding(float val)
    {
        Left = val;
        Right = val;
        Top = val;
        Bottom = val;
    }
    
    public WidgetPadding(float horizontal,float vertical)
    {
        Left = horizontal;
        Right = horizontal;
        Top = vertical;
        Bottom = vertical;
    }
}