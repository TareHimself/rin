using System.Numerics;
using Rin.Core.Views;

namespace Rin.Core.Tests.Views;

public class BorderRadiusTests
{
    [Test]
    public void All_and_the_float_conversion_set_every_corner()
    {
        BorderRadius converted = 8f;

        Assert.That(BorderRadius.All(8f), Is.EqualTo(new BorderRadius(8f, 8f, 8f, 8f)));
        Assert.That(converted, Is.EqualTo(BorderRadius.All(8f)));
    }

    [Test]
    public void Side_helpers_round_only_that_side()
    {
        Assert.That(BorderRadius.Top(4f), Is.EqualTo(new BorderRadius(4f, 4f, 0f, 0f)));
        Assert.That(BorderRadius.Bottom(4f), Is.EqualTo(new BorderRadius(0f, 0f, 4f, 4f)));
        Assert.That(BorderRadius.Left(4f), Is.EqualTo(new BorderRadius(4f, 0f, 0f, 4f)));
        Assert.That(BorderRadius.Right(4f), Is.EqualTo(new BorderRadius(0f, 4f, 4f, 0f)));
    }

    [Test]
    public void Arithmetic_applies_to_every_corner()
    {
        var radius = new BorderRadius(1f, 2f, 3f, 4f);

        Assert.That(radius + 1f, Is.EqualTo(new BorderRadius(2f, 3f, 4f, 5f)));
        Assert.That(radius * 2f, Is.EqualTo(new BorderRadius(2f, 4f, 6f, 8f)));
        Assert.That(radius.ToVector4(), Is.EqualTo(new Vector4(1f, 2f, 3f, 4f)));
    }
}
