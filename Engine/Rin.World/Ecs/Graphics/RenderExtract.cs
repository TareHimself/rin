using System.Numerics;
using Rin.World.Ecs.Animation;
using Rin.World.Ecs.Scheduling;
using Rin.World.Ecs.Transforms;
using Rin.World.Graphics;

namespace Rin.World.Ecs.Graphics;

/// <summary>
///     Pushes mesh transforms whose <see cref="WorldTransform.Version" /> changed, every skinned pose, dirty or moved
///     lights, and the world's interpolation alpha to the render system.
/// </summary>
internal sealed class RenderExtract : FrameSystem
{
    private readonly EntityWorld _world;
    private readonly IRenderSystem _renderSystem;
    private readonly PoseSources _poseSources;
    private readonly Query<StaticMesh, WorldTransform, RenderProxy> _staticMeshes;
    private readonly Query<SkinnedMesh, WorldTransform, RenderProxy> _skinnedMeshTransforms;
    private readonly Query<SkinnedMesh, RenderProxy> _skinnedMeshPoses;
    private readonly Query<PointLight, RenderProxy> _pointLights;
    private readonly Query<DirectionalLight, RenderProxy> _directionalLights;

    public RenderExtract(EntityWorld world, IRenderSystem renderSystem, PoseSources poseSources)
    {
        _world = world;
        _renderSystem = renderSystem;
        _poseSources = poseSources;
        _staticMeshes = world.Query<StaticMesh, WorldTransform, RenderProxy>();
        _skinnedMeshTransforms = world.Query<SkinnedMesh, WorldTransform, RenderProxy>();
        _skinnedMeshPoses = world.Query<SkinnedMesh, RenderProxy>();
        _pointLights = world.Query<PointLight, RenderProxy>();
        _directionalLights = world.Query<DirectionalLight, RenderProxy>();
    }

    public override void Update(in FrameContext context)
    {
        PushMeshTransforms(_staticMeshes);
        PushMeshTransforms(_skinnedMeshTransforms);
        PushSkinnedPoses();
        PushPointLights();
        PushDirectionalLights();
        _renderSystem.SetInterpolationAlpha(context.InterpolationAlpha);
    }

    private void PushMeshTransforms<TMesh>(Query<TMesh, WorldTransform, RenderProxy> meshes)
        where TMesh : struct, IComponentData
    {
        foreach (var (_, transforms, proxies) in meshes)
            for (var i = 0; i < transforms.Length; i++)
            {
                ref readonly var transform = ref transforms[i];
                ref var proxy = ref proxies[i];
                if (transform.Version == proxy.LastPushedVersion) continue;

                _renderSystem.UpdateProxyTransform(proxy.Handle, transform.Matrix);
                proxy.LastPushedVersion = transform.Version;
            }
    }

    // Poses have no cheap change check, so every one is pushed each frame.
    private void PushSkinnedPoses()
    {
        foreach (var (meshes, proxies) in _skinnedMeshPoses)
            for (var i = 0; i < meshes.Length; i++)
                _renderSystem.UpdateSkinnedProxyPose(proxies[i].Handle, _poseSources.Get(meshes[i].PoseSource).GetPose());
    }

    private void PushPointLights()
    {
        foreach (var (lights, proxies, entities) in _pointLights)
            for (var i = 0; i < lights.Length; i++)
            {
                ref var light = ref lights[i];
                ref var proxy = ref proxies[i];
                var (matrix, version) = TransformOf(entities[i]);
                if (!light.IsDirty && version == proxy.LastPushedVersion) continue;

                _renderSystem.UpdateLightProxy(proxy.Handle, light.ToLightInfo(matrix));
                light.IsDirty = false;
                proxy.LastPushedVersion = version;
            }
    }

    private void PushDirectionalLights()
    {
        foreach (var (lights, proxies, entities) in _directionalLights)
            for (var i = 0; i < lights.Length; i++)
            {
                ref var light = ref lights[i];
                ref var proxy = ref proxies[i];
                var (matrix, version) = TransformOf(entities[i]);
                if (!light.IsDirty && version == proxy.LastPushedVersion) continue;

                _renderSystem.UpdateLightProxy(proxy.Handle, light.ToLightInfo(matrix));
                light.IsDirty = false;
                proxy.LastPushedVersion = version;
            }
    }

    // Looked up per light so a light without transform components still pushes its property changes.
    private (Matrix4x4 Matrix, uint Version) TransformOf(Entity entity)
    {
        if (!_world.Has<WorldTransform>(entity)) return (Matrix4x4.Identity, 0);
        ref readonly var transform = ref _world.Get<WorldTransform>(entity);
        return (transform.Matrix, transform.Version);
    }
}
