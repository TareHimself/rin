using aerox.Runtime.Graphics;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Events;
using aerox.Runtime.Windows;

namespace aerox.Runtime.Widgets.Defaults.Content;

public class TextBox : Text
{
    public TextBox(string data = "",int fontSize = 100,string fontFamily = "Arial") : base(data,fontSize,fontFamily)
    {
    }

    public virtual void OnCharacter(Window.CharEvent data) => OnCharacter(data.Data);
    
    public virtual void OnCharacter(char data)
    {
        Content += data;
    }

    protected override void OnAddedToRoot(WidgetSurface widgetSurface)
    {
        base.OnAddedToRoot(widgetSurface);
        if (widgetSurface is WidgetWindowSurface windowSurface)
        {
            windowSurface.Window.OnChar += OnCharacter;
        }
    }

    protected override bool OnCursorDown(CursorDownEvent e)
    {
        return e.Surface.RequestFocus(this);
    }

    protected override void OnRemovedFromRoot(WidgetSurface widgetSurface)
    {
        base.OnRemovedFromRoot(widgetSurface);
        if (widgetSurface is WidgetWindowSurface windowSurface)
        {
            windowSurface.Window.OnChar -= OnCharacter;
        }
    }

    public override void Collect(WidgetFrame frame, DrawInfo info)
    {
        base.Collect(frame, info);

        if (!ShouldDraw || !Focused) return;

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