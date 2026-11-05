using System.Numerics;
using Rin.World.Ecs.Scheduling;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Mesh;
using Rin.World.Mesh.Skinning;
using Rin.World.Mesh.Skinning.Animation;

namespace Examples.AssetViewer;

/// <summary>
///     Draws one skinned mesh at the origin and plays its animation graph. Stands in for an ECS skinned mesh component
///     until the ECS has one.
/// </summary>
internal sealed class AnimatedModel : FrameSystem
{
    private readonly IRenderSystem _renderSystem;
    private readonly AnimationGraph _graph;
    private readonly RenderProxyHandle _proxy;

    public AnimatedModel(IRenderSystem renderSystem, IMesh mesh, Skeleton skeleton, AnimationGraph graph)
    {
        _renderSystem = renderSystem;
        _graph = graph;

        var surfaceCount = mesh.GetSurfaces().Length;
        var surfaceIndices = new int[surfaceCount];
        var materials = new IMeshMaterial[surfaceCount];
        for (var i = 0; i < surfaceCount; i++)
        {
            surfaceIndices[i] = i;
            materials[i] = DefaultMeshMaterial.DefaultMesh;
        }

        _proxy = renderSystem.CreateSkinnedMeshProxy(new SkinnedMeshProxyDesc
        {
            Skeleton = skeleton,
            Pose = graph.GetPose(),
            Mesh = mesh,
            Transform = Matrix4x4.Identity,
            SurfaceIndices = surfaceIndices,
            Materials = materials
        });
    }

    public override void Update(in FrameContext context)
    {
        _graph.Tick(context.DeltaSeconds);
        _graph.ReleaseNotifies();
        _renderSystem.UpdateSkinnedProxyPose(_proxy, _graph.GetPose());
    }
}
