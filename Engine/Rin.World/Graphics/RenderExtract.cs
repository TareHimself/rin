using System.Numerics;
using Rin.World.Animation;
using Rin.World.Graphics.ImageBasedLighting;
using Rin.World.Scheduling;
using Rin.World.Transforms;

namespace Rin.World.Graphics;

/// <summary>
///     Pushes mesh transforms whose <see cref="WorldTransform.Version" /> changed, every skinned pose, dirty or moved
///     lights and environment lights, and the world's interpolation alpha to the render system.
/// </summary>
internal sealed class RenderExtract : FrameSystem
{
    private readonly World _world;
    private readonly IRenderSystem _renderSystem;
    private readonly PoseSources _poseSources;
    private readonly Query<StaticMesh, WorldTransform, RenderProxy> _staticMeshes;
    private readonly Query<SkinnedMesh, WorldTransform, RenderProxy> _skinnedMeshTransforms;
    private readonly Query<SkinnedMesh, RenderProxy> _skinnedMeshPoses;
    private readonly Query<PointLight, RenderProxy> _pointLights;
    private readonly Query<DirectionalLight, RenderProxy> _directionalLights;
    private readonly Query<EnvironmentLight, RenderProxy> _environmentLights;
    private readonly EnvironmentMaps _environments;
    private float _time;

    public RenderExtract(World world, IRenderSystem renderSystem, PoseSources poseSources,
        EnvironmentMaps environments)
    {
        _environments = environments;
        _environmentLights = world.Query<EnvironmentLight, RenderProxy>();
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
        PushEnvironmentLights();
        _renderSystem.SetInterpolationAlpha(context.InterpolationAlpha);
        _time += context.DeltaSeconds;
        _renderSystem.SetTime(_time);
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

    private void PushEnvironmentLights()
    {
        foreach (var (lights, proxies, entities) in _environmentLights)
            for (var i = 0; i < lights.Length; i++)
            {
                ref var light = ref lights[i];
                ref var proxy = ref proxies[i];
                var (matrix, version) = TransformOf(entities[i]);
                if (!light.IsDirty && version == proxy.LastPushedVersion) continue;

                if (_environments.TryDescribe(light, matrix, out var volume))
                    _renderSystem.UpdateEnvironmentProxy(proxy.Handle, volume);
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
