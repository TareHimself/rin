using System.Numerics;
using Rin.World.Animation;
using Rin.World.Graphics.ImageBasedLighting;
using Rin.World.Scheduling;
using Rin.World.Transforms;

namespace Rin.World.Graphics;

/// <summary>
///     Pushes mesh transforms and lights written since its last run, every skinned pose, and the world's interpolation
///     alpha to the render system.
/// </summary>
/// <remarks>
///     Every query but the poses is a <c>Changed</c> query, so a still scene costs one tick compare per archetype. A
///     light with a <see cref="WorldTransform" /> is pushed when the light or the transform changed; one without is
///     placed at the origin and pushed when the light changed.
/// </remarks>
internal sealed class RenderExtract : FrameSystem
{
    private readonly IRenderSystem _renderSystem;
    private readonly PoseSources _poseSources;
    private readonly EnvironmentMaps _environments;
    private readonly Query<StaticMesh, WorldTransform, RenderProxy> _staticMeshes;
    private readonly Query<SkinnedMesh, WorldTransform, RenderProxy> _skinnedMeshTransforms;
    private readonly Query<SkinnedMesh, RenderProxy> _skinnedMeshPoses;
    private readonly Query<PointLight, WorldTransform, RenderProxy> _placedPointLights;
    private readonly Query<PointLight, RenderProxy> _unplacedPointLights;
    private readonly Query<DirectionalLight, WorldTransform, RenderProxy> _placedDirectionalLights;
    private readonly Query<DirectionalLight, RenderProxy> _unplacedDirectionalLights;
    private readonly Query<EnvironmentLight, WorldTransform, RenderProxy> _placedEnvironmentLights;
    private readonly Query<EnvironmentLight, RenderProxy> _unplacedEnvironmentLights;
    private float _time;

    public RenderExtract(World world, IRenderSystem renderSystem, PoseSources poseSources,
        EnvironmentMaps environments)
    {
        _renderSystem = renderSystem;
        _poseSources = poseSources;
        _environments = environments;
        _staticMeshes = world.Query<StaticMesh, WorldTransform, RenderProxy>().Changed<WorldTransform>();
        _skinnedMeshTransforms = world.Query<SkinnedMesh, WorldTransform, RenderProxy>().Changed<WorldTransform>();
        _skinnedMeshPoses = world.Query<SkinnedMesh, RenderProxy>();
        _placedPointLights = world.Query<PointLight, WorldTransform, RenderProxy>()
            .Changed<PointLight>().Changed<WorldTransform>();
        _unplacedPointLights = world.Query<PointLight, RenderProxy>()
            .Without<WorldTransform>().Changed<PointLight>();
        _placedDirectionalLights = world.Query<DirectionalLight, WorldTransform, RenderProxy>()
            .Changed<DirectionalLight>().Changed<WorldTransform>();
        _unplacedDirectionalLights = world.Query<DirectionalLight, RenderProxy>()
            .Without<WorldTransform>().Changed<DirectionalLight>();
        _placedEnvironmentLights = world.Query<EnvironmentLight, WorldTransform, RenderProxy>()
            .Changed<EnvironmentLight>().Changed<WorldTransform>();
        _unplacedEnvironmentLights = world.Query<EnvironmentLight, RenderProxy>()
            .Without<WorldTransform>().Changed<EnvironmentLight>();
    }

    public override void Update(in FrameContext context)
    {
        _renderSystem.BeginFrame(context.TicksRun > 0);
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
        where TMesh : unmanaged, IComponentData
    {
        foreach (var chunk in meshes)
            for (var i = 0; i < chunk.Length; i++)
                if (chunk.HasChanged<WorldTransform>(i))
                    _renderSystem.UpdateProxyTransform(chunk.Components3[i].Handle, chunk.Components2[i].Matrix);
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
        foreach (var chunk in _placedPointLights)
            for (var i = 0; i < chunk.Length; i++)
                if (chunk.HasChanged<PointLight>(i) || chunk.HasChanged<WorldTransform>(i))
                    _renderSystem.UpdateLightProxy(chunk.Components3[i].Handle,
                        chunk.Components1[i].ToLightInfo(chunk.Components2[i].Matrix));

        foreach (var chunk in _unplacedPointLights)
            for (var i = 0; i < chunk.Length; i++)
                if (chunk.HasChanged<PointLight>(i))
                    _renderSystem.UpdateLightProxy(chunk.Components2[i].Handle,
                        chunk.Components1[i].ToLightInfo(Matrix4x4.Identity));
    }

    private void PushDirectionalLights()
    {
        foreach (var chunk in _placedDirectionalLights)
            for (var i = 0; i < chunk.Length; i++)
                if (chunk.HasChanged<DirectionalLight>(i) || chunk.HasChanged<WorldTransform>(i))
                    _renderSystem.UpdateLightProxy(chunk.Components3[i].Handle,
                        chunk.Components1[i].ToLightInfo(chunk.Components2[i].Matrix));

        foreach (var chunk in _unplacedDirectionalLights)
            for (var i = 0; i < chunk.Length; i++)
                if (chunk.HasChanged<DirectionalLight>(i))
                    _renderSystem.UpdateLightProxy(chunk.Components2[i].Handle,
                        chunk.Components1[i].ToLightInfo(Matrix4x4.Identity));
    }

    private void PushEnvironmentLights()
    {
        foreach (var chunk in _placedEnvironmentLights)
            for (var i = 0; i < chunk.Length; i++)
                if (chunk.HasChanged<EnvironmentLight>(i) || chunk.HasChanged<WorldTransform>(i))
                    PushEnvironmentLight(chunk.Components3[i].Handle, chunk.Components1[i], chunk.Components2[i].Matrix);

        foreach (var chunk in _unplacedEnvironmentLights)
            for (var i = 0; i < chunk.Length; i++)
                if (chunk.HasChanged<EnvironmentLight>(i))
                    PushEnvironmentLight(chunk.Components2[i].Handle, chunk.Components1[i], Matrix4x4.Identity);
    }

    private void PushEnvironmentLight(RenderProxyHandle handle, in EnvironmentLight light, in Matrix4x4 world)
    {
        if (_environments.TryDescribe(light, world, out var volume)) _renderSystem.UpdateEnvironmentProxy(handle, volume);
    }
}
