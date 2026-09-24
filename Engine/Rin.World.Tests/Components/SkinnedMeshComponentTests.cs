using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Actors;
using Rin.World.Components;
using Rin.World.Math;
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

    private static (Skeleton Skeleton, Bone Socket) BuildSkeletonWithSocket(Vector3 socketOffset)
    {
        var root = new Bone { Name = "root", LocalTransform = new Transform() };
        var socket = new Bone { Name = "socket", Parent = root, LocalTransform = new Transform { Position = socketOffset } };
        root.Children = [socket];
        return (new Skeleton([root, socket]), socket);
    }

    [Test]
    public void ResolvesABoneByName()
    {
        var (skeleton, _) = BuildSkeletonWithSocket(new Vector3(1, 0, 0));
        var mesh = new SkinnedMeshComponent { Mesh = new SkinnedMesh { Skeleton = skeleton, MeshId = 0 } };

        mesh.Update(1f / 60f);

        Assert.That(mesh.GetAttachPointTransform("socket").Position, Is.EqualTo(new Vector3(1, 0, 0)));
    }

    [Test]
    public void FallsBackToOwnTransformForAnUnknownName()
    {
        var (skeleton, _) = BuildSkeletonWithSocket(new Vector3(1, 0, 0));
        var mesh = new SkinnedMeshComponent { Mesh = new SkinnedMesh { Skeleton = skeleton, MeshId = 0 } };
        mesh.SetLocation(new Vector3(5, 0, 0));

        mesh.Update(1f / 60f);

        Assert.That(mesh.GetAttachPointTransform("nonexistent").Position, Is.EqualTo(new Vector3(5, 0, 0)));
    }

    [Test]
    public void AttachedChildFollowsTheBoneEachTick()
    {
        var (skeleton, socket) = BuildSkeletonWithSocket(new Vector3(2, 0, 0));
        var mesh = new SkinnedMeshComponent { Mesh = new SkinnedMesh { Skeleton = skeleton, MeshId = 0 } };
        var child = new TestMeshComponent();

        child.AttachTo(mesh, "socket");
        mesh.Update(1f / 60f);

        Assert.That(child.GetTransform(Space.World).Position, Is.EqualTo(new Vector3(2, 0, 0)));

        socket.LocalTransform = new Transform { Position = new Vector3(9, 0, 0) };
        mesh.Update(1f / 60f);

        Assert.That(child.GetTransform(Space.World).Position, Is.EqualTo(new Vector3(9, 0, 0)),
            "the attached child re-resolves against the bone every tick, not just when it was first attached");
    }
}
