using aerox.Runtime.Graphics;
using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Defaults.Content;

public class TextBox : Text
{
    public TextBox(float inSize) : base("", inSize)
    {
        var window = SRuntime.Get().GetModule<SGraphicsModule>().GetMainWindow();
        if (window != null) window.OnChar += e => { Content += e.Data; };
    }

    public override void Draw(WidgetFrame frame, DrawInfo info)
    {
        base.Draw(frame, info);

        if (!ShouldDraw) return;

        var drawInfo = info.AccountFor(this);

        GetContentBounds(out var bounds);
        var cursorPosition = Content.Length > 0 ? Content.Length - 1 : -1;

        Vector2<float> offset = 0.0f;

        if (cursorPosition != -1)
        {
            var targetBounds = bounds[cursorPosition];
            offset.X += targetBounds.Bounds.Right - 2.0f;
        }

        var height = GetDesiredSize().Height;
        var color = ForegroundColor.Clone();
        var sin = (float)((System.Math.Sin(SRuntime.Get().GetTimeSinceCreation() * 5) + 1.0f) / 2.0f);
        color.A *= sin > 0.35 ? 1.0f : 0.0f;
        frame.AddRect(drawInfo.Transform.Translate(offset), new Size2d(2.0f, height),
            color: color);
    }
}