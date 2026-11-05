using System.Numerics;
using Rin.World.Graphics;
using Rin.World.Transforms;

namespace Rin.World.Tests.Graphics;

public class CameraExtensionsTests
{
    [Test]
    public void TheViewTakesTheCameraSettingsAndTheWorldMatrix()
    {
        var world = new World();
        var parent = world.Spawn();
        var camera = world.Spawn();
        world.SetPosition(parent, new Vector3(0, 10, 0));
        world.SetPosition(camera, new Vector3(0, 0, -5));
        world.SetParent(camera, parent);
        world.Add(camera, new Camera { FieldOfView = 60f, NearClipPlane = 0.5f, FarClipPlane = 900f });
        world.Update(0f);

        var view = world.GetCameraView(camera);

        Assert.That(view.World, Is.EqualTo(world.Get<WorldTransform>(camera).Matrix));
        Assert.That(view.World.Translation, Is.EqualTo(new Vector3(0, 10, -5)));
        Assert.That(view.FieldOfView, Is.EqualTo(60f));
        Assert.That(view.NearClipPlane, Is.EqualTo(0.5f));
        Assert.That(view.FarClipPlane, Is.EqualTo(900f));
    }

    [Test]
    public void ACameraWithoutTransformsViewsFromTheOrigin()
    {
        var world = new World();
        var camera = world.Spawn();
        world.Add(camera, new Camera());

        Assert.That(world.GetCameraView(camera).World, Is.EqualTo(Matrix4x4.Identity));
    }
}
