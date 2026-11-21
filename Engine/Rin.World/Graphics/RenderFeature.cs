using System.Numerics;
using Rin.Core.Extensions;
using Rin.World.Animation;
using Rin.World.Scheduling;
using Rin.World.Transforms;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.ImageBasedLighting;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics;

/// <summary>
///     Connects an <see cref="World" /> to an <see cref="IRenderSystem" />. Hooks create a render proxy and a
///     <see cref="RenderProxy" /> component when a <see cref="StaticMesh" />, <see cref="SkinnedMesh" />,
///     <see cref="PointLight" />, <see cref="DirectionalLight" /> or <see cref="EnvironmentLight" /> is added and destroy both when it is removed or
///     despawned. <see cref="FramePhase.Update" /> systems advance pose sources and bone sockets, and a
///     <see cref="FramePhase.Extract" /> system added after transform propagation pushes the changes every frame.
/// </summary>
/// <remarks>
///     An entity holds at most one of those components; use child entities for more. Adding a mesh component whose
///     handle is not a live mesh throws. A snapshot restore destroys every proxy and recreates one, at the restored
///     world pose, for each restored entity whose mesh is still live.
/// </remarks>
public sealed class RenderFeature : IWorldRestoreListener
{
    private readonly IMeshFactory _meshFactory;
    private readonly Query<RenderProxy> _proxies;
    private readonly List<Entity> _lostProxies = [];

    public RenderFeature(World world, IRenderSystem renderSystem, IMeshFactory meshFactory)
    {
        World = world;
        RenderSystem = renderSystem;
        _meshFactory = meshFactory;

        world.OnAdd<StaticMesh>(CreateStaticMeshProxy);
        world.OnAdd<SkinnedMesh>(CreateSkinnedMeshProxy);
        world.OnAdd<PointLight>(CreatePointLightProxy);
        world.OnAdd<DirectionalLight>(CreateDirectionalLightProxy);
        world.OnAdd<EnvironmentLight>(CreateEnvironmentLightProxy);
        world.OnRemove<StaticMesh>(DestroyProxy);
        world.OnRemove<SkinnedMesh>(DestroyProxy);
        world.OnRemove<PointLight>(DestroyProxy);
        world.OnRemove<DirectionalLight>(DestroyProxy);
        world.OnRemove<EnvironmentLight>(DestroyProxy);
        world.AddSystem(FramePhase.Update, new AnimationSystem(world, PoseSources));
        world.AddSystem(FramePhase.Update, new BoneSocketSync(world, PoseSources));
        world.AddSystem(FramePhase.Extract, new RenderExtract(world, renderSystem, PoseSources, Environments));
        world.AddRestoreListener(this);
        _proxies = world.Query<RenderProxy>();
    }

    public World World { get; }

    public IRenderSystem RenderSystem { get; }

    public MaterialSets Materials { get; } = new();

    public PoseSources PoseSources { get; } = new();

    public EnvironmentMaps Environments { get; } = new();

    /// <summary>
    ///     Parents <paramref name="child" /> to <paramref name="parent" /> and attaches it to the named bone of the
    ///     parent's <see cref="SkinnedMesh" /> pose. A structural change, so not for use inside a query loop.
    /// </summary>
    public void AttachToBone(Entity child, Entity parent, string boneName)
    {
        if (!World.Has<SkinnedMesh>(parent))
            throw new InvalidOperationException($"{parent} has no {nameof(SkinnedMesh)} to attach to.");

        var poseSource = PoseSources.Entry(World.Get<SkinnedMesh>(parent).PoseSource);
        if (!poseSource.Source.Skeleton.BoneNameToIndex.TryGetValue(boneName, out var boneIndex))
            throw new ArgumentException($"The skeleton of {parent} has no bone named '{boneName}'.", nameof(boneName));

        World.SetParent(child, parent);
        World.Add(child, new AttachSocket(boneIndex) { SocketMatrix = poseSource.BoneMatrices[boneIndex] });
    }

    /// <summary>
    ///     Stops <paramref name="child" /> following a bone. It stays parented and is placed relative to its parent again.
    /// </summary>
    public void DetachFromBone(Entity child) => World.Remove<AttachSocket>(child);

    void IWorldRestoreListener.BeforeRestore()
    {
        foreach (var (proxies, _) in _proxies)
            for (var i = 0; i < proxies.Length; i++)
                if (proxies[i].Handle.IsValid) RenderSystem.DestroyProxy(proxies[i].Handle);
    }

    void IWorldRestoreListener.AfterRestore()
    {
        foreach (var chunk in _proxies)
            for (var i = 0; i < chunk.Length; i++)
            {
                var entity = chunk.Entities[i];
                if (TryRecreateProxy(entity, out var handle))
                    chunk.Write<RenderProxy>(i) = new RenderProxy { Handle = handle };
                else
                    _lostProxies.Add(entity);
            }

        foreach (var entity in _lostProxies) World.Remove<RenderProxy>(entity);
        _lostProxies.Clear();
    }

    private bool TryRecreateProxy(Entity entity, out RenderProxyHandle handle)
    {
        var matrix = CurrentWorldMatrix(entity);
        if (World.Has<StaticMesh>(entity)) handle = CreateStaticMeshHandle(World.Get<StaticMesh>(entity), matrix);
        else if (World.Has<SkinnedMesh>(entity)) handle = CreateSkinnedMeshHandle(World.Get<SkinnedMesh>(entity), matrix);
        else if (World.Has<PointLight>(entity)) handle = CreateLightHandle(World.Get<PointLight>(entity), matrix);
        else if (World.Has<DirectionalLight>(entity)) handle = CreateLightHandle(World.Get<DirectionalLight>(entity), matrix);
        else if (World.Has<EnvironmentLight>(entity)) handle = CreateEnvironmentHandle(World.Get<EnvironmentLight>(entity), matrix);
        else handle = default;
        return handle.IsValid;
    }

    private void CreateStaticMeshProxy(Entity entity, in StaticMesh staticMesh)
    {
        EnsureNoProxy(entity);
        AddMeshProxy(entity, CreateStaticMeshHandle(staticMesh, CurrentWorldMatrix(entity)));
    }

    private void CreateSkinnedMeshProxy(Entity entity, in SkinnedMesh skinnedMesh)
    {
        EnsureNoProxy(entity);
        if (!skinnedMesh.PoseSource.IsValid)
            throw new InvalidOperationException($"The {nameof(SkinnedMesh)} of {entity} needs a pose source.");

        AddMeshProxy(entity, CreateSkinnedMeshHandle(skinnedMesh, CurrentWorldMatrix(entity)));
    }

    private void CreatePointLightProxy(Entity entity, in PointLight _)
    {
        EnsureNoProxy(entity);
        AddProxy(entity, CreateLightHandle(World.Get<PointLight>(entity), CurrentWorldMatrix(entity)));
    }

    private void CreateDirectionalLightProxy(Entity entity, in DirectionalLight _)
    {
        EnsureNoProxy(entity);
        AddProxy(entity, CreateLightHandle(World.Get<DirectionalLight>(entity), CurrentWorldMatrix(entity)));
    }

    private void CreateEnvironmentLightProxy(Entity entity, in EnvironmentLight _)
    {
        EnsureNoProxy(entity);
        var handle = CreateEnvironmentHandle(World.Get<EnvironmentLight>(entity), CurrentWorldMatrix(entity));
        if (!handle.IsValid)
            throw new InvalidOperationException($"The map of {entity} is not a live map of {nameof(Environments)}.");

        AddProxy(entity, handle);
    }

    private RenderProxyHandle CreateStaticMeshHandle(in StaticMesh staticMesh, in Matrix4x4 transform)
    {
        if (_meshFactory.GetMesh(staticMesh.Mesh) is not { } mesh) return default;

        var (surfaceIndices, materials) = ResolveSurfaceMaterials(mesh, Materials.Get(staticMesh.Materials));
        return RenderSystem.CreateStaticMeshProxy(new StaticMeshProxyDesc
        {
            Mesh = mesh,
            Transform = transform,
            SurfaceIndices = surfaceIndices,
            Materials = materials
        });
    }

    private RenderProxyHandle CreateSkinnedMeshHandle(in SkinnedMesh skinnedMesh, in Matrix4x4 transform)
    {
        if (_meshFactory.GetMesh(skinnedMesh.Mesh) is not { } mesh) return default;

        var poseSource = PoseSources.Get(skinnedMesh.PoseSource);
        var (surfaceIndices, materials) = ResolveSurfaceMaterials(mesh, Materials.Get(skinnedMesh.Materials));
        return RenderSystem.CreateSkinnedMeshProxy(new SkinnedMeshProxyDesc
        {
            Mesh = mesh,
            Skeleton = poseSource.Skeleton,
            Pose = poseSource.GetPose(),
            Transform = transform,
            SurfaceIndices = surfaceIndices,
            Materials = materials
        });
    }

    private RenderProxyHandle CreateLightHandle(in PointLight light, in Matrix4x4 transform) =>
        RenderSystem.CreateLightProxy(light.ToLightInfo(transform));

    private RenderProxyHandle CreateLightHandle(in DirectionalLight light, in Matrix4x4 transform) =>
        RenderSystem.CreateLightProxy(light.ToLightInfo(transform));

    private RenderProxyHandle CreateEnvironmentHandle(in EnvironmentLight light, in Matrix4x4 transform) =>
        Environments.TryDescribe(light, transform, out var volume) ? RenderSystem.CreateEnvironmentProxy(volume) : default;

    private void DestroyProxy<T>(Entity entity, in T component) where T : unmanaged, IComponentData
    {
        if (!World.Has<RenderProxy>(entity)) return;
        RenderSystem.DestroyProxy(World.Get<RenderProxy>(entity).Handle);
        World.Remove<RenderProxy>(entity);
    }

    private void EnsureNoProxy(Entity entity)
    {
        if (World.Has<RenderProxy>(entity))
            throw new InvalidOperationException($"{entity} already has a render proxy. Put each mesh or light on its own entity.");
    }

    private void AddMeshProxy(Entity entity, RenderProxyHandle handle)
    {
        if (!handle.IsValid)
            throw new InvalidOperationException($"The mesh of {entity} is not a live mesh of the mesh factory.");

        AddProxy(entity, handle);
    }

    private void AddProxy(Entity entity, RenderProxyHandle handle) => World.Add(entity, new RenderProxy { Handle = handle });

    // The stored matrix is stale until propagation runs, and a proxy created from it would interpolate from that pose.
    private Matrix4x4 CurrentWorldMatrix(Entity entity)
    {
        if (World.Has<LocalTransform>(entity)) return World.ComputeWorldMatrix(entity);
        return World.Has<WorldTransform>(entity) ? World.Get<WorldTransform>(entity).Matrix : Matrix4x4.Identity;
    }

    private static (int[] SurfaceIndices, IMeshMaterial[] Materials) ResolveSurfaceMaterials(IMesh mesh,
        IMeshMaterial?[] materials)
    {
        var surfaceCount = mesh.GetSurfaces().Length;
        var surfaceIndices = new int[surfaceCount];
        var resolved = new IMeshMaterial[surfaceCount];
        IMeshMaterial? previous = null;
        for (var i = 0; i < surfaceCount; i++)
        {
            surfaceIndices[i] = i;
            previous = resolved[i] = materials.TryGet(i) ?? previous ?? DefaultMaterials.Pbr;
        }

        return (surfaceIndices, resolved);
    }
}
