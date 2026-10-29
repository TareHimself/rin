using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Graphics.Windows.Events;
using Rin.Graphics.Vulkan.Windows;

namespace Rin.Graphics.Vulkan.Tests.Windows;

public class RinWindowTests
{
    private RinWindow _window = null!;
    private ulong _handle;

    [SetUp]
    public void CreateWindow()
    {
        FakeNative.Reset();
        _handle = Native.CreateWindow("test", new Extent2D(640, 480), WindowFlags.None);
        _window = new RinWindow(_handle, null);
    }

    [TearDown]
    public void DisposeWindow()
    {
        _window.Dispose();
    }

    private static Native.Window.WindowEvent Event(Native.Window.EventType type, ulong windowId)
    {
        return new Native.Window.WindowEvent { info = new Native.Window.EventInfo { type = type, windowId = windowId } };
    }

    private static ResolvedWindowEvent Resolved(in Native.Window.WindowEvent e, Vector2 cursor = default,
        string text = "")
    {
        return new ResolvedWindowEvent(e, cursor, text);
    }

    [Test]
    public void Constructor_RegistersDropCallbacks()
    {
        Assert.That(FakeNative.WindowHasDropCallbacks(_handle), Is.EqualTo(1));
    }

    [Test]
    public void Dispose_DestroysTheNativeWindowOnce()
    {
        var disposed = 0;
        _window.OnDispose += () => disposed++;

        _window.Dispose();
        _window.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(FakeNative.WindowCount(), Is.Zero);
            Assert.That(_window.GetHandle(), Is.Zero);
            Assert.That(disposed, Is.EqualTo(2));
        });
    }

    [Test]
    public void GetSize_ReadsTheNativeWindow()
    {
        Assert.That(_window.GetSize(), Is.EqualTo(new Extent2D(640, 480)));
    }

    [Test]
    public void SetSize_IsReflectedByGetSize()
    {
        _window.SetSize(new Extent2D(800, 600));

        Assert.That(_window.GetSize(), Is.EqualTo(new Extent2D(800, 600)));
    }

    [Test]
    public void SetFullscreen_ReachesTheNativeWindowAndTracksTheState()
    {
        _window.SetFullscreen(true);

        Assert.Multiple(() =>
        {
            Assert.That(_window.IsFullscreen, Is.True);
            Assert.That(FakeNative.WindowIsFullscreen(_handle), Is.EqualTo(1));
        });

        _window.SetFullscreen(false);

        Assert.Multiple(() =>
        {
            Assert.That(_window.IsFullscreen, Is.False);
            Assert.That(FakeNative.WindowIsFullscreen(_handle), Is.Zero);
        });
    }

    [Test]
    public void SetCursorPosition_RoundTrips()
    {
        _window.SetCursorPosition(new Vector2(12, 34));

        Assert.That(_window.GetCursorPosition(), Is.EqualTo(new Vector2(12, 34)));
    }

    [Test]
    public void ProcessEvent_Key_RaisesOnKey()
    {
        KeyEvent? raised = null;
        _window.OnKey += e => raised = e;
        var e = Event(Native.Window.EventType.Key, _handle);
        e.key.key = InputKey.A;
        e.key.state = InputState.Repeat;
        e.key.modifier = InputModifier.Control | InputModifier.Shift;

        _window.ProcessEvent(Resolved(e));

        Assert.That(raised, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(raised!.Key, Is.EqualTo(InputKey.A));
            Assert.That(raised.State, Is.EqualTo(InputState.Repeat));
            Assert.That(raised.IsControlDown, Is.True);
            Assert.That(raised.IsShiftDown, Is.True);
            Assert.That(raised.IsAltDown, Is.False);
            Assert.That(raised.Window, Is.SameAs(_window));
        });
    }

    [Test]
    public void ProcessEvent_Resize_RaisesOnResize()
    {
        ResizeEvent? raised = null;
        _window.OnResize += e => raised = e;
        var e = Event(Native.Window.EventType.Resize, _handle);
        e.resize.size = new Extent2D(800, 600);

        _window.ProcessEvent(Resolved(e));

        Assert.That(raised?.Size, Is.EqualTo(new Extent2D(800, 600)));
    }

    [TestCase(1, true)]
    [TestCase(0, false)]
    public void ProcessEvent_KeyboardFocus_MapsTheFocusedFlag(int focused, bool expected)
    {
        FocusEvent? raised = null;
        _window.OnKeyboardFocus += e => raised = e;
        var e = Event(Native.Window.EventType.KeyboardFocus, _handle);
        e.keyboardFocus.focused = focused;

        _window.ProcessEvent(Resolved(e));

        Assert.That(raised?.IsFocused, Is.EqualTo(expected));
    }

    [Test]
    public void ProcessEvent_CursorButton_UsesTheResolvedCursorPosition()
    {
        CursorButtonEvent? raised = null;
        _window.OnCursorButton += e => raised = e;
        var e = Event(Native.Window.EventType.CursorButton, _handle);
        e.cursorButton.button = CursorButton.Two;
        e.cursorButton.state = InputState.Released;

        _window.ProcessEvent(Resolved(e, cursor: new Vector2(5, 6)));

        Assert.That(raised, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(raised!.Position, Is.EqualTo(new Vector2(5, 6)));
            Assert.That(raised.Button, Is.EqualTo(CursorButton.Two));
            Assert.That(raised.State, Is.EqualTo(InputState.Released));
        });
    }

    [Test]
    public void ProcessEvent_Scroll_CarriesTheDeltaAndTheResolvedCursorPosition()
    {
        ScrollEvent? raised = null;
        _window.OnScroll += e => raised = e;
        var e = Event(Native.Window.EventType.Scroll, _handle);
        e.scroll.delta = new Vector2(0, -3);

        _window.ProcessEvent(Resolved(e, cursor: new Vector2(7, 8)));

        Assert.Multiple(() =>
        {
            Assert.That(raised?.Delta, Is.EqualTo(new Vector2(0, -3)));
            Assert.That(raised?.Position, Is.EqualTo(new Vector2(7, 8)));
        });
    }

    [Test]
    public void ProcessEvent_CursorMove_UsesTheResolvedCursorPosition()
    {
        CursorMoveEvent? raised = null;
        _window.OnCursorMoved += e => raised = e;
        var e = Event(Native.Window.EventType.CursorMove, _handle);

        _window.ProcessEvent(Resolved(e, cursor: new Vector2(1, 2)));

        Assert.That(raised?.Position, Is.EqualTo(new Vector2(1, 2)));
    }

    [Test]
    public void ProcessEvent_TextCommit_RaisesOneCharacterPerUtf16Unit()
    {
        var characters = new List<char>();
        _window.OnCharacter += e => characters.Add(e.Data);
        var e = Event(Native.Window.EventType.TextCommit, _handle);

        _window.ProcessEvent(Resolved(e, text: "a😀"));

        Assert.That(characters, Is.EqualTo("a😀".ToCharArray()));
    }

    [Test]
    public void ProcessEvent_TextCommit_WithoutText_RaisesNothing()
    {
        var characters = 0;
        _window.OnCharacter += _ => characters++;
        var e = Event(Native.Window.EventType.TextCommit, _handle);

        _window.ProcessEvent(Resolved(e));

        Assert.That(characters, Is.Zero);
    }

    [Test]
    public void ProcessEvent_TextPreedit_RaisesTextAndCaret()
    {
        TextPreeditEvent? raised = null;
        _window.OnTextPreedit += e => raised = e;
        var e = Event(Native.Window.EventType.TextPreedit, _handle);
        e.textPreedit.caretStart = 1;
        e.textPreedit.caretEnd = 2;

        _window.ProcessEvent(Resolved(e, text: "にほん"));

        Assert.That(raised, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(raised!.Text, Is.EqualTo("にほん"));
            Assert.That(raised.CaretStart, Is.EqualTo(1));
            Assert.That(raised.CaretEnd, Is.EqualTo(2));
        });
    }

    [Test]
    public void ProcessEvent_UnknownType_Throws()
    {
        var e = Event((Native.Window.EventType)99, _handle);

        Assert.Throws<ArgumentOutOfRangeException>(() => _window.ProcessEvent(Resolved(e)));
    }

    private unsafe DropEffect Drag(string[] paths, string? text, DropEffect allowed,
        Func<ulong, IntPtr, IntPtr, uint> stage)
    {
        var pathPointers = paths.Select(Marshal.StringToCoTaskMemUTF8).ToArray();
        try
        {
            fixed (IntPtr* files = pathPointers)
            fixed (char* chars = text)
            {
                var data = new Native.Window.DropData
                {
                    text = chars,
                    textLength = (ulong)(text?.Length ?? 0),
                    files = (byte**)files,
                    fileCount = (ulong)paths.Length
                };
                var info = new Native.Window.DropInfo
                {
                    position = new Vector2(10, 20),
                    allowed = allowed,
                    modifiers = InputModifier.Shift
                };
                return (DropEffect)stage(_handle, (IntPtr)(&info), (IntPtr)(&data));
            }
        }
        finally
        {
            foreach (var pointer in pathPointers) Marshal.FreeCoTaskMem(pointer);
        }
    }

    private static unsafe uint Enter(ulong handle, IntPtr info, IntPtr data)
    {
        return FakeNative.DragEnter(handle, (Native.Window.DropInfo*)info, (Native.Window.DropData*)data);
    }

    private static unsafe uint Over(ulong handle, IntPtr info, IntPtr data)
    {
        return FakeNative.DragOver(handle, (Native.Window.DropInfo*)info, (Native.Window.DropData*)data);
    }

    private static unsafe uint Drop(ulong handle, IntPtr info, IntPtr data)
    {
        return FakeNative.Drop(handle, (Native.Window.DropInfo*)info, (Native.Window.DropData*)data);
    }

    [Test]
    public void Drag_WithoutADropHandler_RefusesAndRaisesNothing()
    {
        var entered = false;
        _window.OnDragEnter += _ => entered = true;

        var effect = Drag(["a.txt"], null, DropEffect.Copy, Enter);

        Assert.Multiple(() =>
        {
            Assert.That(effect, Is.EqualTo(DropEffect.None));
            Assert.That(entered, Is.False);
        });
    }

    [Test]
    public void DragEnter_DecodesPathsTextAndTheDefaultEffect()
    {
        DragEvent? raised = null;
        _window.OnDrop += _ => { };
        _window.OnDragEnter += e => raised = e;

        var effect = Drag(["C:/a.txt", "C:/ü.png"], "dragged", DropEffect.Move | DropEffect.Link, Enter);

        Assert.That(raised, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(effect, Is.EqualTo(DropEffect.Move));
            Assert.That(raised!.Paths, Is.EqualTo(new[] { "C:/a.txt", "C:/ü.png" }));
            Assert.That(raised.Text, Is.EqualTo("dragged"));
            Assert.That(raised.Position, Is.EqualTo(new Vector2(10, 20)));
            Assert.That(raised.Allowed, Is.EqualTo(DropEffect.Move | DropEffect.Link));
            Assert.That(raised.Modifiers, Is.EqualTo(InputModifier.Shift));
        });
    }

    [Test]
    public void DragOver_WithoutText_HasNoText()
    {
        DragEvent? raised = null;
        _window.OnDrop += _ => { };
        _window.OnDragOver += e => raised = e;

        Drag(["a.txt"], null, DropEffect.Copy, Over);

        Assert.Multiple(() =>
        {
            Assert.That(raised?.HasText, Is.False);
            Assert.That(raised?.HasPaths, Is.True);
        });
    }

    [Test]
    public void Drop_ReturnsTheEffectTheHandlerChose()
    {
        DropEvent? raised = null;
        _window.OnDrop += e =>
        {
            raised = e;
            e.Effect = DropEffect.Link;
        };

        var effect = Drag([], "text", DropEffect.Copy | DropEffect.Link, Drop);

        Assert.Multiple(() =>
        {
            Assert.That(effect, Is.EqualTo(DropEffect.Link));
            Assert.That(raised?.Paths, Is.Empty);
        });
    }

    [Test]
    public void DragLeave_RaisesOnDragLeave()
    {
        var left = 0;
        _window.OnDrop += _ => { };
        _window.OnDragLeave += () => left++;

        FakeNative.DragLeave(_handle);

        Assert.That(left, Is.EqualTo(1));
    }

    [Test]
    public void DragHandlerException_IsKeptUntilThrowPendingDragHandlerError()
    {
        _window.OnDrop += _ => { };
        _window.OnDragEnter += _ => throw new InvalidOperationException("handler failed");

        var effect = Drag(["a.txt"], null, DropEffect.Copy, Enter);

        Assert.Multiple(() =>
        {
            Assert.That(effect, Is.EqualTo(DropEffect.None));
            var thrown = Assert.Throws<InvalidOperationException>(() => _window.ThrowPendingDragHandlerError());
            Assert.That(thrown.Message, Is.EqualTo("handler failed"));
            Assert.DoesNotThrow(() => _window.ThrowPendingDragHandlerError());
        });
    }
}
