using System.Numerics;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Commands;

namespace Rin.Core.Tests.Views.Graphics;

public class CommandListClipTests
{
    private static CommandList NewList()
    {
        return new CommandList { SurfaceSize = new Vector2(800f, 600f) };
    }

    [Test]
    public void CommandsOutsideAnyClipHaveAnEmptyStack()
    {
        var list = NewList();

        list.Add(new NoOpCommand());

        Assert.That(list.ClipIds.Single().Ids, Is.Empty);
    }

    [Test]
    public void NestedClipsAccumulateAndPopRestoresTheParentStack()
    {
        var list = NewList();

        list.PushClip(Matrix4x4.Identity, new Vector2(10f));
        list.Add(new NoOpCommand());
        list.PushClip(Matrix4x4.Identity, new Vector2(5f));
        list.Add(new NoOpCommand());
        list.PopClip();
        list.Add(new NoOpCommand());
        list.PopClip();
        list.Add(new NoOpCommand());

        Assert.That(list.ClipIds.Select(k => k.Ids), Is.EqualTo(new[]
        {
            new uint[] { 0 },
            new uint[] { 0, 1 },
            new uint[] { 0 },
            Array.Empty<uint>()
        }));
    }

    [Test]
    public void CommandsSharingAStackRegisterItOnce()
    {
        var list = NewList();

        list.PushClip(Matrix4x4.Identity, new Vector2(10f));
        list.Add(new NoOpCommand());
        list.Add(new NoOpCommand());
        list.Add(new NoOpCommand());

        Assert.That(list.UniqueClipStacks, Has.Count.EqualTo(1));
        Assert.That(list.ClipIds.Distinct().Count(), Is.EqualTo(1));
    }

    [Test]
    public void ReenteringAClipRegistersANewStackBecauseEachPushIsItsOwnClip()
    {
        var list = NewList();

        list.PushClip(Matrix4x4.Identity, new Vector2(10f));
        list.Add(new NoOpCommand());
        list.PopClip();
        list.PushClip(Matrix4x4.Identity, new Vector2(10f));
        list.Add(new NoOpCommand());

        Assert.That(list.Clips, Has.Count.EqualTo(2));
        Assert.That(list.UniqueClipStacks, Has.Count.EqualTo(2));
    }
}
