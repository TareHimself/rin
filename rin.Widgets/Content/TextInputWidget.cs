using rin.Core;
using rin.Graphics;
using rin.Core.Math;
using rin.Widgets.Events;
using rin.Widgets.Graphics;
using rin.Windows;

namespace rin.Widgets.Content;

public class TextInputWidget : TextWidget
{
    public TextInputWidget(string data = "",int fontSize = 100,string fontFamily = "Arial") : base(data,fontSize,fontFamily)
    {
    }

    public virtual void OnCharacter(Window.CharEvent data) => OnCharacter(data.Data);
    
    public virtual void OnCharacter(char data)
    {
        Content += data;
    }

    protected override void OnAddedToSurface(Surface surface)
    {
        base.OnAddedToSurface(surface);
        if (surface is WindowSurface windowSurface)
        {
            windowSurface.Window.OnChar += OnCharacter;
        }
    }

    protected override bool OnCursorDown(CursorDownEvent e)
    {
        return e.Surface.RequestFocus(this);
    }

    protected override void OnRemovedFromSurface(Surface surface)
    {
        base.OnRemovedFromSurface(surface);
        if (surface is WindowSurface windowSurface)
        {
            windowSurface.Window.OnChar -= OnCharacter;
        }
    }

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        base.CollectContent(info,drawCommands);

        if (!ShouldDraw || !IsFocused) return;
        
        GetContentBounds(out var bounds);
        var cursorPosition = Content.Length > 0 ? Content.Length - 1 : -1;

        Vector2<float> offset = 0.0f;

        if (cursorPosition != -1)
        {
            var targetBounds = bounds[cursorPosition];
            offset.X += targetBounds.Bounds.Right - 2.0f;
        }

        var height = GetDesiredSize().X;
        var color = ForegroundColor.Clone();
        var sin = (float)((System.Math.Sin(SRuntime.Get().GetTimeSeconds() * 5) + 1.0f) / 2.0f);
        color.A *= sin > 0.35 ? 1.0f : 0.0f;
        // frame.AddRect(drawInfo.Transform.Translate(offset), new Vector2<float>(2.0f, height),
        //     color: color);
    }
}