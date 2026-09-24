using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.World.Components;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Tests.Graphics;

public class SkinnedProxyPoseTests
{
    private static (Skeleton Skeleton, RenderProxyHandle Handle) CreateProxy(DefaultRenderSystem render)
    {
        var skeleton = new Skeleton([new Bone { Name = "root" }]);
        var handle = render.CreateSkinnedMeshProxy(new SkinnedMeshProxyDesc
        {
            Skeleton = skeleton,
            Pose = skeleton.BasePose,
            Mesh = new FakeMesh(),
            Transform = Matrix4x4.Identity,
            SurfaceIndices = [],
            Materials = []
        });
        render.Snapshot(new CameraComponent(), new Extent2D(1, 1));
        return (skeleton, handle);
    }

    [Test]
    public void ReplacesTheStalePoseFromStart()
    {
        var render = new DefaultRenderSystem();
        var (skeleton, handle) = CreateProxy(render);

        var pushed = new SkeletalPose(skeleton.Bones.Length);
        pushed.Set(0, new Transform { Position = new Vector3(1, 2, 3) });
        render.UpdateSkinnedProxyPose(handle, pushed);

        var context = (DefaultWorldCollectedData)render.Snapshot(new CameraComponent(), new Extent2D(1, 1));

        Assert.That(context.SkinnedGeometry[0].Pose.IsSet(0), Is.True,
            "before this method existed, a proxy's pose was frozen at whatever Start() pushed and never refreshed");
        Assert.That(context.SkinnedGeometry[0].Pose.BoneTransforms[0].Position, Is.EqualTo(new Vector3(1, 2, 3)));
    }
}
