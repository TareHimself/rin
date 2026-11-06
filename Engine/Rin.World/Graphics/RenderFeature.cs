using System.Numerics;
using Rin.Core.Extensions;
using Rin.World.Animation;
using Rin.World.Scheduling;
using Rin.World.Transforms;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics;

/// <summary>
///     Connects an <see cref="World" /> to an <see cref="IRenderSystem" />. Hooks create a render proxy and a
///     <see cref="RenderProxy" /> component when a <see cref="StaticMesh" />, <see cref="SkinnedMesh" />,
///     <see cref="PointLight" /> or <see cref="DirectionalLight" /> is added and destroy both when it is removed or
///     despawned. <see cref="FramePhase.Update" /> systems advance pose sources and bone sockets, and a
///     <see cref="FramePhase.Extract" /> system added after transform propagation pushes the changes every frame.
/// </summary>
/// <remarks>
///     An entity holds at most one of those components; use child entities for more. A mesh that is not ready in the
///     mesh factory when its component is added gets no proxy. A snapshot restore destroys every proxy and recreates one,
///     at the restored world pose, for each restored entity that had one.
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
        world.OnRemove<StaticMesh>(DestroyProxy);
        world.OnRemove<SkinnedMesh>(DestroyProxy);
        world.OnRemove<PointLight>(DestroyProxy);
        world.OnRemove<DirectionalLight>(DestroyProxy);
        world.AddSystem(FramePhase.Update, new AnimationSystem(world, PoseSources));
        world.AddSystem(FramePhase.Update, new BoneSocketSync(world, PoseSources));
        world.AddSystem(FramePhase.Extract, new RenderExtract(world, renderSystem, PoseSources));
        world.AddRestoreListener(this);
        _proxies = world.Query<RenderProxy>();
    }

    public World World { get; }

    public IRenderSystem RenderSystem { get; }

    public MaterialSets Materials { get; } = new();

    public PoseSources PoseSources { get; } = new();

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

    void IWorldRestoreListener.BeforeRestore()
    {
        foreach (var (proxies, _) in _proxies)
            for (var i = 0; i < proxies.Length; i++)
                if (proxies[i].Handle.IsValid) RenderSystem.DestroyProxy(proxies[i].Handle);
    }

    void IWorldRestoreListener.AfterRestore()
    {
        foreach (var (proxies, entities) in _proxies)
            for (var i = 0; i < proxies.Length; i++)
            {
                var entity = entities[i];
                if (TryRecreateProxy(entity, out var handle, out var version))
                    proxies[i] = new RenderProxy { Handle = handle, LastPushedVersion = version };
                else
                    _lostProxies.Add(entity);
            }

        foreach (var entity in _lostProxies) World.Remove<RenderProxy>(entity);
        _lostProxies.Clear();
    }

    private bool TryRecreateProxy(Entity entity, out RenderProxyHandle handle, out uint version)
    {
        var transform = CurrentWorldTransform(entity);
        version = transform.Version;
        if (World.Has<StaticMesh>(entity)) handle = CreateStaticMeshHandle(World.Get<StaticMesh>(entity), transform.Matrix);
        else if (World.Has<SkinnedMesh>(entity)) handle = CreateSkinnedMeshHandle(World.Get<SkinnedMesh>(entity), transform.Matrix);
        else if (World.Has<PointLight>(entity)) handle = CreateLightHandle(ref World.Get<PointLight>(entity), transform.Matrix);
        else if (World.Has<DirectionalLight>(entity)) handle = CreateLightHandle(ref World.Get<DirectionalLight>(entity), transform.Matrix);
        else handle = default;
        return handle.IsValid;
    }

    private void CreateStaticMeshProxy(Entity entity, ref StaticMesh staticMesh)
    {
        EnsureNoProxy(entity);
        var transform = CurrentWorldTransform(entity);
        var handle = CreateStaticMeshHandle(staticMesh, transform.Matrix);
        if (handle.IsValid) AddProxy(entity, handle, transform.Version);
    }

    private void CreateSkinnedMeshProxy(Entity entity, ref SkinnedMesh skinnedMesh)
    {
        EnsureNoProxy(entity);
        if (!skinnedMesh.PoseSource.IsValid)
            throw new InvalidOperationException($"The {nameof(SkinnedMesh)} of {entity} needs a pose source.");

        var transform = CurrentWorldTransform(entity);
        var handle = CreateSkinnedMeshHandle(skinnedMesh, transform.Matrix);
        if (handle.IsValid) AddProxy(entity, handle, transform.Version);
    }

    private void CreatePointLightProxy(Entity entity, ref PointLight light)
    {
        EnsureNoProxy(entity);
        var transform = CurrentWorldTransform(entity);
        AddProxy(entity, CreateLightHandle(ref light, transform.Matrix), transform.Version);
    }

    private void CreateDirectionalLightProxy(Entity entity, ref DirectionalLight light)
    {
        EnsureNoProxy(entity);
        var transform = CurrentWorldTransform(entity);
        AddProxy(entity, CreateLightHandle(ref light, transform.Matrix), transform.Version);
    }

    private RenderProxyHandle CreateStaticMeshHandle(in StaticMesh staticMesh, in Matrix4x4 transform)
    {
        if (_meshFactory.GetMesh(staticMesh.MeshId) is not { } mesh) return default;

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
        if (_meshFactory.GetMesh(skinnedMesh.MeshId) is not { } mesh) return default;

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

    private RenderProxyHandle CreateLightHandle(ref PointLight light, in Matrix4x4 transform)
    {
        light.IsDirty = false;
        return RenderSystem.CreateLightProxy(light.ToLightInfo(transform));
    }

    private RenderProxyHandle CreateLightHandle(ref DirectionalLight light, in Matrix4x4 transform)
    {
        light.IsDirty = false;
        return RenderSystem.CreateLightProxy(light.ToLightInfo(transform));
    }

    private void DestroyProxy<T>(Entity entity, ref T component) where T : struct, IComponentData
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

    private void AddProxy(Entity entity, RenderProxyHandle handle, uint transformVersion) =>
        World.Add(entity, new RenderProxy { Handle = handle, LastPushedVersion = transformVersion });

    // The stored matrix is stale until propagation runs, and a proxy created from it would interpolate from that pose.
    private WorldTransform CurrentWorldTransform(Entity entity)
    {
        var transform = World.Has<WorldTransform>(entity) ? World.Get<WorldTransform>(entity) : new WorldTransform();
        if (World.Has<LocalTransform>(entity)) transform.Matrix = World.ComputeWorldMatrix(entity);
        return transform;
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
