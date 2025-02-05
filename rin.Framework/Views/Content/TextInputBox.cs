using System.Numerics;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Graphics.Windows;
using rin.Framework.Core.Extensions;
using rin.Framework.Graphics;
using rin.Framework.Views.Events;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Quads;

using Timer = System.Timers.Timer;

namespace rin.Framework.Views.Content;

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
    public override bool OnCursorDown(CursorDownEvent e)
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
        if (e is { Key: InputKey.Backspace, State: InputState.Pressed or InputState.Repeat })
        {
            if (CursorPosition > -1)
            {
                ResetTypingDelay();
                Content = Content.Remove(CursorPosition,1);
                CursorPosition--;
            }
        }
        else if (e is { Key: InputKey.Left or InputKey.Right, State: InputState.Pressed or InputState.Repeat })
        {
            if (Content.NotEmpty())
            {
                ResetTypingDelay();
                var delta = e.Key == InputKey.Left ? -1 : 1;
                CursorPosition = Math.Clamp(CursorPosition + delta, -1, Content.Length - 1);
            }
        }
        else if (e is { Key: InputKey.Left or InputKey.Enter, State: InputState.Pressed or InputState.Repeat })
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

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        base.CollectContent(transform,commands);
        
        if (!FontReady || !IsFocused) return;
                
        Vector2 offset = default;

        if (CursorPosition != -1)
        {
            
            var targetBounds = GetCharacterBounds(Wrap).ToArray()[CursorPosition];
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
        commands.AddRect(transform.Translate(offset), new Vector2(2.0f, height), color: color);
    }
}