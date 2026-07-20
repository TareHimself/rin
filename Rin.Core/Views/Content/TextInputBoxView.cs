using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Extensions;
using Rin.Core.Shared.Math;
using Rin.Core.Views.Graphics.Quads;
using Timer = System.Timers.Timer;

namespace Rin.Core.Views.Content;

public class TextInputBoxView : TextBoxView
{
    private readonly IApplication _application;

    private readonly Timer _typingTimer = new(200)
    {
        AutoReset = false
    };

    public TextInputBoxView(IApplication? application = null)
    {
        _application = application ?? IApplication.Get();
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

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        e.Target = this;
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
        else if (e is { Key: InputKey.Enter, State: InputState.Pressed or InputState.Repeat })
        {
            ResetTypingDelay();
            OnCharacter(new CharacterSurfaceEvent(e.Surface, '\n', 0));
        }
    }

    /// <summary>
    ///     Keeps CursorPosition valid if Content is ever reassigned by something other than this view's own
    ///     typing/backspace handling (which already keep it in sync), so GetCaretOffset never indexes out of
    ///     bounds.
    /// </summary>
    protected override void TextChanged(string newText)
    {
        base.TextChanged(newText);
        CursorPosition = int.Clamp(CursorPosition, -1, newText.Length - 1);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        // Fill available space like an input field should, but never collapse: an unbounded
        // axis (e.g. the main axis of a Column-oriented ListView, which is always handed down
        // as +Infinity) must fall back to the measured content size instead of propagating
        // Infinity, since View.Layout() zeroes out non-finite components via FiniteOr(). Height
        // additionally never drops below LineHeight, so an empty box still shows a caret-sized
        // line - the same default an HTML <input> has.
        var measured = base.LayoutContent(availableSpace);
        var width = float.IsFinite(availableSpace.X) ? availableSpace.X : measured.X;
        var height = float.Max(LineHeight, float.IsFinite(availableSpace.Y) ? availableSpace.Y : measured.Y);
        return new Vector2(width, height);
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        base.CollectContent(transform, commands);

        if (!FontReady || !IsFocused) return;

        var offset = GetCaretOffset();
        offset.X -= 2.0f;

        var height = LineHeight;
        var color = ForegroundColor;
        var sin = (float.Sin(_application.TimeSeconds * 5) + 1.0f) / 2.0f;
        color.A *= IsTyping ? 1.0f : sin > 0.35 ? 1.0f : 0.0f;
        commands.AddRect(transform.Translate(offset), new Vector2(2.0f, height), color);
    }

    /// <summary>
    ///     Pen position (top-of-line Y, advance-based X - not glyph ink, which is unsuitable here since a
    ///     space/NBSP has none) immediately after the character at <see cref="CursorPosition" />, i.e. where the
    ///     caret belongs. A caret sitting right after a line break instead jumps to the start of the next line,
    ///     matching how every other text editor positions it.
    /// </summary>
    private Vector2 GetCaretOffset()
    {
        if (CursorPosition == -1) return Vector2.Zero;

        var bounds = GetCharacterBounds(Wrap);
        if (CursorPosition >= bounds.Length) return Vector2.Zero;

        var target = bounds[CursorPosition];

        if (Content[CursorPosition] == '\n')
            return CursorPosition + 1 < bounds.Length
                ? new Vector2(bounds[CursorPosition + 1].PenX, bounds[CursorPosition + 1].LineTop)
                : new Vector2(0f, target.LineTop + target.LineHeight);

        return new Vector2(target.PenX + target.Advance, target.LineTop);
    }
}