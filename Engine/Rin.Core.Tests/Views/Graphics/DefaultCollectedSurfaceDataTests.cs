using System.Numerics;
using Rin.Core.Shared.Pooling;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace Rin.Core.Tests.Views.Graphics;

public class DefaultCollectedSurfaceDataTests
{
    [Test]
    public void DisposingDataThatWasNeverWrittenReleasesItsCommands()
    {
        var commands = new CommandList { SurfaceSize = new Vector2(800f, 600f) };
        var command = new QuadDrawCommand([Quad.Rect(Matrix4x4.Identity, new Vector2(10f))]);
        commands.Add(command);
        var data = new DefaultCollectedSurfaceData(commands);

        data.Dispose();

        Assert.That(command.Quads.IsEmpty);
        Assert.DoesNotThrow(data.Dispose);
    }

    [Test]
    public void DisposingHandsTheCommandListBackEmptyAndOnlyOnce()
    {
        var lists = new Recycler<CommandList>(() => new CommandList { SurfaceSize = Vector2.Zero });
        var commands = lists.Take();
        commands.Add(new QuadDrawCommand([Quad.Rect(Matrix4x4.Identity, new Vector2(10f))]));
        var data = new DefaultCollectedSurfaceData(commands, lists);

        data.Dispose();
        data.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(lists.Take(), Is.SameAs(commands));
            Assert.That(commands.Commands, Is.Empty);
            Assert.That(lists.Take(), Is.Not.SameAs(commands));
        });
    }
}
