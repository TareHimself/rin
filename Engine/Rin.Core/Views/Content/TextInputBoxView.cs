using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Events;
using Rin.Core.Views.Font;
using Rin.Core.Views.Graphics;
using Rin.Core.Extensions;
using Rin.Core.Shared.Math;
using Rin.Core.Views.Graphics.Quads;
using Timer = System.Timers.Timer;

namespace Rin.Core.Views.Content;

public class TextInputBoxView : TextBoxView
{
    private const float CaretWidth = 2f;

    private readonly IApplication _application;
    private Vector2 _scroll;
    private Vector2 _shownCaret;
    private bool _editing;
    private int _preeditStart;
    private int _preeditLength;
    private Rect2D? _typingRect;

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

    /// <summary>
    ///     Whether Escape gives up focus. Turn off for a field that uses Escape for something else.
    /// </summary>
    [PublicAPI] public bool UnfocusOnEscape { get; set; } = true;


    private void ResetTypingDelay()
    {
        IsTyping = true;
        _typingTimer.Stop();
        _typingTimer.Start();
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        e.Target = this;
        CancelComposition();
        if (!FontReady) return;

        var point = e.Position.Transform(ComputeAbsoluteContentTransform().Inverse()) + _scroll;
        ResetTypingDelay();
        CursorPosition = TextCaretLayout.IndexAt(GetContentBounds(), point);
    }

    public override void OnCharacter(CharacterSurfaceEvent e)
    {
        base.OnCharacter(e);
        ClearPreedit();
        ResetTypingDelay();
        var insertAt = int.Clamp(CursorPosition + 1, 0, Content.Length);
        Edit(Content.Insert(insertAt, e.Character.ToString()), insertAt);
    }

    /// <summary>
    ///     Shows the text an input method is composing in place, at the caret, until it is committed.
    /// </summary>
    public override void OnTextPreedit(TextPreeditSurfaceEvent e)
    {
        base.OnTextPreedit(e);
        ResetTypingDelay();
        var start = _preeditLength > 0 ? _preeditStart : int.Clamp(CursorPosition + 1, 0, Content.Length);
        var content = Content.Remove(start, _preeditLength).Insert(start, e.Text);
        _preeditStart = start;
        _preeditLength = e.Text.Length;
        Edit(content, start + int.Clamp(e.CaretStart, 0, e.Text.Length) - 1);
    }

    private void ClearPreedit()
    {
        if (_preeditLength == 0) return;
        var length = _preeditLength;
        _preeditLength = 0;
        Edit(Content.Remove(_preeditStart, length), _preeditStart - 1);
    }

    /// <summary>
    ///     Drops the text being composed, here and in the input method, by restarting text input.
    /// </summary>
    private void CancelComposition()
    {
        if (_preeditLength == 0) return;
        ClearPreedit();
        _typingRect = null;
        Surface?.StopTyping(this);
        Surface?.StartTyping(this);
    }

    public override void OnFocus()
    {
        base.OnFocus();
        _typingRect = null;
        Surface?.StartTyping(this);
    }

    public override void OnFocusLost()
    {
        base.OnFocusLost();
        ClearPreedit();
        Surface?.StopTyping(this);
    }

    public override void OnKeyboard(KeyboardSurfaceEvent e)
    {
        base.OnKeyboard(e);
        if (e is { Key: InputKey.Escape, State: InputState.Pressed })
        {
            // While composing, Escape belongs to the input method, which cancels the composition.
            if (UnfocusOnEscape && _preeditLength == 0) e.Surface.ClearFocus();
        }
        else if (e is { Key: InputKey.Backspace, State: InputState.Pressed or InputState.Repeat })
        {
            if (CursorPosition > -1 && CursorPosition < Content.Length)
            {
                ResetTypingDelay();
                Edit(Content.Remove(CursorPosition, 1), CursorPosition - 1);
            }
        }
        else if (e is { Key: InputKey.Delete, State: InputState.Pressed or InputState.Repeat })
        {
            if (CursorPosition + 1 < Content.Length)
            {
                ResetTypingDelay();
                Edit(Content.Remove(CursorPosition + 1, 1), CursorPosition);
            }
        }
        else if (e is { Key: InputKey.Home or InputKey.End, State: InputState.Pressed or InputState.Repeat })
        {
            ResetTypingDelay();
            CursorPosition = e.Key == InputKey.Home ? -1 : Content.Length - 1;
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
    ///     Content assigned from outside moves the caret to the end; this view's own edits set it themselves.
    /// </summary>
    protected override void TextChanged(string newText)
    {
        base.TextChanged(newText);
        if (_editing) return;
        CursorPosition = newText.Length - 1;
        _preeditLength = 0;
    }

    private void Edit(string newContent, int newCursorPosition)
    {
        _editing = true;
        try
        {
            Content = newContent;
        }
        finally
        {
            _editing = false;
        }

        CursorPosition = int.Clamp(newCursorPosition, -1, newContent.Length - 1);
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
        UpdateScroll();
        // The clip leaves room for a caret sitting before the first character.
        commands.PushClip(transform.Translate(new Vector2(-CaretWidth, 0f)),
            GetContentSize() + new Vector2(CaretWidth, 0f));
        CollectText(transform.Translate(-_scroll), commands);
        commands.PopClip();
    }

    /// <summary>
    ///     Follows the caret when it moves, and otherwise only keeps the scroll inside the text.
    /// </summary>
    private void UpdateScroll()
    {
        var caret = GetCaretOffset();
        var caretMoved = caret != _shownCaret;
        _shownCaret = caret;
        var view = GetContentSize();
        var text = TextCaretLayout.Size(GetContentBounds());
        _scroll = new Vector2(
            WrapContent ? 0f :
            caretMoved ? TextCaretLayout.ScrollToShow(_scroll.X, caret.X, caret.X, view.X, text.X) :
            TextCaretLayout.ClampScroll(_scroll.X, view.X, text.X),
            caretMoved
                ? TextCaretLayout.ScrollToShow(_scroll.Y, caret.Y, caret.Y + LineHeight, view.Y, text.Y)
                : TextCaretLayout.ClampScroll(_scroll.Y, view.Y, text.Y));
    }

    protected override bool OnScroll(ScrollSurfaceEvent e)
    {
        var view = GetContentSize();
        var text = TextCaretLayout.Size(GetContentBounds());
        var scroll = new Vector2(
            WrapContent ? 0f : TextCaretLayout.ClampScroll(_scroll.X + e.Delta.X, view.X, text.X),
            TextCaretLayout.ClampScroll(_scroll.Y - e.Delta.Y, view.Y, text.Y));
        if (scroll == _scroll) return base.OnScroll(e);

        _scroll = scroll;
        return true;
    }

    private void CollectText(in Matrix4x4 transform, CommandList commands)
    {
        base.CollectContent(transform, commands);

        if (!FontReady || !IsFocused) return;

        var offset = GetCaretOffset();
        offset.X -= CaretWidth;

        var height = LineHeight;
        var color = ForegroundColor;
        UpdateTypingRect(new Rect2D(offset, new Vector2(CaretWidth, height), transform));
        CollectPreeditUnderline(transform, commands);
        var sin = (float.Sin(_application.TimeSeconds * 5) + 1.0f) / 2.0f;
        color.A *= IsTyping ? 1.0f : sin > 0.35 ? 1.0f : 0.0f;
        commands.AddRect(transform.Translate(offset), new Vector2(CaretWidth, height), color);
    }

    private void UpdateTypingRect(in Rect2D caret)
    {
        if (_typingRect == caret) return;
        _typingRect = caret;
        Surface?.SetTypingRect(this, caret);
    }

    private void CollectPreeditUnderline(in Matrix4x4 transform, CommandList commands)
    {
        if (_preeditLength == 0) return;
        var bounds = GetContentBounds();
        var end = int.Min(_preeditStart + _preeditLength, bounds.Length);
        for (var i = _preeditStart; i < end; i++)
        {
            var bound = bounds[i];
            var underline = new Vector2(bound.PenX, bound.LineTop + bound.LineHeight - 1.0f);
            commands.AddRect(transform.Translate(underline), new Vector2(bound.Advance, 1.0f), ForegroundColor);
        }
    }

    private GlyphRect[] GetContentBounds()
    {
        var bounds = GetCharacterBounds(Wrap);
        return bounds.Length == Content.Length ? bounds : GetCharacterBounds(Wrap, false);
    }

    /// <summary>
    ///     Pen position (top-of-line Y, advance-based X - not glyph ink, which is unsuitable here since a
    ///     space/NBSP has none) immediately after the character at <see cref="CursorPosition" />, i.e. where the
    ///     caret belongs. A caret sitting right after a line break instead jumps to the start of the next line,
    ///     matching how every other text editor positions it.
    /// </summary>
    private Vector2 GetCaretOffset()
    {
        var content = Content;
        var cursor = CursorPosition;
        if (cursor < 0 || cursor >= content.Length) return Vector2.Zero;

        var bounds = GetContentBounds();
        if (cursor >= bounds.Length) return Vector2.Zero;

        var target = bounds[cursor];

        if (content[cursor] == '\n')
            return cursor + 1 < bounds.Length
                ? new Vector2(bounds[cursor + 1].PenX, bounds[cursor + 1].LineTop)
                : new Vector2(0f, target.LineTop + target.LineHeight);

        return new Vector2(target.PenX + target.Advance, target.LineTop);
    }
}