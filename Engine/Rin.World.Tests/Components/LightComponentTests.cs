using Rin.World.Actors;
using Rin.World.Components.Lights;

namespace Rin.World.Tests.Components;

public class LightComponentTests
{
    [Test]
    public void PropertyChangeAfterStartPushesLightUpdateWithoutTransformChange()
    {
        var render = new FakeRenderSystem();
        var world = new World(render, new FakePhysicsSystem());
        var light = new PointLightComponent();
        world.Start();
        world.AddActor(new Actor { RootComponent = light });

        light.Radiance = 5000;
        world.Update(world.PhysicsUpdateInterval);

        Assert.That(render.PushedLights, Has.Count.EqualTo(1));
        Assert.That(render.PushedLights[0].Light.Radiance, Is.EqualTo(5000));
    }

    [Test]
    public void UnchangedLightProducesNoUpdates()
    {
        var render = new FakeRenderSystem();
        var world = new World(render, new FakePhysicsSystem());
        world.Start();
        world.AddActor(new Actor { RootComponent = new PointLightComponent() });

        world.Update(world.PhysicsUpdateInterval);
        world.Update(world.PhysicsUpdateInterval);

        Assert.That(render.PushedLights, Is.Empty);
    }
}
