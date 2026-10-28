using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Graphics.Windows.Events;

namespace Rin.Graphics.Vulkan.Windows;

public class RinWindow : IWindow
{
    private readonly HashSet<IWindow> _children = [];
    private readonly GCHandle _self;
    private ulong _handle;
    private bool _typing;
    private Native.Window.Rect2D _typingRect;
    private ExceptionDispatchInfo? _dragHandlerError;

    public RinWindow(in ulong handle, IWindow? parent)
    {
        _handle = handle;
        Parent = parent;
        _self = GCHandle.Alloc(this);
        RegisterDropCallbacks();
    }

    public IWindow? Parent { get; }

    public void Dispose()
    {
        foreach (var window in _children) window.Dispose();
        _children.Clear();

        OnDispose?.Invoke();
        if (_handle == 0) return;
        Native.platformWindowClearDropCallbacks(_handle);
        Native.platformWindowDestroy(_handle);
        _handle = 0;
        _self.Free();
    }

    public bool IsFullscreen { get; }
    public event Action<KeyEvent>? OnKey;
    public event Action<CursorMoveEvent>? OnCursorMoved;
    public event Action<CursorButtonEvent>? OnCursorButton;
    public event Action<FocusEvent>? OnCursorFocus;
    public event Action<FocusEvent>? OnKeyboardFocus;
    public event Action<ScrollEvent>? OnScroll;
    public event Action<ResizeEvent>? OnResize;
    public event Action<CloseEvent>? OnClose;
    public event Action<CharacterEvent>? OnCharacter;
    public event Action<TextPreeditEvent>? OnTextPreedit;
    public event Action<MaximizeEvent>? OnMaximize;
    public event Action<MinimizeEvent>? OnMinimize;
    public event Action<RefreshEvent>? OnRefresh;
    public event Action<DragEvent>? OnDragEnter;
    public event Action<DragEvent>? OnDragOver;
    public event Action? OnDragLeave;
    public event Action<DropEvent>? OnDrop;

    private unsafe void RegisterDropCallbacks()
    {
        var callbacks = new Native.Window.DropCallbacks
        {
            enter = &OnNativeDragEnter,
            over = &OnNativeDragOver,
            drop = &OnNativeDrop,
            leave = &OnNativeDragLeave,
            userData = GCHandle.ToIntPtr(_self)
        };
        Native.platformWindowSetDropCallbacks(_handle, &callbacks);
    }

    private static RinWindow? WindowFrom(IntPtr userData)
    {
        return GCHandle.FromIntPtr(userData).Target as RinWindow;
    }

    [UnmanagedCallersOnly]
    private static unsafe DropEffect OnNativeDragEnter(Native.Window.DropInfo* info, Native.Window.DropData* data,
        IntPtr userData)
    {
        return WindowFrom(userData)?.RaiseDrag(info, data, DragStage.Enter) ?? DropEffect.None;
    }

    [UnmanagedCallersOnly]
    private static unsafe DropEffect OnNativeDragOver(Native.Window.DropInfo* info, Native.Window.DropData* data,
        IntPtr userData)
    {
        return WindowFrom(userData)?.RaiseDrag(info, data, DragStage.Over) ?? DropEffect.None;
    }

    [UnmanagedCallersOnly]
    private static unsafe DropEffect OnNativeDrop(Native.Window.DropInfo* info, Native.Window.DropData* data,
        IntPtr userData)
    {
        return WindowFrom(userData)?.RaiseDrag(info, data, DragStage.Drop) ?? DropEffect.None;
    }

    [UnmanagedCallersOnly]
    private static void OnNativeDragLeave(IntPtr userData)
    {
        if (WindowFrom(userData) is not { } window || window.OnDrop is null) return;
        try
        {
            window.OnDragLeave?.Invoke();
        }
        catch (Exception e)
        {
            window._dragHandlerError ??= ExceptionDispatchInfo.Capture(e);
        }
    }

    private enum DragStage
    {
        Enter,
        Over,
        Drop
    }

    // Runs inside the native pump, where an exception cannot unwind, so handler errors are kept for the next update.
    private unsafe DropEffect RaiseDrag(Native.Window.DropInfo* info, Native.Window.DropData* data, DragStage stage)
    {
        if (OnDrop is null) return DropEffect.None;

        try
        {
            var e = ReadDrag(info, data, stage == DragStage.Drop);
            switch (stage)
            {
                case DragStage.Enter:
                    OnDragEnter?.Invoke(e);
                    break;
                case DragStage.Over:
                    OnDragOver?.Invoke(e);
                    break;
                case DragStage.Drop:
                    OnDrop?.Invoke((DropEvent)e);
                    break;
            }

            return e.Effect;
        }
        catch (Exception exception)
        {
            _dragHandlerError ??= ExceptionDispatchInfo.Capture(exception);
            return DropEffect.None;
        }
    }

    private unsafe DragEvent ReadDrag(Native.Window.DropInfo* info, Native.Window.DropData* data, bool drop)
    {
        var paths = new string[data->fileCount];
        for (var i = 0; i < paths.Length; i++) paths[i] = Marshal.PtrToStringUTF8((nint)data->files[i]) ?? string.Empty;
        var text = data->text == null ? null : new string(data->text, 0, (int)data->textLength);

        return drop
            ? new DropEvent
            {
                Window = this,
                Position = info->position,
                Paths = paths,
                Text = text,
                Allowed = info->allowed,
                Modifiers = info->modifiers,
                Effect = DefaultEffect(info->allowed)
            }
            : new DragEvent
            {
                Window = this,
                Position = info->position,
                Paths = paths,
                Text = text,
                Allowed = info->allowed,
                Modifiers = info->modifiers,
                Effect = DefaultEffect(info->allowed)
            };
    }

    private static DropEffect DefaultEffect(DropEffect allowed)
    {
        if (allowed.HasFlag(DropEffect.Copy)) return DropEffect.Copy;
        if (allowed.HasFlag(DropEffect.Move)) return DropEffect.Move;
        if (allowed.HasFlag(DropEffect.Link)) return DropEffect.Link;
        return DropEffect.None;
    }

    /// <summary>
    ///     Rethrows an exception a drag handler threw inside the native event pump.
    /// </summary>
    internal void ThrowPendingDragHandlerError()
    {
        if (_dragHandlerError is null) return;
        var error = _dragHandlerError;
        _dragHandlerError = null;
        error.Throw();
    }

    public void SetHitTestCallback(Func<WindowHitTestResult, IWindow>? callback)
    {
        throw new NotImplementedException();
    }

    public Vector2 GetCursorPosition()
    {
        return Native.platformWindowGetCursorPosition(_handle);
    }

    public void SetCursorPosition(in Vector2 position)
    {
        Native.platformWindowSetCursorPosition(_handle, position);
    }

    public void SetFullscreen(bool state)
    {
        throw new NotImplementedException();
    }

    public void SetSize(in Extent2D size)
    {
        throw new NotImplementedException();
    }

    public Extent2D GetSize()
    {
        return Native.platformWindowGetSize(_handle);
    }

    public IWindow CreateChild(string name, in Extent2D extent, WindowFlags flags = WindowFlags.Visible)
    {
        var child = IGraphicsModule.Get().CreateWindow(name, extent, flags, this);
        _children.Add(child);
        child.OnDispose += () => _children.Remove(child);
        return child;
    }

    public void StartTyping()
    {
        _typing = true;
        Native.platformWindowStartTextInput(_handle, _typingRect);
    }

    public void StopTyping()
    {
        _typing = false;
        Native.platformWindowStopTextInput(_handle);
    }

    public void SetTypingRect(in Rect2D caret)
    {
        _typingRect = new Native.Window.Rect2D
        {
            x = (int)float.Floor(caret.Offset.X),
            y = (int)float.Floor(caret.Offset.Y),
            width = (uint)float.Ceiling(float.Max(caret.Size.X, 0f)),
            height = (uint)float.Ceiling(float.Max(caret.Size.Y, 0f))
        };
        if (_typing) Native.platformWindowSetTextInputRect(_handle, _typingRect);
    }

    public event Action? OnDispose;

    /// <summary>
    ///     Turns a resolved native event into the matching window event. Pure: the cursor position and text were
    ///     read by <see cref="ResolvedWindowEvent.Resolve" /> before the event got here.
    /// </summary>
    internal void ProcessEvent(in ResolvedWindowEvent resolved)
    {
        var e = resolved.Raw;
        switch (e.info.type)
        {
            case Native.Window.EventType.Key:
                OnKey?.Invoke(new KeyEvent
                {
                    Key = e.key.key,
                    Modifiers = e.key.modifier,
                    State = e.key.state,
                    Window = this
                });
                break;
            case Native.Window.EventType.Resize:
                OnResize?.Invoke(new ResizeEvent
                {
                    Window = this,
                    Size = e.resize.size
                });
                break;
            case Native.Window.EventType.Minimize:
                OnMinimize?.Invoke(new MinimizeEvent
                {
                    Window = this
                });
                break;
            case Native.Window.EventType.Maximize:
                OnMaximize?.Invoke(new MaximizeEvent
                {
                    Window = this
                });
                break;
            case Native.Window.EventType.Scroll:
                OnScroll?.Invoke(new ScrollEvent
                {
                    Window = this,
                    Delta = e.scroll.delta,
                    Position = resolved.CursorPosition
                });
                break;
            case Native.Window.EventType.CursorMove:
                OnCursorMoved?.Invoke(new CursorMoveEvent
                {
                    Window = this,
                    Position = resolved.CursorPosition
                });
                break;
            case Native.Window.EventType.CursorButton:
                OnCursorButton?.Invoke(new CursorButtonEvent
                {
                    Window = this,
                    Position = resolved.CursorPosition,
                    Button = e.cursorButton.button,
                    Modifiers = e.cursorButton.modifier,
                    State = e.cursorButton.state
                });
                break;
            case Native.Window.EventType.CursorFocus:
                OnCursorFocus?.Invoke(new FocusEvent
                {
                    Window = this,
                    IsFocused = e.cursorFocus.focused == 1
                });
                break;
            case Native.Window.EventType.KeyboardFocus:
                OnKeyboardFocus?.Invoke(new FocusEvent
                {
                    Window = this,
                    IsFocused = e.keyboardFocus.focused == 1
                });
                break;
            case Native.Window.EventType.Close:
                OnClose?.Invoke(new CloseEvent
                {
                    Window = this
                });
                break;
            case Native.Window.EventType.TextCommit:
                // UTF-16, so a character outside the basic plane arrives as its two surrogates in order.
                foreach (var character in resolved.Text)
                    OnCharacter?.Invoke(new CharacterEvent
                    {
                        Window = this,
                        Data = character,
                        Modifiers = 0
                    });
                break;
            case Native.Window.EventType.TextPreedit:
                OnTextPreedit?.Invoke(new TextPreeditEvent
                {
                    Window = this,
                    Text = resolved.Text,
                    CaretStart = (int)e.textPreedit.caretStart,
                    CaretEnd = (int)e.textPreedit.caretEnd
                });
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetPosition(in Offset2D position)
    {
        throw new NotImplementedException();
    }

    public ulong GetHandle()
    {
        return _handle;
    }
}