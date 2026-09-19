using Rin.World.Actors;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Tests.Components;

public class SkinnedMeshComponentTests
{
    [Test]
    public void PoseIsPushedEveryLateUpdateWhenAPoseSourceIsSet()
    {
        var render = new FakeRenderSystem();
        var physics = new FakePhysicsSystem();
        var world = new World(render, physics);

        var skeleton = new Skeleton([new Bone { Name = "root" }]);
        var poseSource = new FakePoseSource { Skeleton = skeleton, Pose = new SkeletalPose(1) };
        var mesh = new TestSkinnedMeshComponent { PoseSource = poseSource };
        var actor = new Actor { RootComponent = mesh };

        world.Start();
        world.AddActor(actor);

        world.Update(1f / 60f);
        world.Update(1f / 60f);

        Assert.That(render.PushedPoses, Has.Count.EqualTo(2),
            "unlike transform, pose has no cheap dirty-check, so it pushes unconditionally every LateUpdate");
        Assert.That(render.PushedPoses[0].Handle, Is.EqualTo(mesh.Proxy));
    }

    [Test]
    public void NoPosePushWhenThereIsNoPoseSource()
    {
        var render = new FakeRenderSystem();
        var physics = new FakePhysicsSystem();
        var world = new World(render, physics);

        var mesh = new TestSkinnedMeshComponent();
        var actor = new Actor { RootComponent = mesh };

        world.Start();
        world.AddActor(actor);

        world.Update(1f / 60f);

        Assert.That(render.PushedPoses, Is.Empty);
    }
}
