using System.Runtime.InteropServices;
using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Shared;
using Rin.Core.Shared.Buffers;
using Rin.Core.Shared.Math;
using Rin.World.Graphics;
using Rin.World.Graphics.ImageBasedLighting;
using Rin.World.Graphics.Mesh;
using Rin.World.Mesh.Skinning;
using Rin.World.Mesh.Skinning.Animation;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;
using Rin.World.Transforms;

namespace Rin.World.Tests;

internal sealed class FakeMesh : IMesh
{
    public MeshSurface[] Surfaces { get; init; } = [];

    public MeshSurface[] GetSurfaces() => Surfaces;
    public MeshSurface GetSurface(int surfaceIndex) => throw new NotSupportedException();
    public DeviceBufferView GetVertices() => default;
    public DeviceBufferView GetVertices(int surfaceIndex) => default;
    public uint GetVertexCount(int surfaceIndex) => 0;
    public DeviceBufferView GetIndices() => default;
    public Bounds3D GetBounds() => default;
}

internal sealed class FakeMeshMaterial : IMeshMaterial
{
    public IMaterialPass ColorPass => throw new NotSupportedException();
    public IMaterialPass DepthPass => throw new NotSupportedException();
}

internal sealed class FakeMeshFactory : IMeshFactory
{
    private readonly Dictionary<MeshHandle, IMesh> _meshes = [];
    private uint _nextIndex;

    public MeshHandle Register(IMesh mesh)
    {
        var handle = new MeshHandle(_nextIndex++, 1);
        _meshes.Add(handle, mesh);
        return handle;
    }

    public IMesh? GetMesh(MeshHandle mesh) => _meshes.GetValueOrDefault(mesh);

    public MeshHandle CreateMesh<TVertexFormat>(ReadOnlySpan<TVertexFormat> vertices, ReadOnlySpan<uint> indices,
        MeshSurface[] surfaces) where TVertexFormat : unmanaged, IVertex => throw new NotSupportedException();

    public void FreeMeshes(params ReadOnlySpan<MeshHandle> meshes)
    {
        foreach (var mesh in meshes) _meshes.Remove(mesh);
    }

    public void Dispose()
    {
    }
}

internal sealed class FakePoseSource : IPoseSource
{
    public required Skeleton Skeleton { get; init; }
    public SkeletalPose Pose;
    public int TickCount { get; private set; }
    public float TickedSeconds { get; private set; }

    public SkeletalPose GetPose() => Pose;

    public void Tick(float deltaSeconds)
    {
        TickCount++;
        TickedSeconds += deltaSeconds;
    }
}

internal sealed class FakeRenderSystem : IRenderSystem
{
    private uint _nextIndex;
    private readonly HashSet<RenderProxyHandle> _live = [];

    public List<(RenderProxyHandle Handle, Matrix4x4 Transform)> PushedTransforms { get; } = [];
    public List<(RenderProxyHandle Handle, SkeletalPose Pose)> PushedPoses { get; } = [];
    public List<(RenderProxyHandle Handle, LightInfo Light)> PushedLights { get; } = [];
    public List<(RenderProxyHandle Handle, StaticMeshProxyDesc Desc)> CreatedStaticMeshes { get; } = [];
    public List<(RenderProxyHandle Handle, SkinnedMeshProxyDesc Desc)> CreatedSkinnedMeshes { get; } = [];
    public List<(RenderProxyHandle Handle, LightInfo Light)> CreatedLights { get; } = [];
    public List<(RenderProxyHandle Handle, EnvironmentVolume Volume)> CreatedEnvironments { get; } = [];
    public List<(RenderProxyHandle Handle, EnvironmentVolume Volume)> PushedEnvironments { get; } = [];
    public List<RenderProxyHandle> Destroyed { get; } = [];
    public int DestroyCount => Destroyed.Count;
    public int LiveCount => _live.Count;
    public float LastInterpolationAlpha { get; private set; } = 1f;
    public float LastTime { get; private set; }

    public RenderProxyHandle CreateStaticMeshProxy(in StaticMeshProxyDesc desc)
    {
        var handle = Create();
        CreatedStaticMeshes.Add((handle, desc));
        return handle;
    }

    public RenderProxyHandle CreateSkinnedMeshProxy(in SkinnedMeshProxyDesc desc)
    {
        var handle = Create();
        CreatedSkinnedMeshes.Add((handle, desc));
        return handle;
    }

    public RenderProxyHandle CreateLightProxy(in LightInfo desc)
    {
        var handle = Create();
        CreatedLights.Add((handle, desc));
        return handle;
    }

    public RenderProxyHandle CreateEnvironmentProxy(in EnvironmentVolume desc)
    {
        var handle = Create();
        CreatedEnvironments.Add((handle, desc));
        return handle;
    }

    public void UpdateEnvironmentProxy(RenderProxyHandle handle, in EnvironmentVolume desc)
    {
        if (!_live.Contains(handle)) throw new InvalidOperationException("Stale or unknown proxy handle");
        PushedEnvironments.Add((handle, desc));
    }

    private RenderProxyHandle Create()
    {
        var handle = new RenderProxyHandle(++_nextIndex, 1);
        _live.Add(handle);
        return handle;
    }

    public void UpdateProxyTransform(RenderProxyHandle handle, in Matrix4x4 worldTransform)
    {
        if (!_live.Contains(handle)) throw new InvalidOperationException("Stale or unknown proxy handle");
        PushedTransforms.Add((handle, worldTransform));
    }

    public void UpdateSkinnedProxyPose(RenderProxyHandle handle, in SkeletalPose pose)
    {
        if (!_live.Contains(handle)) throw new InvalidOperationException("Stale or unknown proxy handle");
        PushedPoses.Add((handle, pose));
    }

    public void UpdateLightProxy(RenderProxyHandle handle, in LightInfo desc)
    {
        if (!_live.Contains(handle)) throw new InvalidOperationException("Stale or unknown proxy handle");
        PushedLights.Add((handle, desc));
    }

    public void DestroyProxy(RenderProxyHandle handle)
    {
        if (!_live.Remove(handle)) throw new InvalidOperationException("Stale or unknown proxy handle");
        Destroyed.Add(handle);
    }

    public void SetInterpolationAlpha(float alpha) => LastInterpolationAlpha = alpha;
    public void SetTime(float seconds) => LastTime = seconds;

    public IWorldCollectedData Snapshot(in CameraView view, in Extent2D extent) => throw new NotSupportedException();
    public void Dispose()
    {
    }
}

/// <summary>
///     A world with a <see cref="RenderFeature" /> on fake render and mesh backends.
/// </summary>
public abstract class RenderFeatureFixture
{
    private protected World _world = null!;
    private protected FakeRenderSystem _render = null!;
    private protected FakeMeshFactory _meshes = null!;
    private protected RenderFeature _feature = null!;

    [SetUp]
    public void CreateRenderFeature()
    {
        _world = new World();
        _render = new FakeRenderSystem();
        _meshes = new FakeMeshFactory();
        _feature = new RenderFeature(_world, _render, _meshes);
    }

    [TearDown]
    public void DisposeRenderBackends()
    {
        _render.Dispose();
        _meshes.Dispose();
    }

    private protected Entity SpawnMesh(Vector3 position)
    {
        var entity = _world.Spawn();
        _world.SetPosition(entity, position);
        _world.Add(entity, new StaticMesh { Mesh = _meshes.Register(new FakeMesh()) });
        return entity;
    }
}

/// <summary>
///     A world with a <see cref="PhysicsFeature" /> on a <see cref="FakePhysicsSystem" />.
/// </summary>
public abstract class PhysicsFeatureFixture
{
    private protected World _world = null!;
    private protected FakePhysicsSystem _physics = null!;
    private protected PhysicsFeature _feature = null!;

    [SetUp]
    public void CreatePhysicsFeature()
    {
        _world = new World();
        _physics = new FakePhysicsSystem();
        _feature = new PhysicsFeature(_world, _physics);
    }

    private protected PhysicsBodyHandle HandleOf(Entity entity) => _world.Get<PhysicsBody>(entity).Handle;

    private protected void Tick() => _world.Update(_world.TickInterval);
}

/// <summary>
///     Gives each test a real <see cref="BepuPhysicsSystem" /> and destroys it afterwards, which stops its worker threads.
/// </summary>
public abstract class BepuFixture
{
    private protected BepuPhysicsSystem Bepu { get; private set; } = null!;

    [SetUp]
    public void CreateBepu()
    {
        Bepu = new BepuPhysicsSystem();
    }

    [TearDown]
    public void DestroyBepu()
    {
        Bepu.Destroy();
    }

    private protected static Transform At(Vector3 position) =>
        new() { Position = position, Orientation = Quaternion.Identity, Scale = Vector3.One };
}

internal sealed class FakePhysicsSystem : IPhysicsSystem
{
    private sealed class Body
    {
        public Vector3 Position;
        public Quaternion Orientation = Quaternion.Identity;
        public Vector3 Scale = Vector3.One;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
        public float Mass = 1f;
        public PhysicsState State;
        public Vector3 BoxSize;
        public float SphereRadius;
        public float CapsuleRadius;
        public float CapsuleHalfHeight;
        public int CollisionChannel;
        public bool IsTrigger;
    }

    private uint _nextIndex;
    private Vector3 _gravity = new(0, -9.81f, 0);
    private readonly Dictionary<PhysicsBodyHandle, Body> _bodies = [];

    /// <summary>
    ///     The most recently created body's handle, enough for the single-body tests here.
    /// </summary>
    public PhysicsBodyHandle LastCreated { get; private set; }

    public int BodyCount => _bodies.Count;

    /// <summary>
    ///     Stands in for a real physics step; tests call <see cref="SetPosition" /> etc. from here.
    /// </summary>
    public Action<float>? OnUpdate { get; set; }

    public Vector3 GetGravity() => _gravity;
    public void SetGravity(in Vector3 gravity) => _gravity = gravity;

    public PhysicsBodyHandle CreateBox(in Vector3 size, in Transform transform, PhysicsState state)
    {
        var capturedSize = size;
        return Create(transform, state, body => body.BoxSize = capturedSize);
    }

    public PhysicsBodyHandle CreateSphere(float radius, in Transform transform, PhysicsState state) =>
        Create(transform, state, body => body.SphereRadius = radius);

    public PhysicsBodyHandle CreateCapsule(float radius, float halfHeight, in Transform transform, PhysicsState state) =>
        Create(transform, state, body =>
        {
            body.CapsuleRadius = radius;
            body.CapsuleHalfHeight = halfHeight;
        });

    public PhysicsBodyHandle CreateStaticMesh(ReadOnlySpan<Vector3> vertices, ReadOnlySpan<uint> indices,
        in Transform transform) =>
        Create(transform, PhysicsState.Static, _ => { });

    public PhysicsBodyHandle CreateConvexHull(ReadOnlySpan<Vector3> points, in Transform transform, PhysicsState state) =>
        Create(transform, state, _ => { });

    private PhysicsBodyHandle Create(in Transform transform, PhysicsState state, Action<Body> configure)
    {
        var handle = new PhysicsBodyHandle(++_nextIndex, 1);
        var body = new Body { Position = transform.Position, Orientation = transform.Orientation, Scale = transform.Scale, State = state };
        configure(body);
        _bodies[handle] = body;
        LastCreated = handle;
        return handle;
    }

    public void DestroyBody(PhysicsBodyHandle handle) => _bodies.Remove(handle);

    private Body Get(PhysicsBodyHandle handle) => _bodies[handle];

    public Vector3 GetLinearVelocity(PhysicsBodyHandle handle) => Get(handle).LinearVelocity;
    public void SetLinearVelocity(PhysicsBodyHandle handle, in Vector3 velocity) => Get(handle).LinearVelocity = velocity;
    public Vector3 GetAngularVelocity(PhysicsBodyHandle handle) => Get(handle).AngularVelocity;
    public void SetAngularVelocity(PhysicsBodyHandle handle, in Vector3 velocity) => Get(handle).AngularVelocity = velocity;
    public Vector3 GetPosition(PhysicsBodyHandle handle) => Get(handle).Position;
    public void SetPosition(PhysicsBodyHandle handle, in Vector3 position) => Get(handle).Position = position;
    public Quaternion GetOrientation(PhysicsBodyHandle handle) => Get(handle).Orientation;
    public void SetOrientation(PhysicsBodyHandle handle, in Quaternion orientation) => Get(handle).Orientation = orientation;
    public Vector3 GetScale(PhysicsBodyHandle handle) => Get(handle).Scale;
    public void SetScale(PhysicsBodyHandle handle, in Vector3 scale) => Get(handle).Scale = scale;
    public PhysicsState GetState(PhysicsBodyHandle handle) => Get(handle).State;
    public void SetState(PhysicsBodyHandle handle, PhysicsState state) => Get(handle).State = state;
    public float GetMass(PhysicsBodyHandle handle) => Get(handle).Mass;
    public void SetMass(PhysicsBodyHandle handle, float mass) => Get(handle).Mass = mass;

    public Vector3 GetBoxSize(PhysicsBodyHandle handle) => Get(handle).BoxSize;
    public void SetBoxSize(PhysicsBodyHandle handle, in Vector3 size) => Get(handle).BoxSize = size;
    public float GetSphereRadius(PhysicsBodyHandle handle) => Get(handle).SphereRadius;
    public void SetSphereRadius(PhysicsBodyHandle handle, float radius) => Get(handle).SphereRadius = radius;
    public float GetCapsuleRadius(PhysicsBodyHandle handle) => Get(handle).CapsuleRadius;
    public float GetCapsuleHalfHeight(PhysicsBodyHandle handle) => Get(handle).CapsuleHalfHeight;
    public void SetCapsuleRadius(PhysicsBodyHandle handle, float radius) => Get(handle).CapsuleRadius = radius;
    public void SetCapsuleHalfHeight(PhysicsBodyHandle handle, float halfHeight) => Get(handle).CapsuleHalfHeight = halfHeight;

    private readonly Dictionary<PhysicsBodyHandle, float> _timeScales = [];

    public void SetTimeScale(PhysicsBodyHandle handle, float scale) => _timeScales[handle] = scale;
    public float GetTimeScale(PhysicsBodyHandle handle) => _timeScales.GetValueOrDefault(handle, 1f);

    public int GetCollisionChannel(PhysicsBodyHandle handle) => Get(handle).CollisionChannel;
    public void SetCollisionChannel(PhysicsBodyHandle handle, int channel) => Get(handle).CollisionChannel = channel;
    public bool GetTrigger(PhysicsBodyHandle handle) => Get(handle).IsTrigger;
    public void SetTrigger(PhysicsBodyHandle handle, bool trigger) => Get(handle).IsTrigger = trigger;

    private readonly List<PhysicsContactEvent> _contactEvents = [];

    public ReadOnlySpan<PhysicsContactEvent> ContactEvents => CollectionsMarshal.AsSpan(_contactEvents);

    /// <summary>
    ///     Queues a contact event for the next <see cref="Update" /> to report, since the fake has no narrow phase.
    /// </summary>
    public void EmitContact(PhysicsBodyHandle a, PhysicsBodyHandle b, PhysicsContactKind kind, bool trigger) =>
        _pendingContacts.Add(new PhysicsContactEvent(a, b, kind, trigger));

    private readonly List<PhysicsContactEvent> _pendingContacts = [];

    public void Update(float deltaTime)
    {
        _contactEvents.Clear();
        _contactEvents.AddRange(_pendingContacts);
        _pendingContacts.Clear();
        OnUpdate?.Invoke(deltaTime);
    }

    public void Destroy() => _bodies.Clear();

    private static NotSupportedException NoQueries() => new("FakePhysicsSystem has no queries, use BepuPhysicsSystem");

    public RayCastResult? RayCast(in Vector3 begin, in Vector3 direction, float distance, int channel = -1) =>
        throw NoQueries();

    public void RayCastAll<THandler>(in Vector3 begin, in Vector3 direction, float distance, ref THandler handler, int channel = -1)
        where THandler : struct, IPhysicsHitHandler =>
        throw NoQueries();

    public RayCastResult? SphereCast(float radius, in Vector3 begin, in Vector3 direction, float distance, int channel = -1) =>
        throw NoQueries();

    public void SphereCastAll<THandler>(float radius, in Vector3 begin, in Vector3 direction, float distance, ref THandler handler, int channel = -1)
        where THandler : struct, IPhysicsHitHandler =>
        throw NoQueries();

    public RayCastResult? BoxCast(in Vector3 size, in Vector3 begin, in Quaternion orientation, in Vector3 direction, float distance, int channel = -1) =>
        throw NoQueries();

    public void BoxCastAll<THandler>(in Vector3 size, in Vector3 begin, in Quaternion orientation, in Vector3 direction, float distance, ref THandler handler, int channel = -1)
        where THandler : struct, IPhysicsHitHandler =>
        throw NoQueries();

    public RayCastResult? CapsuleCast(float radius, float halfHeight, in Vector3 begin, in Quaternion orientation, in Vector3 direction, float distance, int channel = -1) =>
        throw NoQueries();

    public void CapsuleCastAll<THandler>(float radius, float halfHeight, in Vector3 begin, in Quaternion orientation, in Vector3 direction, float distance, ref THandler handler, int channel = -1)
        where THandler : struct, IPhysicsHitHandler =>
        throw NoQueries();

    public void OverlapSphere<THandler>(float radius, in Vector3 center, ref THandler handler, int channel = -1)
        where THandler : struct, IPhysicsOverlapHandler =>
        throw NoQueries();

    public void OverlapBox<THandler>(in Vector3 size, in Vector3 center, in Quaternion orientation, ref THandler handler, int channel = -1)
        where THandler : struct, IPhysicsOverlapHandler =>
        throw NoQueries();

    public void OverlapCapsule<THandler>(float radius, float halfHeight, in Vector3 center, in Quaternion orientation, ref THandler handler, int channel = -1)
        where THandler : struct, IPhysicsOverlapHandler =>
        throw NoQueries();
}

internal record struct Health(int Value) : IComponentData;

internal record struct Speed(float Value) : IComponentData;

internal record struct Armor(int Value) : IComponentData;

internal record struct Ammo(int Value) : IComponentData;

internal struct Stunned : ITagData;

internal static class TestCameras
{
    public static CameraView Identity => new(Matrix4x4.Identity, 90f, 0.01f, 50f);
}

internal static class TestSkeletons
{
    public const string SocketBone = "hand";

    public static Skeleton WithSocket(Vector3 socketOffset)
    {
        var root = new Bone { Name = "root", LocalTransform = new Transform() };
        var socket = new Bone { Name = SocketBone, Parent = root, LocalTransform = new Transform { Position = socketOffset } };
        root.Children = [socket];
        return new Skeleton([root, socket]);
    }
}

internal sealed class FakePoseNode(int boneCount, params (int Index, Transform Value)[] overrides) : IPoseNode
{
    public int EvaluateCount { get; private set; }
    public List<AnimationNotifyStateRange> ActiveStates { get; } = [];
    public List<AnimationNotify> CrossedNotifies { get; } = [];

    public PooledSkeletalPose Evaluate(in AnimationEvalContext ctx)
    {
        EvaluateCount++;
        if (ctx.Notifies is { } sink)
        {
            sink.ReportSource(this, ctx.Weight);
            foreach (var notify in CrossedNotifies) sink.ReportCrossed(notify, this);
        }
        if (ctx.ActiveNotifyStates is { } active)
            foreach (var range in ActiveStates)
                active.Add(range);

        var pose = new PooledSkeletalPose(boneCount);
        foreach (var (index, value) in overrides) pose.Set(index, value);
        return pose;
    }
}

internal sealed class RecordingNotify(string name) : IAnimationNotify
{
    public string Name => name;
    public int ResetCount { get; private set; }
    public int NotifyCount { get; private set; }
    public Entity LastEntity { get; private set; }

    public void Reset()
    {
        ResetCount++;
    }

    public void Notify(World world, Entity entity, AnimationGraph graph)
    {
        NotifyCount++;
        LastEntity = entity;
    }
}

internal sealed class SingleInstanceNotifyFactory(IAnimationNotify instance) : IAnimationNotifyFactory
{
    public int CreateCount { get; private set; }
    public int ReleaseCount { get; private set; }

    public IAnimationNotify Create()
    {
        CreateCount++;
        return instance;
    }

    public void Release(IAnimationNotify released)
    {
        ReleaseCount++;
    }
}

internal sealed class RecordingNotifyState(string name) : IAnimationNotifyState
{
    public string Name => name;
    public int ResetCount { get; private set; }
    public int BeginCount { get; private set; }
    public int EndCount { get; private set; }

    public void Reset()
    {
        ResetCount++;
    }

    public void NotifyBegin(World world, Entity entity, AnimationGraph graph)
    {
        BeginCount++;
    }

    public void NotifyEnd(World world, Entity entity, AnimationGraph graph)
    {
        EndCount++;
    }
}

internal sealed class SingleInstanceNotifyStateFactory(IAnimationNotifyState instance) : IAnimationNotifyStateFactory
{
    public int CreateCount { get; private set; }
    public int ReleaseCount { get; private set; }

    public IAnimationNotifyState Create()
    {
        CreateCount++;
        return instance;
    }

    public void Release(IAnimationNotifyState released)
    {
        ReleaseCount++;
    }
}

internal static class TestNotify
{
    public static (AnimationNotify Notify, RecordingNotify Handler) At(float time, string name,
        bool dominantClipOnly = false)
    {
        var handler = new RecordingNotify(name);
        return (new AnimationNotify(time, new SingleInstanceNotifyFactory(handler), dominantClipOnly), handler);
    }

    public static (AnimationNotifyStateRange Range, RecordingNotifyState Handler, SingleInstanceNotifyStateFactory Factory) StateAt(
        float start, float end, string name)
    {
        var handler = new RecordingNotifyState(name);
        var factory = new SingleInstanceNotifyStateFactory(handler);
        return (new AnimationNotifyStateRange(start, end, factory), handler, factory);
    }
}
