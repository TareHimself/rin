using System.Numerics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Events;

namespace Rin.Core.Tests.Views.Composite;

public class ButtonViewTests : FakeApplicationFixture
{
    [Test]
    public void CursorDownIsCapturedEvenWithoutHandlers()
    {
        var button = new ButtonView();
        var e = new CursorDownSurfaceEvent(new FakeSurface(), CursorButton.One, Vector2.Zero);

        button.OnCursorDown(e, Matrix4x4.Identity);

        Assert.That(e.Target, Is.SameAs(button));
        Assert.That(e.Handled, Is.True);
    }

    [Test]
    public void CursorDownRaisesOnPressedAndCaptures()
    {
        var button = new ButtonView();
        var pressed = 0;
        button.OnPressed += (_, _) => pressed++;
        var e = new CursorDownSurfaceEvent(new FakeSurface(), CursorButton.One, Vector2.Zero);

        button.OnCursorDown(e, Matrix4x4.Identity);

        Assert.That(pressed, Is.EqualTo(1));
        Assert.That(e.Target, Is.SameAs(button));
    }
}
