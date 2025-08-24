using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Extensions;
using Rin.Framework.Math;
using Rin.Framework.Views.Graphics.Quads;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;
using Timer = System.Timers.Timer;

namespace Rin.Framework.Views.Content;

public class TextInputBox : TextBox
{
    private readonly Timer _typingTimer = new(200)
    {
        AutoReset = false
    };

    public TextInputBox()
    {
        CursorPosition = Content.Length - 1;
        _typingTimer.Elapsed += (_, _) => IsTyping = false;
    }

    public override bool IsFocusable => true;

    [PublicAPI] public int CursorPosition { get; private set; }

    [PublicAPI] public bool IsTyping { get; private set; }


    private void ResetTypingDelay()
    {
        IsTyping = true;
        _typingTimer.Stop();
        _typingTimer.Start();
    }

    public override bool OnCursorDown(CursorDownSurfaceEvent e)
    {
        return true;
    }

    public override void OnCharacter(CharacterSurfaceEvent e)
    {
        base.OnCharacter(e);
        ResetTypingDelay();
        if (Content.Empty())
            Content += e.Character;
        else
            Content = Content[..(CursorPosition + 1)] + e.Character + Content[(CursorPosition + 1)..];
        CursorPosition++;
    }

    public override void OnFocus()
    {
        base.OnFocus();
        Surface?.StartTyping(this);
    }

    public override void OnFocusLost()
    {
        base.OnFocusLost();
        Surface?.StopTyping(this);
    }

    public override void OnKeyboard(KeyboardSurfaceEvent e)
    {
        base.OnKeyboard(e);
        if (e is { Key: InputKey.Backspace, State: InputState.Pressed or InputState.Repeat })
        {
            if (CursorPosition > -1)
            {
                ResetTypingDelay();
                Content = Content.Remove(CursorPosition, 1);
                CursorPosition--;
            }
        }
        else if (e is { Key: InputKey.Left or InputKey.Right, State: InputState.Pressed or InputState.Repeat })
        {
            if (Content.NotEmpty())
            {
                ResetTypingDelay();
                var delta = e.Key == InputKey.Left ? -1 : 1;
                CursorPosition = int.Clamp(CursorPosition + delta, -1, Content.Length - 1);
            }
        }
        else if (e is { Key: InputKey.Left or InputKey.Enter, State: InputState.Pressed or InputState.Repeat })
        {
            ResetTypingDelay();
            OnCharacter(new CharacterSurfaceEvent(e.Surface, '\n', 0));
        }
    }

    protected override void TextChanged(string newText)
    {
        base.TextChanged(newText);
        //CursorPosition = float.Clamp(CursorPosition, -1, Content.Length - 1);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        base.LayoutContent(availableSpace);
        return availableSpace;
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        base.CollectContent(transform, commands);

        if (!FontReady || !IsFocused) return;

        Vector2 offset = default;

        if (CursorPosition != -1)
        {
            var targetBounds = GetCharacterBounds(Wrap).ToArray()[CursorPosition];
            offset.X += targetBounds.Right - 2.0f;
            //offset.Y = LineHeight * (float)Math.Floor((targetBounds.Y + targetBounds.Height / 2.0f) / LineHeight);
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
        var color = ForegroundColor;
        var sin = (float.Sin(SApplication.Get().GetTimeSeconds() * 5) + 1.0f) / 2.0f;
        color.A *= IsTyping ? 1.0f : sin > 0.35 ? 1.0f : 0.0f;
        commands.AddRect(transform.Translate(offset), new Vector2(2.0f, height), color);
    }
}