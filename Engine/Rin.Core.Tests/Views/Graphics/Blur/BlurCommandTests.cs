using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Blur;

namespace Rin.Core.Tests.Views.Graphics.Blur;

public class BlurCommandTests
{
    private const int CommandsPerBlur = 4;

    private static CommandList NewList()
    {
        return new CommandList { SurfaceSize = new Vector2(800f, 600f) };
    }

    private static Matrix4x4 At(float x, float y)
    {
        return Matrix4x4.Identity.Translate(new Vector2(x, y));
    }

    [Test]
    public void BlurOnTheSurfaceAddsItsFourCommands()
    {
        var list = NewList().AddBlur(At(100f, 100f), new Vector2(200f, 100f));

        Assert.That(list.Commands, Has.Count.EqualTo(CommandsPerBlur));
    }

    [Test]
    public void BlurPartlyOffTheSurfaceStillAddsItsCommands()
    {
        var list = NewList().AddBlur(At(700f, 550f), new Vector2(200f, 100f));

        Assert.That(list.Commands, Has.Count.EqualTo(CommandsPerBlur));
    }

    [TestCase(900f, 100f)]
    [TestCase(100f, 700f)]
    [TestCase(-300f, 100f)]
    [TestCase(800f, 100f)]
    public void BlurFullyOffTheSurfaceAddsNothing(float x, float y)
    {
        var list = NewList().AddBlur(At(x, y), new Vector2(200f, 100f));

        Assert.That(list.Commands, Is.Empty);
    }
}
