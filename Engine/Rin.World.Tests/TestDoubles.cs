using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared;
using Rin.Core.Shared.Math;
using Rin.World.Components;
using Rin.World.Graphics;
using Rin.World.Graphics.Mesh;
using Rin.World.Math;
using Rin.World.Physics;

namespace Rin.World.Tests;

internal sealed class FakeMesh : IMesh
{
    public ulong GetVertexFormatSize() => throw new NotSupportedException();
    public MeshSurface[] GetSurfaces() => [];
    public MeshSurface GetSurface(int surfaceIndex) => throw new NotSupportedException();
    public DeviceBufferView GetVertices() => default;
    public DeviceBufferView GetVertices(int surfaceIndex) => default;
    public uint GetVertexCount() => 0;
    public uint GetVertexCount(int surfaceIndex) => 0;
    public DeviceBufferView GetIndices() => default;
    public Bounds3D GetBounds() => default;
}

/// <summary>
///     Mirrors <c>StaticMeshComponent</c>'s proxy push idiom without the <c>IMeshFactory</c> singleton.
/// </summary>
internal sealed class TestMeshComponent : WorldComponent
{
    private static readonly IMesh Mesh = new FakeMesh();

    private RenderProxyHandle _proxy = RenderProxyHandle.Invalid;
    private uint _lastPushedVersion;

    public RenderProxyHandle Proxy => _proxy;

    public override void Start()
    {
        base.Start();
        _proxy = Owner!.World!.RenderSystem.CreateStaticMeshProxy(new StaticMeshProxyDesc
        {
            Mesh = Mesh,
            Transform = GetTransform(Space.World).ToMatrix(),
            SurfaceIndices = [],
            Materials = []
        });
        _lastPushedVersion = TransformVersion;
    }

    public override void Stop()
    {
        if (_proxy.IsValid)
        {
            Owner!.World!.RenderSystem.DestroyProxy(_proxy);
            _proxy = RenderProxyHandle.Invalid;
        }

        base.Stop();
    }

    public override void LateUpdate(float deltaSeconds)
    {
        base.LateUpdate(deltaSeconds);
        if (!_proxy.IsValid) return;
        var worldTransform = GetTransform(Space.World);
        if (TransformVersion != _lastPushedVersion)
        {
            Owner!.World!.RenderSystem.UpdateProxyTransform(_proxy, worldTransform.ToMatrix());
            _lastPushedVersion = TransformVersion;
        }
    }
}

internal sealed class FakeRenderSystem : IRenderSystem
{
    private uint _nextIndex;
    private readonly HashSet<RenderProxyHandle> _live = [];

    public List<(RenderProxyHandle Handle, Matrix4x4 Transform)> PushedTransforms { get; } = [];
    public int DestroyCount { get; private set; }

    public RenderProxyHandle CreateStaticMeshProxy(in StaticMeshProxyDesc desc) => Create();
    public RenderProxyHandle CreateSkinnedMeshProxy(in SkinnedMeshProxyDesc desc) => Create();
    public RenderProxyHandle CreateLightProxy(in LightInfo desc) => Create();

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

    public void UpdateStaticMeshProxy(RenderProxyHandle handle, in StaticMeshProxyDesc desc)
    {
    }

    public void UpdateLightProxy(RenderProxyHandle handle, in LightInfo desc)
    {
    }

    public void DestroyProxy(RenderProxyHandle handle)
    {
        _live.Remove(handle);
        DestroyCount++;
    }

    public IWorldRenderContext Snapshot(CameraComponent view, in Extent2D extent) => throw new NotSupportedException();
    public void Build(IGraphBuilder builder, IWorldRenderContext context) => throw new NotSupportedException();
    public void Dispose()
    {
    }
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

        public float ApproxRadius => float.Max(BoxSize.Length() / 2f, float.Max(SphereRadius, CapsuleRadius + CapsuleHalfHeight));
    }

    private uint _nextIndex;
    private Vector3 _gravity = new(0, -9.81f, 0);
    private readonly Dictionary<PhysicsBodyHandle, Body> _bodies = [];

    /// <summary>
    ///     The most recently created body's handle — enough for the single-body tests here.
    /// </summary>
    public PhysicsBodyHandle LastCreated { get; private set; }

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

    public void Update(float deltaTime) => OnUpdate?.Invoke(deltaTime);
    public void Destroy() => _bodies.Clear();

    // Approximate: every body is treated as a bounding sphere. Good enough for a fake; BepuPhysicsSystem
    // has the real implementation, exercised directly in PhysicsQueryTests.
    public RayCastResult? RayCast(in Vector3 begin, in Vector3 direction, float distance, int channel = -1)
    {
        var all = RayCastAll(begin, direction, distance, channel);
        return all.Length == 0 ? null : all.OrderBy(r => r.Distance).First();
    }

    public RayCastResult[] RayCastAll(in Vector3 begin, in Vector3 direction, float distance, int channel = -1)
    {
        var dir = Vector3.Normalize(direction);
        List<RayCastResult> results = [];
        foreach (var (handle, body) in _bodies)
        {
            if (channel != -1 && body.CollisionChannel != channel) continue;
            var toBody = body.Position - begin;
            var t = Vector3.Dot(toBody, dir);
            if (t < 0 || t > distance) continue;
            var closest = begin + dir * t;
            if (Vector3.Distance(closest, body.Position) > body.ApproxRadius) continue;
            results.Add(new RayCastResult { Body = handle, Location = closest, Normal = Vector3.Normalize(closest - body.Position), Distance = t });
        }

        return results.ToArray();
    }

    public RayCastResult? SphereCast(float radius, in Vector3 begin, in Vector3 direction, float distance, int channel = -1) =>
        RayCast(begin, direction, distance, channel);

    public RayCastResult[] SphereCastAll(float radius, in Vector3 begin, in Vector3 direction, float distance, int channel = -1) =>
        RayCastAll(begin, direction, distance, channel);

    public RayCastResult? BoxCast(in Vector3 size, in Vector3 begin, in Quaternion orientation, in Vector3 direction, float distance, int channel = -1) =>
        RayCast(begin, direction, distance, channel);

    public RayCastResult[] BoxCastAll(in Vector3 size, in Vector3 begin, in Quaternion orientation, in Vector3 direction, float distance, int channel = -1) =>
        RayCastAll(begin, direction, distance, channel);

    public RayCastResult? CapsuleCast(float radius, float halfHeight, in Vector3 begin, in Quaternion orientation, in Vector3 direction, float distance, int channel = -1) =>
        RayCast(begin, direction, distance, channel);

    public RayCastResult[] CapsuleCastAll(float radius, float halfHeight, in Vector3 begin, in Quaternion orientation, in Vector3 direction, float distance, int channel = -1) =>
        RayCastAll(begin, direction, distance, channel);

    public PhysicsBodyHandle[] OverlapSphere(float radius, in Vector3 center, int channel = -1)
    {
        var capturedCenter = center;
        return _bodies.Where(kv => (channel == -1 || kv.Value.CollisionChannel == channel) && Vector3.Distance(capturedCenter, kv.Value.Position) <= radius + kv.Value.ApproxRadius)
            .Select(kv => kv.Key).ToArray();
    }

    public PhysicsBodyHandle[] OverlapBox(in Vector3 size, in Vector3 center, in Quaternion orientation, int channel = -1) =>
        OverlapSphere(size.Length() / 2f, center, channel);

    public PhysicsBodyHandle[] OverlapCapsule(float radius, float halfHeight, in Vector3 center, in Quaternion orientation, int channel = -1) =>
        OverlapSphere(radius + halfHeight, center, channel);
}
