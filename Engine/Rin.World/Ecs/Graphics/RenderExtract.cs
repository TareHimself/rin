using System.Numerics;
using Rin.World.Ecs.Scheduling;
using Rin.World.Ecs.Transforms;
using Rin.World.Graphics;

namespace Rin.World.Ecs.Graphics;

/// <summary>
///     Pushes mesh transforms whose <see cref="WorldTransform.Version" /> changed, dirty or moved lights, and the world's
///     interpolation alpha to the render system.
/// </summary>
internal sealed class RenderExtract : FrameSystem
{
    private readonly EntityWorld _world;
    private readonly IRenderSystem _renderSystem;
    private readonly Query<StaticMesh, WorldTransform, RenderProxy> _meshes;
    private readonly Query<PointLight, RenderProxy> _pointLights;
    private readonly Query<DirectionalLight, RenderProxy> _directionalLights;

    public RenderExtract(EntityWorld world, IRenderSystem renderSystem)
    {
        _world = world;
        _renderSystem = renderSystem;
        _meshes = world.Query<StaticMesh, WorldTransform, RenderProxy>();
        _pointLights = world.Query<PointLight, RenderProxy>();
        _directionalLights = world.Query<DirectionalLight, RenderProxy>();
    }

    public override void Update(in FrameContext context)
    {
        PushMeshTransforms();
        PushPointLights();
        PushDirectionalLights();
        _renderSystem.SetInterpolationAlpha(context.InterpolationAlpha);
    }

    private void PushMeshTransforms()
    {
        foreach (var (_, transforms, proxies) in _meshes)
            for (var i = 0; i < transforms.Length; i++)
            {
                ref readonly var transform = ref transforms[i];
                ref var proxy = ref proxies[i];
                if (transform.Version == proxy.LastPushedVersion) continue;

                _renderSystem.UpdateProxyTransform(proxy.Handle, transform.Matrix);
                proxy.LastPushedVersion = transform.Version;
            }
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
