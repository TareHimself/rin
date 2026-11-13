using System.Numerics;
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
}
