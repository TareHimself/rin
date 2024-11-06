using rin.Core;
using rin.Graphics;
using rin.Core.Math;
using rin.Widgets.Events;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Quads;
using rin.Windows;

namespace rin.Widgets.Content;

public class TextInputBox : TextBox
{
    public override bool IsFocusable => true;

    public TextInputBox(string data = "",int fontSize = 100,string fontFamily = "Arial") : base(data,fontSize,fontFamily)
    {
    }
    
    
    protected override bool OnCursorDown(CursorDownEvent e)
    {
        return true;
    }

    public override void OnCharacter(CharacterEvent e)
    {
        base.OnCharacter(e);
        Content += e.Character;
    }

    public override void OnKeyboard(KeyboardEvent e)
    {
        base.OnKeyboard(e);
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

        var height = LineHeight;
        var color = ForegroundColor.Clone();
        var sin = (float)((System.Math.Sin(SRuntime.Get().GetTimeSeconds() * 5) + 1.0f) / 2.0f);
        color.A *= sin > 0.35 ? 1.0f : 0.0f;
        drawCommands.AddRect(new Vector2<float>(2.0f, height), info.Transform.Translate(offset), color: color);
        // frame.AddRect(drawInfo.Transform.Translate(offset), new Vector2<float>(2.0f, height),
        //     color: color);
    }
}