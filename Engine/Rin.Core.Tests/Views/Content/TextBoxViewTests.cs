using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Content;
using Rin.Core.Views.Font;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace Rin.Core.Tests.Views.Content;

public class TextBoxViewTests
{
    private FakeViewsModule _module = null!;

    [SetUp]
    public void SetUp()
    {
        IApplication.Override = new FakeApplication();
        _module = new FakeViewsModule();
        ((FakeFontManager)_module.FontManager).GlyphState = LiveGlyphState.Ready;
        IViewsModule.Override = _module;
    }

    [TearDown]
    public void TearDown()
    {
        IApplication.Override = null;
        IViewsModule.Override = null;
    }

    private static int CollectQuadCount(TextBoxView view)
    {
        var commands = new CommandList { SurfaceSize = new Vector2(800f, 600f) };
        view.CollectContent(Matrix4x4.Identity, commands);
        var count = 0;
        foreach (var command in commands.Commands)
        {
            var quads = (QuadDrawCommand)command;
            count += quads.Quads.Length;
            quads.Dispose();
        }

        return count;
    }

    [Test]
    public void CollectsOneQuadPerReadyGlyph()
    {
        var text = new TextBoxView { Content = "abc" };

        Assert.That(CollectQuadCount(text), Is.EqualTo(3));
        Assert.That(CollectQuadCount(text), Is.EqualTo(3));
    }

    [Test]
    public void CollectsTheNewTextAfterAChange()
    {
        var text = new TextBoxView { Content = "abc" };
        CollectQuadCount(text);

        text.Content = "abcde";

        Assert.That(CollectQuadCount(text), Is.EqualTo(5));
    }

    [Test]
    public void StillCollectsAfterItsLayoutIsReleased()
    {
        var text = new TextBoxView { Content = "abc" };
        CollectQuadCount(text);

        text.Dispose();
        text.Dispose();

        Assert.That(CollectQuadCount(text), Is.EqualTo(3));
    }

    [Test]
    public void CollectsNothingWhileGlyphsArePending()
    {
        ((FakeFontManager)_module.FontManager).GlyphState = LiveGlyphState.Pending;
        var text = new TextBoxView { Content = "abc" };

        Assert.That(CollectQuadCount(text), Is.Zero);
    }

    [Test]
    public void UnchangedTextCollectsWithoutAllocatingLayout()
    {
        var text = new TextBoxView { Content = new string('a', 512) };
        CollectQuadCount(text);
        CollectQuadCount(text);

        var before = GC.GetAllocatedBytesForCurrentThread();
        CollectQuadCount(text);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.LessThan(1024));
    }
}
