using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Graphics.Vulkan.Windows;

namespace Rin.Graphics.Vulkan.Tests.Windows;

public class ResolvedWindowEventTests
{
    private ulong _handle;

    [SetUp]
    public void CreateWindow()
    {
        FakeNative.Reset();
        _handle = Native.CreateWindow("test", new Extent2D(640, 480), WindowFlags.None);
        Native.platformWindowSetCursorPosition(_handle, new Vector2(5, 6));
    }

    private Native.Window.WindowEvent Event(Native.Window.EventType type)
    {
        return new Native.Window.WindowEvent { info = new Native.Window.EventInfo { type = type, windowId = _handle } };
    }

    // The type travels as a uint because NUnit needs public test signatures and EventType is internal.
    [TestCase((uint)Native.Window.EventType.CursorMove)]
    [TestCase((uint)Native.Window.EventType.CursorButton)]
    [TestCase((uint)Native.Window.EventType.Scroll)]
    public void Resolve_CursorEvents_ReadTheWindowsCursorPosition(uint type)
    {
        var resolved = ResolvedWindowEvent.Resolve(Event((Native.Window.EventType)type));

        Assert.Multiple(() =>
        {
            Assert.That(resolved.CursorPosition, Is.EqualTo(new Vector2(5, 6)));
            Assert.That(resolved.Text, Is.Empty);
        });
    }

    [Test]
    public void Resolve_TextCommit_CopiesTheTextBehindTheRef()
    {
        FakeNative.SetEventText("xyz");
        var e = Event(Native.Window.EventType.TextCommit);
        e.textCommit.text = new Native.Window.TextRef { offset = 1, length = 2 };

        var resolved = ResolvedWindowEvent.Resolve(e);

        Assert.That(resolved.Text, Is.EqualTo("yz"));
    }

    [Test]
    public void Resolve_TextPreedit_CopiesTheTextBehindTheRef()
    {
        FakeNative.SetEventText("にほん");
        var e = Event(Native.Window.EventType.TextPreedit);
        e.textPreedit.text = new Native.Window.TextRef { offset = 0, length = 3 };

        var resolved = ResolvedWindowEvent.Resolve(e);

        Assert.That(resolved.Text, Is.EqualTo("にほん"));
    }

    [Test]
    public void Resolve_TextWhoseBackingIsGone_HasNoText()
    {
        var e = Event(Native.Window.EventType.TextCommit);
        e.textCommit.text = new Native.Window.TextRef { offset = 0, length = 3 };

        var resolved = ResolvedWindowEvent.Resolve(e);

        Assert.That(resolved.Text, Is.Empty);
    }

    [Test]
    public void Resolve_OtherEvents_ReadNothing()
    {
        var resolved = ResolvedWindowEvent.Resolve(Event(Native.Window.EventType.Key));

        Assert.Multiple(() =>
        {
            Assert.That(resolved.CursorPosition, Is.EqualTo(Vector2.Zero));
            Assert.That(resolved.Text, Is.Empty);
            Assert.That(resolved.Raw.info.type, Is.EqualTo(Native.Window.EventType.Key));
        });
    }
}
