using rin.Core;
using rin.Core.Extensions;
using rin.Graphics;
using rin.Core.Math;
using rin.Widgets.Events;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Quads;
using rin.Windows;
using Timer = System.Timers.Timer;

namespace rin.Widgets.Content;

public class TextInputBox : TextBox
{
    public override bool IsFocusable => true;

    public int CursorPosition;

    public bool IsTyping = false;

    private readonly Timer _typingTimer = new Timer(200)
    {
        AutoReset = false,
    };
    
    public TextInputBox(string data = "", int fontSize = 100, string fontFamily = "Arial") : base(data, fontSize, fontFamily)
    {
        CursorPosition = data.Length - 1;
        _typingTimer.Elapsed += (_, __) => IsTyping = false;
    }


    protected void ResetTypingDelay()
    {
        IsTyping = true;
        _typingTimer.Stop();
        _typingTimer.Start();
    }
    protected override bool OnCursorDown(CursorDownEvent e)
    {
        return true;
    }

    public override void OnCharacter(CharacterEvent e)
    {
        base.OnCharacter(e);
        ResetTypingDelay();
        if (Content.Empty())
        {
            Content += e.Character;
        }
        else
        {
            Content = Content[..(CursorPosition + 1)] + e.Character + Content[(CursorPosition + 1)..];
        }
        CursorPosition++;
    }

    public override void OnKeyboard(KeyboardEvent e)
    {
        base.OnKeyboard(e);
        if (e is { Key: Key.Backspace, State: KeyState.Pressed or KeyState.Repeat })
        {
            if (CursorPosition > -1)
            {
                ResetTypingDelay();
                Content = Content.Remove(CursorPosition,1);
                CursorPosition--;
            }
        }
        else if (e is { Key: Key.Left or Key.Right, State: KeyState.Pressed or KeyState.Repeat })
        {
            if (Content.NotEmpty())
            {
                ResetTypingDelay();
                var delta = e.Key == Key.Left ? -1 : 1;
                CursorPosition = Math.Clamp(CursorPosition + delta, -1, Content.Length - 1);
            }
        }
        else if (e is { Key: Key.Left or Key.Enter, State: KeyState.Pressed or KeyState.Repeat })
        {
            ResetTypingDelay();
            OnCharacter(new CharacterEvent(e.Surface,'\n',0));
        }
    }

    protected override void TextChanged(string newText)
    {
        base.TextChanged(newText);
        //CursorPosition = Math.Clamp(CursorPosition, -1, Content.Length - 1);
    }

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        base.CollectContent(info,drawCommands);

        if (!FontReady || !IsFocused) return;
        
        Vector2<float> offset = 0.0f;

        if (CursorPosition != -1)
        {
            
            var targetBounds = GetCharacterBounds().ToArray()[CursorPosition];
            offset.X += targetBounds.Right - 2.0f;
            offset.Y = LineHeight * (float)Math.Floor(((targetBounds.Y + (targetBounds.Height / 2.0f)) / LineHeight));
            // if (Content[CursorPosition] == '\n')
            // {
            //     offset.Y = (Content.Split("\n").Length - 1) * LineHeight;
            // }
            // else
            // {
            //     var targetBounds = GetContentBounds().ToArray()[CursorPosition];
            //     offset.X += targetBounds.Right - 2.0f;
            //     offset.Y = (float)Math.Floor(((targetBounds.Y + (targetBounds.Height / 2.0f)) / LineHeight));
            // }
        }

        var height = LineHeight;
        var color = ForegroundColor.Clone();
        var sin = (float)((System.Math.Sin(SRuntime.Get().GetTimeSeconds() * 5) + 1.0f) / 2.0f);
        color.A *= IsTyping ? 1.0f : sin > 0.35 ? 1.0f : 0.0f;
        drawCommands.AddRect(info.Transform.Translate(offset), new Vector2<float>(2.0f, height), color: color);
        // frame.AddRect(drawInfo.Transform.Translate(offset), new Vector2<float>(2.0f, height),
        //     tint: tint);
    }
}