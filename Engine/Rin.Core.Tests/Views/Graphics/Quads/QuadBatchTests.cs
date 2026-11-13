using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace Rin.Core.Tests.Views.Graphics.Quads;

public class QuadBatchTests
{
    private static Quad[] Rects(int count)
    {
        var quads = new Quad[count];
        for (var i = 0; i < count; i++) quads[i] = Quad.Rect(Matrix4x4.Identity, new Vector2(i + 1f));
        return quads;
    }

    [Test]
    public void CommandKeepsItsOwnCopyOfTheQuads()
    {
        var source = Rects(2);
        using var command = new QuadDrawCommand(source);

        source[0] = Quad.Rect(Matrix4x4.Identity, new Vector2(99f));

        Assert.That(command.Quads.Length, Is.EqualTo(2));
        Assert.That(command.Quads[0].Size, Is.EqualTo(new Vector2(1f)));
    }

    [Test]
    public void BatchCountsQuadsAcrossCommandsInOrder()
    {
        var first = new QuadDrawCommand(Rects(2));
        var second = new QuadDrawCommand(Rects(3));
        using var batch = new QuadBatch();

        batch.AddFromCommand(first);
        batch.AddFromCommand(second);

        Assert.That(batch.QuadCount, Is.EqualTo(5));
        Assert.That(batch.Commands, Is.EqualTo(new[] { first, second }));
        Assert.That(batch.GetMemoryNeeded(), Is.EqualTo(Utils.ByteSizeOf<Quad>(5)));
    }

    [Test]
    public void DisposingTheBatchReleasesItsCommands()
    {
        var command = new QuadDrawCommand(Rects(4), [new ResourceHandle(ResourceType.Texture, 1)]);
        var batch = new QuadBatch();
        batch.AddFromCommand(command);

        batch.Dispose();

        Assert.That(command.Quads.IsEmpty);
        Assert.That(command.Textures.IsEmpty);
        Assert.That(batch.QuadCount, Is.Zero);
        Assert.DoesNotThrow(command.Dispose);
    }

    [Test]
    public void ReleasedQuadMemoryIsReusedByTheNextCommand()
    {
        var quads = Rects(256);
        new QuadDrawCommand(quads).Dispose();

        var before = GC.GetAllocatedBytesForCurrentThread();
        var command = new QuadDrawCommand(quads);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        command.Dispose();

        Assert.That((ulong)allocated, Is.LessThan(Utils.ByteSizeOf<Quad>(quads.Length)));
    }
}
