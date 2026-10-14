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

/// <summary>
///     An editable <see cref="TextBoxView" /> with a caret and a selection. Caret and selection indices are the
///     character the caret sits after, -1 for the start, as in <see cref="TextCaretLayout" />.
/// </summary>
public class TextInputBoxView : TextBoxView
{
    private const float CaretWidth = 2f;

    private readonly IApplication _application;
    private readonly List<Rect2D> _selectionRects = [];
    private Vector2 _scroll;
    private Vector2 _shownCaret;
    private bool _editing;
    private bool _dragging;
    private int? _anchor;
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

    [PublicAPI] public Color SelectionColor { get; set; } = new(0.35f, 0.55f, 1f, 0.45f);

    /// <summary>
    ///     Whether the selection covers at least one character. The caret is always one end of it.
    /// </summary>
    [PublicAPI] public bool HasSelection => _anchor is { } anchor && anchor != CursorPosition;

    [PublicAPI] public int SelectionStart => int.Min(_anchor ?? CursorPosition, CursorPosition);

    [PublicAPI] public int SelectionEnd => int.Max(_anchor ?? CursorPosition, CursorPosition);

    [PublicAPI]
    public string SelectedText
    {
        get
        {
            if (!HasSelection) return string.Empty;
            var first = int.Clamp(SelectionStart + 1, 0, Content.Length);
            var last = int.Clamp(SelectionEnd + 1, first, Content.Length);
            return Content[first..last];
        }
    }

    /// <summary>
    ///     Selects from one caret index to another and puts the caret at the second.
    /// </summary>
    [PublicAPI]
    public void Select(int anchor, int caret)
    {
        _anchor = int.Clamp(anchor, -1, Content.Length - 1);
        CursorPosition = int.Clamp(caret, -1, Content.Length - 1);
    }

    [PublicAPI]
    public void SelectAll()
    {
        Select(-1, Content.Length - 1);
    }

    [PublicAPI]
    public void ClearSelection()
    {
        _anchor = null;
    }

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

        ResetTypingDelay();
        CursorPosition = IndexAt(e.Position);
        _anchor = CursorPosition;
        _dragging = true;
    }

    /// <summary>
    ///     Extends the selection while the button that put the caret down is held, even outside the view.
    /// </summary>
    public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
    {
        if (!_dragging || !FontReady) return;

        var caret = IndexAt(e.Position);
        if (caret == CursorPosition) return;

        ResetTypingDelay();
        CursorPosition = caret;
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        _dragging = false;
        if (!HasSelection) _anchor = null;
    }

    private int IndexAt(in Vector2 surfacePosition)
    {
        var point = surfacePosition.Transform(ComputeAbsoluteContentTransform().Inverse()) + _scroll;
        return TextCaretLayout.IndexAt(GetContentBounds(), point);
    }

    public override void OnCharacter(CharacterSurfaceEvent e)
    {
        base.OnCharacter(e);
        ClearPreedit();
        ResetTypingDelay();
        ReplaceSelection(e.Character.ToString());
    }

    /// <summary>
    ///     Shows the text an input method is composing in place, at the caret, until it is committed.
    /// </summary>
    public override void OnTextPreedit(TextPreeditSurfaceEvent e)
    {
        base.OnTextPreedit(e);
        ResetTypingDelay();
        if (_preeditLength == 0 && HasSelection) DeleteSelection();
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
        if (e is { IsControlDown: true, State: InputState.Pressed or InputState.Repeat })
        {
            HandleShortcut(e);
        }
        else if (e is { Key: InputKey.Escape, State: InputState.Pressed })
        {
            // While composing, Escape belongs to the input method, which cancels the composition.
            if (UnfocusOnEscape && _preeditLength == 0) e.Surface.ClearFocus();
        }
        else if (e is { Key: InputKey.Backspace, State: InputState.Pressed or InputState.Repeat })
        {
            if (HasSelection)
            {
                ResetTypingDelay();
                DeleteSelection();
            }
            else if (CursorPosition > -1 && CursorPosition < Content.Length)
            {
                ResetTypingDelay();
                Edit(Content.Remove(CursorPosition, 1), CursorPosition - 1);
            }
        }
        else if (e is { Key: InputKey.Delete, State: InputState.Pressed or InputState.Repeat })
        {
            if (HasSelection)
            {
                ResetTypingDelay();
                DeleteSelection();
            }
            else if (CursorPosition + 1 < Content.Length)
            {
                ResetTypingDelay();
                Edit(Content.Remove(CursorPosition + 1, 1), CursorPosition);
            }
        }
        else if (e is { Key: InputKey.Home or InputKey.End, State: InputState.Pressed or InputState.Repeat })
        {
            ResetTypingDelay();
            MoveCaret(e.Key == InputKey.Home ? -1 : Content.Length - 1, e.IsShiftDown);
        }
        else if (e is { Key: InputKey.Left or InputKey.Right, State: InputState.Pressed or InputState.Repeat })
        {
            if (Content.NotEmpty())
            {
                ResetTypingDelay();
                var left = e.Key == InputKey.Left;
                // Without Shift, an arrow collapses the selection to its near end, as other editors do.
                if (HasSelection && !e.IsShiftDown)
                    MoveCaret(left ? SelectionStart : SelectionEnd, false);
                else
                    MoveCaret(int.Clamp(CursorPosition + (left ? -1 : 1), -1, Content.Length - 1), e.IsShiftDown);
            }
        }
        else if (e is { Key: InputKey.Enter, State: InputState.Pressed or InputState.Repeat })
        {
            ResetTypingDelay();
            OnCharacter(new CharacterSurfaceEvent(e.Surface, '\n', 0));
        }
    }

    private void MoveCaret(int caret, bool extendSelection)
    {
        if (extendSelection)
            _anchor ??= CursorPosition;
        else
            _anchor = null;
        CursorPosition = caret;
    }

    private void HandleShortcut(KeyboardSurfaceEvent e)
    {
        switch (e.Key)
        {
            case InputKey.A:
                if (Content.Length > 0) SelectAll();
                break;
            case InputKey.V:
                if (e.Surface.Clipboard.GetText() is { Length: > 0 } text) InsertText(text);
                break;
            case InputKey.C:
                if (HasSelection) e.Surface.Clipboard.SetText(SelectedText);
                break;
            case InputKey.X:
                if (!HasSelection) break;
                e.Surface.Clipboard.SetText(SelectedText);
                ClearPreedit();
                ResetTypingDelay();
                DeleteSelection();
                break;
        }
    }

    /// <summary>
    ///     Replaces the selection, or inserts at the caret, as one edit and moves the caret past the text.
    /// </summary>
    public virtual void InsertText(string text)
    {
        ClearPreedit();
        ResetTypingDelay();
        ReplaceSelection(text);
    }

    private void ReplaceSelection(string text)
    {
        var first = HasSelection ? SelectionStart + 1 : int.Clamp(CursorPosition + 1, 0, Content.Length);
        var length = HasSelection ? SelectionEnd - SelectionStart : 0;
        Edit(Content.Remove(first, length).Insert(first, text), first + text.Length - 1);
    }

    private void DeleteSelection()
    {
        ReplaceSelection(string.Empty);
    }

    /// <summary>
    ///     Content assigned from outside moves the caret to the end; this view's own edits set it themselves.
    /// </summary>
    protected override void TextChanged(string newText)
    {
        base.TextChanged(newText);
        if (_editing) return;
        CursorPosition = newText.Length - 1;
        _anchor = null;
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
        _anchor = null;
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
        if (FontReady && IsFocused) CollectSelection(transform, commands);
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

    private void CollectSelection(in Matrix4x4 transform, CommandList commands)
    {
        if (!HasSelection) return;
        TextCaretLayout.SelectionRects(GetContentBounds(), SelectionStart, SelectionEnd, _selectionRects);
        foreach (var rect in _selectionRects)
            commands.AddRect(transform.Translate(rect.Offset), rect.Size, SelectionColor);
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
