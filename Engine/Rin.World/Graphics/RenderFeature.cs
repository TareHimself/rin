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
///     mesh factory when its component is added gets no proxy.
/// </remarks>
public sealed class RenderFeature
{
    private readonly IMeshFactory _meshFactory;

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

    private void CreateStaticMeshProxy(Entity entity, ref StaticMesh staticMesh)
    {
        EnsureNoProxy(entity);
        if (_meshFactory.GetMesh(staticMesh.MeshId) is not { } mesh) return;

        var (surfaceIndices, materials) = ResolveSurfaceMaterials(mesh, Materials.Get(staticMesh.Materials));
        var transform = CurrentWorldTransform(entity);
        var handle = RenderSystem.CreateStaticMeshProxy(new StaticMeshProxyDesc
        {
            Mesh = mesh,
            Transform = transform.Matrix,
            SurfaceIndices = surfaceIndices,
            Materials = materials
        });
        AddProxy(entity, handle, transform.Version);
    }

    private void CreateSkinnedMeshProxy(Entity entity, ref SkinnedMesh skinnedMesh)
    {
        EnsureNoProxy(entity);
        if (!skinnedMesh.PoseSource.IsValid)
            throw new InvalidOperationException($"The {nameof(SkinnedMesh)} of {entity} needs a pose source.");
        if (_meshFactory.GetMesh(skinnedMesh.MeshId) is not { } mesh) return;

        var poseSource = PoseSources.Get(skinnedMesh.PoseSource);
        var (surfaceIndices, materials) = ResolveSurfaceMaterials(mesh, Materials.Get(skinnedMesh.Materials));
        var transform = CurrentWorldTransform(entity);
        var handle = RenderSystem.CreateSkinnedMeshProxy(new SkinnedMeshProxyDesc
        {
            Mesh = mesh,
            Skeleton = poseSource.Skeleton,
            Pose = poseSource.GetPose(),
            Transform = transform.Matrix,
            SurfaceIndices = surfaceIndices,
            Materials = materials
        });
        AddProxy(entity, handle, transform.Version);
    }

    private void CreatePointLightProxy(Entity entity, ref PointLight light)
    {
        EnsureNoProxy(entity);
        var transform = CurrentWorldTransform(entity);
        var handle = RenderSystem.CreateLightProxy(light.ToLightInfo(transform.Matrix));
        light.IsDirty = false;
        AddProxy(entity, handle, transform.Version);
    }

    private void CreateDirectionalLightProxy(Entity entity, ref DirectionalLight light)
    {
        EnsureNoProxy(entity);
        var transform = CurrentWorldTransform(entity);
        var handle = RenderSystem.CreateLightProxy(light.ToLightInfo(transform.Matrix));
        light.IsDirty = false;
        AddProxy(entity, handle, transform.Version);
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
            previous = resolved[i] = materials.TryGet(i) ?? previous ?? DefaultMeshMaterial.DefaultMesh;
        }

        return (surfaceIndices, resolved);
    }
}
