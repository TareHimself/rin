using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using JetBrains.Annotations;
using Rin.Core.Shared.Math;

namespace Rin.World.Physics.Bepu;

public class BepuPhysicsSystem : IPhysicsSystem
{
    private readonly BufferPool _pool = new();
    private readonly ThreadDispatcher _threadDispatcher = new(int.Max(1, Environment.ProcessorCount - 1));

    private readonly List<BepuBody?> _slots = [];
    private readonly List<uint> _versions = [];
    private readonly Stack<uint> _freeIndices = new();
    private readonly Dictionary<int, TimeDilation> _timeDilation = [];
    private readonly HashSet<int> _dilatedBodiesSeenThisStep = [];
    private readonly Dictionary<int, BepuBody> _bodyHandleLookup = [];
    private readonly Dictionary<int, BepuBody> _staticHandleLookup = [];
    private Vector3 _gravity = new(0, -9.81f, 0);
    private readonly ContactTracker _contacts;

    public BepuPhysicsSystem()
    {
        _contacts = new ContactTracker(this, _threadDispatcher.ThreadCount);
        Simulation = Simulation.Create(_pool, new NarrowPhaseCallbacks(this), new PoseIntegratorCallbacks(this),
            new SolveDescription(VelocityIterationCount, SubstepCount));
    }

    public ReadOnlySpan<PhysicsContactEvent> ContactEvents => _contacts.Events;

    public Simulation Simulation { get; set; }

    [PublicAPI] public int VelocityIterationCount { get; private set; } = 8;

    // Bepu's substepping is strong; one substep with 8 velocity iterations is the standard
    // high-quality setting. Raising this multiplies solver cost per timestep.
    [PublicAPI] public int SubstepCount { get; private set; } = 1;

    [PublicAPI]
    public AngularIntegrationMode AngularIntegrationMode { get; set; } = AngularIntegrationMode.ConserveMomentum;

    [PublicAPI] public bool AllowSubstepsForUnconstrainedBodies { get; set; } = true;

    [PublicAPI] public float LinearDamping { get; set; }

    [PublicAPI] public float AngularDamping { get; set; }

    public Vector3 GetGravity()
    {
        return _gravity;
    }

    public void SetGravity(in Vector3 gravity)
    {
        _gravity = gravity;
    }

    public PhysicsBodyHandle CreateBox(in Vector3 size, in Transform transform, PhysicsState state)
    {
        var body = new BepuBoxBody(state, transform, this, size);
        body.Init();
        return Register(body);
    }

    public PhysicsBodyHandle CreateSphere(float radius, in Transform transform, PhysicsState state)
    {
        var body = new BepuSphereBody(state, transform, this, radius);
        body.Init();
        return Register(body);
    }

    public PhysicsBodyHandle CreateCapsule(float radius, float halfHeight, in Transform transform, PhysicsState state)
    {
        var body = new BepuCapsuleBody(state, transform, this, radius, halfHeight);
        body.Init();
        return Register(body);
    }

    public PhysicsBodyHandle CreateStaticMesh(ReadOnlySpan<Vector3> vertices, ReadOnlySpan<uint> indices,
        in Transform transform)
    {
        var body = new BepuStaticMeshBody(transform, this, vertices, indices);
        body.Init();
        return Register(body);
    }

    public PhysicsBodyHandle CreateConvexHull(ReadOnlySpan<Vector3> points, in Transform transform, PhysicsState state)
    {
        var body = BepuConvexHullBody.Create(state, transform, this, points);
        body.Init();
        return Register(body);
    }

    public void DestroyBody(PhysicsBodyHandle handle)
    {
        var index = handle.Index;
        if (index >= _slots.Count || _versions[(int)index] != handle.Version || _slots[(int)index] is not { } body)
            return;

        if (body.BodyHandle is { } bodyHandle)
        {
            _timeDilation.Remove(bodyHandle.Value);
            _bodyHandleLookup.Remove(bodyHandle.Value);
        }

        if (body.StaticHandle is { } staticHandle) _staticHandleLookup.Remove(staticHandle.Value);
        body.Dispose();
        _slots[(int)index] = null;
        _versions[(int)index]++;
        _freeIndices.Push(index);
    }

    public Vector3 GetLinearVelocity(PhysicsBodyHandle handle)
    {
        return Resolve(handle).GetLinearVelocity();
    }

    public void SetLinearVelocity(PhysicsBodyHandle handle, in Vector3 velocity)
    {
        Resolve(handle).SetLinearVelocity(velocity);
    }

    public Vector3 GetAngularVelocity(PhysicsBodyHandle handle)
    {
        return Resolve(handle).GetAngularVelocity();
    }

    public void SetAngularVelocity(PhysicsBodyHandle handle, in Vector3 velocity)
    {
        Resolve(handle).SetAngularVelocity(velocity);
    }

    public Vector3 GetPosition(PhysicsBodyHandle handle)
    {
        return Resolve(handle).GetPosition();
    }

    public void SetPosition(PhysicsBodyHandle handle, in Vector3 position)
    {
        Resolve(handle).SetPosition(position);
    }

    public Quaternion GetOrientation(PhysicsBodyHandle handle)
    {
        return Resolve(handle).GetOrientation();
    }

    public void SetOrientation(PhysicsBodyHandle handle, in Quaternion orientation)
    {
        Resolve(handle).SetOrientation(orientation);
    }

    public Vector3 GetScale(PhysicsBodyHandle handle)
    {
        return Resolve(handle).GetScale();
    }

    public void SetScale(PhysicsBodyHandle handle, in Vector3 scale)
    {
        Resolve(handle).SetScale(scale);
    }

    public PhysicsState GetState(PhysicsBodyHandle handle)
    {
        return Resolve(handle).GetState();
    }

    public void SetState(PhysicsBodyHandle handle, PhysicsState state)
    {
        Resolve(handle).SetState(state);
    }

    public float GetMass(PhysicsBodyHandle handle)
    {
        return Resolve(handle).GetMass();
    }

    public void SetMass(PhysicsBodyHandle handle, float mass)
    {
        Resolve(handle).SetMass(mass);
    }

    public Vector3 GetBoxSize(PhysicsBodyHandle handle)
    {
        return Resolve<BepuBoxBody>(handle).GetSize();
    }

    public void SetBoxSize(PhysicsBodyHandle handle, in Vector3 size)
    {
        Resolve<BepuBoxBody>(handle).SetSize(size);
    }

    public float GetSphereRadius(PhysicsBodyHandle handle)
    {
        return Resolve<BepuSphereBody>(handle).GetRadius();
    }

    public void SetSphereRadius(PhysicsBodyHandle handle, float radius)
    {
        Resolve<BepuSphereBody>(handle).SetRadius(radius);
    }

    public float GetCapsuleRadius(PhysicsBodyHandle handle)
    {
        return Resolve<BepuCapsuleBody>(handle).GetRadius();
    }

    public float GetCapsuleHalfHeight(PhysicsBodyHandle handle)
    {
        return Resolve<BepuCapsuleBody>(handle).GetHalfHeight();
    }

    public void SetCapsuleRadius(PhysicsBodyHandle handle, float radius)
    {
        Resolve<BepuCapsuleBody>(handle).SetRadius(radius);
    }

    public void SetCapsuleHalfHeight(PhysicsBodyHandle handle, float halfHeight)
    {
        Resolve<BepuCapsuleBody>(handle).SetHalfHeight(halfHeight);
    }

    public int GetCollisionChannel(PhysicsBodyHandle handle)
    {
        return Resolve(handle).CollisionChannel;
    }

    public void SetCollisionChannel(PhysicsBodyHandle handle, int channel)
    {
        Resolve(handle).CollisionChannel = channel;
    }

    public bool GetTrigger(PhysicsBodyHandle handle)
    {
        return Resolve(handle).IsTrigger;
    }

    public void SetTrigger(PhysicsBodyHandle handle, bool trigger)
    {
        Resolve(handle).IsTrigger = trigger;
    }

    public void SetTimeScale(PhysicsBodyHandle handle, float scale)
    {
        var body = Resolve(handle);
        if (body.BodyHandle is not { } bodyHandle) return;

        if (MathF.Abs(scale - 1f) < 1e-4f)
        {
            // Bepu's stored velocity was overwritten to the scaled value each step; restore the true one first.
            if (_timeDilation.Remove(bodyHandle.Value, out var restored))
            {
                body.SetLinearVelocity(restored.TrueLinearVelocity);
                body.SetAngularVelocity(restored.TrueAngularVelocity);
            }

            return;
        }

        if (!_timeDilation.TryGetValue(bodyHandle.Value, out var dilation))
        {
            dilation = new TimeDilation { TrueLinearVelocity = body.GetLinearVelocity(), TrueAngularVelocity = body.GetAngularVelocity() };
            _timeDilation[bodyHandle.Value] = dilation;
        }

        dilation.Scale = scale;
    }

    public float GetTimeScale(PhysicsBodyHandle handle)
    {
        var body = Resolve(handle);
        if (body.BodyHandle is { } bodyHandle && _timeDilation.TryGetValue(bodyHandle.Value, out var dilation))
            return dilation.Scale;
        return 1f;
    }

    public void Update(float deltaTime)
    {
        _dilatedBodiesSeenThisStep.Clear();
        _contacts.BeginStep();
        Simulation.Timestep(deltaTime, _threadDispatcher);
        _contacts.EndStep();
    }

    public void Destroy()
    {
        foreach (var body in _slots) body?.Dispose();
        _threadDispatcher.Dispose();
    }

    public RayCastResult? RayCast(in Vector3 begin, in Vector3 direction, float distance, int channel = -1)
    {
        if (distance <= 0f) return null;
        var handler = new ClosestRayHitHandler(this, channel);
        Simulation.RayCast(begin, direction, distance, ref handler);
        return handler.Hit ? handler.Result : null;
    }

    public void RayCastAll<THandler>(in Vector3 begin, in Vector3 direction, float distance, ref THandler handler,
        int channel = -1) where THandler : struct, IPhysicsHitHandler
    {
        if (distance <= 0f) return;
        var bepuHandler = new VisitingRayHitHandler<THandler>(this, channel) { Handler = handler };
        Simulation.RayCast(begin, direction, distance, ref bepuHandler);
        handler = bepuHandler.Handler;
    }

    public RayCastResult? SphereCast(float radius, in Vector3 begin, in Vector3 direction, float distance, int channel = -1)
    {
        return Cast(new Sphere(radius), begin, Quaternion.Identity, direction, distance, channel);
    }

    public void SphereCastAll<THandler>(float radius, in Vector3 begin, in Vector3 direction, float distance,
        ref THandler handler, int channel = -1) where THandler : struct, IPhysicsHitHandler
    {
        CastAll(new Sphere(radius), begin, Quaternion.Identity, direction, distance, ref handler, channel);
    }

    public RayCastResult? BoxCast(in Vector3 size, in Vector3 begin, in Quaternion orientation, in Vector3 direction,
        float distance, int channel = -1)
    {
        return Cast(new Box(size.X, size.Y, size.Z), begin, orientation, direction, distance, channel);
    }

    public void BoxCastAll<THandler>(in Vector3 size, in Vector3 begin, in Quaternion orientation, in Vector3 direction,
        float distance, ref THandler handler, int channel = -1) where THandler : struct, IPhysicsHitHandler
    {
        CastAll(new Box(size.X, size.Y, size.Z), begin, orientation, direction, distance, ref handler, channel);
    }

    public RayCastResult? CapsuleCast(float radius, float halfHeight, in Vector3 begin, in Quaternion orientation,
        in Vector3 direction, float distance, int channel = -1)
    {
        return Cast(new Capsule(radius, halfHeight * 2f), begin, orientation, direction, distance, channel);
    }

    public void CapsuleCastAll<THandler>(float radius, float halfHeight, in Vector3 begin, in Quaternion orientation,
        in Vector3 direction, float distance, ref THandler handler, int channel = -1) where THandler : struct, IPhysicsHitHandler
    {
        CastAll(new Capsule(radius, halfHeight * 2f), begin, orientation, direction, distance, ref handler, channel);
    }

    public void OverlapSphere<THandler>(float radius, in Vector3 center, ref THandler handler, int channel = -1)
        where THandler : struct, IPhysicsOverlapHandler
    {
        Overlap(new Sphere(radius), center, Quaternion.Identity, ref handler, channel);
    }

    public void OverlapBox<THandler>(in Vector3 size, in Vector3 center, in Quaternion orientation, ref THandler handler,
        int channel = -1) where THandler : struct, IPhysicsOverlapHandler
    {
        Overlap(new Box(size.X, size.Y, size.Z), center, orientation, ref handler, channel);
    }

    public void OverlapCapsule<THandler>(float radius, float halfHeight, in Vector3 center, in Quaternion orientation,
        ref THandler handler, int channel = -1) where THandler : struct, IPhysicsOverlapHandler
    {
        Overlap(new Capsule(radius, halfHeight * 2f), center, orientation, ref handler, channel);
    }

    private RayCastResult? Cast<TShape>(in TShape shape, in Vector3 begin, in Quaternion orientation, in Vector3 direction,
        float distance, int channel) where TShape : unmanaged, IConvexShape
    {
        if (distance <= 0f || direction == Vector3.Zero) return null;
        var pose = new RigidPose(begin, orientation);
        var velocity = new BodyVelocity(Vector3.Normalize(direction) * distance);
        var handler = new ClosestSweepHitHandler(this, channel);
        Simulation.Sweep(shape, pose, velocity, 1f, _pool, ref handler);
        return handler.Hit ? handler.Result : null;
    }

    private void CastAll<TShape, THandler>(in TShape shape, in Vector3 begin, in Quaternion orientation, in Vector3 direction,
        float distance, ref THandler handler, int channel)
        where TShape : unmanaged, IConvexShape
        where THandler : struct, IPhysicsHitHandler
    {
        if (distance <= 0f || direction == Vector3.Zero) return;
        var pose = new RigidPose(begin, orientation);
        var velocity = new BodyVelocity(Vector3.Normalize(direction) * distance);
        var bepuHandler = new VisitingSweepHitHandler<THandler>(this, channel) { Handler = handler };
        Simulation.Sweep(shape, pose, velocity, 1f, _pool, ref bepuHandler);
        handler = bepuHandler.Handler;
    }

    /// <summary>
    ///     A zero-length, zero-velocity sweep — Bepu's sweep dispatcher reports an overlap at the starting
    ///     pose via <see cref="ISweepHitHandler.OnHitAtZeroT" /> regardless of velocity, so this needs none
    ///     of the convenience <c>Sweep</c> overload's velocity-derived tuning (which divides by velocity
    ///     magnitude and would be a divide-by-zero here).
    /// </summary>
    private void Overlap<TShape, THandler>(in TShape shape, in Vector3 center, in Quaternion orientation, ref THandler handler,
        int channel)
        where TShape : unmanaged, IConvexShape
        where THandler : struct, IPhysicsOverlapHandler
    {
        var pose = new RigidPose(center, orientation);
        var bepuHandler = new VisitingOverlapHandler<THandler>(this, channel) { Handler = handler };
        Simulation.Sweep(shape, pose, default, 0f, _pool, ref bepuHandler, 0f, 0f, 1);
        handler = bepuHandler.Handler;
    }

    private struct ClosestRayHitHandler(BepuPhysicsSystem system, int channel) : IRayHitHandler
    {
        public bool Hit;
        public RayCastResult Result;

        public readonly bool AllowTest(CollidableReference collidable) => system.PassesChannelFilter(collidable, channel);
        public readonly bool AllowTest(CollidableReference collidable, int childIndex) => true;

        public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable,
            int childIndex)
        {
            if (system.ResolveCollidable(collidable) is not { } body) return;
            Hit = true;
            maximumT = t;
            Result = new RayCastResult { Body = body.Handle, Location = ray.Origin + ray.Direction * t, Normal = normal, Distance = t };
        }
    }

    /// <summary>
    ///     Wraps a caller's handler by value (Bepu's generics do not allow ref structs) and copies it back after the
    ///     query. Once the caller stops, every further collidable fails AllowTest, which ends the traversal.
    /// </summary>
    private struct VisitingRayHitHandler<THandler>(BepuPhysicsSystem system, int channel) : IRayHitHandler
        where THandler : struct, IPhysicsHitHandler
    {
        public THandler Handler;
        private bool _stopped;

        public readonly bool AllowTest(CollidableReference collidable) => !_stopped && system.PassesChannelFilter(collidable, channel);
        public readonly bool AllowTest(CollidableReference collidable, int childIndex) => !_stopped;

        public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable,
            int childIndex)
        {
            if (_stopped || system.ResolveCollidable(collidable) is not { } body) return;
            var hit = new RayCastResult { Body = body.Handle, Location = ray.Origin + ray.Direction * t, Normal = normal, Distance = t };
            if (Handler.OnHit(hit)) return;
            _stopped = true;
            maximumT = 0f;
        }
    }

    private struct ClosestSweepHitHandler(BepuPhysicsSystem system, int channel) : ISweepHitHandler
    {
        public bool Hit;
        public RayCastResult Result;

        public readonly bool AllowTest(CollidableReference collidable) => system.PassesChannelFilter(collidable, channel);
        public readonly bool AllowTest(CollidableReference collidable, int childIndex) => true;

        public void OnHit(ref float maximumT, float t, Vector3 hitLocation, Vector3 hitNormal, CollidableReference collidable)
        {
            if (system.ResolveCollidable(collidable) is not { } body) return;
            Hit = true;
            maximumT = t;
            Result = new RayCastResult { Body = body.Handle, Location = hitLocation, Normal = hitNormal, Distance = t };
        }

        public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
        {
            if (system.ResolveCollidable(collidable) is not { } body) return;
            Hit = true;
            maximumT = 0f;
            Result = new RayCastResult { Body = body.Handle, Location = default, Normal = default, Distance = 0f };
        }
    }

    private struct VisitingSweepHitHandler<THandler>(BepuPhysicsSystem system, int channel) : ISweepHitHandler
        where THandler : struct, IPhysicsHitHandler
    {
        public THandler Handler;
        private bool _stopped;

        public readonly bool AllowTest(CollidableReference collidable) => !_stopped && system.PassesChannelFilter(collidable, channel);
        public readonly bool AllowTest(CollidableReference collidable, int childIndex) => !_stopped;

        public void OnHit(ref float maximumT, float t, Vector3 hitLocation, Vector3 hitNormal, CollidableReference collidable)
        {
            if (_stopped || system.ResolveCollidable(collidable) is not { } body) return;
            Report(new RayCastResult { Body = body.Handle, Location = hitLocation, Normal = hitNormal, Distance = t }, ref maximumT);
        }

        public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
        {
            if (_stopped || system.ResolveCollidable(collidable) is not { } body) return;
            Report(new RayCastResult { Body = body.Handle, Location = default, Normal = default, Distance = 0f }, ref maximumT);
        }

        private void Report(in RayCastResult hit, ref float maximumT)
        {
            if (Handler.OnHit(hit)) return;
            _stopped = true;
            maximumT = 0f;
        }
    }

    /// <summary>
    ///     Bepu reports a compound or mesh once per overlapping child, consecutively, so repeats of the body just
    ///     reported are skipped.
    /// </summary>
    private struct VisitingOverlapHandler<THandler>(BepuPhysicsSystem system, int channel) : ISweepHitHandler
        where THandler : struct, IPhysicsOverlapHandler
    {
        public THandler Handler;
        private bool _stopped;
        private PhysicsBodyHandle _last;

        public readonly bool AllowTest(CollidableReference collidable) => !_stopped && system.PassesChannelFilter(collidable, channel);
        public readonly bool AllowTest(CollidableReference collidable, int childIndex) => !_stopped;

        public void OnHit(ref float maximumT, float t, Vector3 hitLocation, Vector3 hitNormal, CollidableReference collidable)
        {
            OnHitAtZeroT(ref maximumT, collidable);
        }

        public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
        {
            if (_stopped || system.ResolveCollidable(collidable) is not { } body || body.Handle == _last) return;
            _last = body.Handle;
            if (Handler.OnOverlap(body.Handle)) return;
            _stopped = true;
            maximumT = 0f;
        }
    }

    private PhysicsBodyHandle Register(BepuBody body)
    {
        uint index;
        if (_freeIndices.Count > 0)
        {
            index = _freeIndices.Pop();
            _slots[(int)index] = body;
        }
        else
        {
            index = (uint)_slots.Count;
            _slots.Add(body);
            _versions.Add(0);
        }

        var version = _versions[(int)index] += 1;
        var handle = new PhysicsBodyHandle(index, version);
        body.Handle = handle;
        if (body.BodyHandle is { } bodyHandle) _bodyHandleLookup[bodyHandle.Value] = body;
        if (body.StaticHandle is { } staticHandle) _staticHandleLookup[staticHandle.Value] = body;
        return handle;
    }

    private BepuBody? ResolveCollidable(CollidableReference collidable)
    {
        return collidable.Mobility == CollidableMobility.Static
            ? _staticHandleLookup.GetValueOrDefault(collidable.StaticHandle.Value)
            : _bodyHandleLookup.GetValueOrDefault(collidable.BodyHandle.Value);
    }

    private bool IsTriggerCollidable(CollidableReference collidable)
    {
        return ResolveCollidable(collidable) is { IsTrigger: true };
    }

    private bool IsDestroyed(PhysicsBodyHandle handle)
    {
        var index = handle.Index;
        return index >= _slots.Count || _versions[(int)index] != handle.Version || _slots[(int)index] is null;
    }

    private bool IsResting(PhysicsBodyHandle handle)
    {
        var index = handle.Index;
        if (index >= _slots.Count || _versions[(int)index] != handle.Version || _slots[(int)index] is not { } body) return true;
        return body.BodyHandle is not { } bodyHandle || !Simulation.Bodies[bodyHandle].Awake;
    }

    private bool PassesChannelFilter(CollidableReference collidable, int channel)
    {
        return channel == -1 || (ResolveCollidable(collidable) is { } body && body.CollisionChannel == channel);
    }

    private BepuBody Resolve(PhysicsBodyHandle handle)
    {
        var index = handle.Index;
        if (index >= _slots.Count || _versions[(int)index] != handle.Version || _slots[(int)index] is not { } body)
            throw new ArgumentException("Stale or invalid PhysicsBodyHandle", nameof(handle));
        return body;
    }

    private T Resolve<T>(PhysicsBodyHandle handle) where T : BepuBody
    {
        if (Resolve(handle) is not T typed)
            throw new ArgumentException($"PhysicsBodyHandle does not refer to a {typeof(T).Name}", nameof(handle));
        return typed;
    }

    /// <summary>
    ///     Keyed by Bepu's <see cref="BodyHandle.Value" />; true velocity is tracked here, not in Bepu's
    ///     own state, so repeated scaling doesn't compound toward zero across substeps.
    /// </summary>
    private sealed class TimeDilation
    {
        public float Scale = 1f;
        public Vector3 TrueLinearVelocity;
        public Vector3 TrueAngularVelocity;
    }

    /// <summary>
    ///     Turns per-step manifolds into begin and end events. Workers append touching pairs to their own list during
    ///     the step, and the diff against the previous step runs on the calling thread afterwards. A pair that
    ///     disappears because every body in it fell asleep is carried over, since Bepu stops reporting sleeping pairs.
    /// </summary>
    private sealed class ContactTracker(BepuPhysicsSystem system, int workerCount)
    {
        private readonly List<TouchingPair>[] _perWorker = CreateLists(workerCount);
        private readonly List<PhysicsContactEvent> _events = [];
        private HashSet<TouchingPair> _current = [];
        private HashSet<TouchingPair> _previous = [];

        public ReadOnlySpan<PhysicsContactEvent> Events => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_events);

        public void BeginStep()
        {
            _events.Clear();
            foreach (var list in _perWorker) list.Clear();
        }

        public bool Record<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold)
            where TManifold : unmanaged, IContactManifold<TManifold>
        {
            if (system.ResolveCollidable(pair.A) is not { } a || system.ResolveCollidable(pair.B) is not { } b) return true;
            var trigger = a.IsTrigger || b.IsTrigger;
            if (IsTouching(ref manifold)) _perWorker[workerIndex].Add(TouchingPair.Of(a.Handle, b.Handle, trigger));
            return !trigger;
        }

        public void EndStep()
        {
            foreach (var list in _perWorker)
            foreach (var pair in list)
                _current.Add(pair);

            foreach (var pair in _current)
                if (!_previous.Contains(pair))
                    _events.Add(new PhysicsContactEvent(pair.A, pair.B, PhysicsContactKind.Begin, pair.IsTrigger));

            foreach (var pair in _previous)
            {
                if (_current.Contains(pair)) continue;
                if (system.IsResting(pair.A) && system.IsResting(pair.B) && !system.IsDestroyed(pair.A) && !system.IsDestroyed(pair.B))
                {
                    _current.Add(pair);
                    continue;
                }

                _events.Add(new PhysicsContactEvent(pair.A, pair.B, PhysicsContactKind.End, pair.IsTrigger));
            }

            (_previous, _current) = (_current, _previous);
            _current.Clear();
        }

        private static bool IsTouching<TManifold>(ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            for (var i = 0; i < manifold.Count; i++)
                if (manifold.GetDepth(i) >= 0f)
                    return true;
            return false;
        }

        private static List<TouchingPair>[] CreateLists(int count)
        {
            var lists = new List<TouchingPair>[count];
            for (var i = 0; i < count; i++) lists[i] = [];
            return lists;
        }

        private readonly record struct TouchingPair(PhysicsBodyHandle A, PhysicsBodyHandle B, bool IsTrigger)
        {
            public static TouchingPair Of(PhysicsBodyHandle a, PhysicsBodyHandle b, bool trigger)
            {
                return a.Index <= b.Index ? new TouchingPair(a, b, trigger) : new TouchingPair(b, a, trigger);
            }
        }
    }

    private struct NarrowPhaseCallbacks(BepuPhysicsSystem physicsSystem) : INarrowPhaseCallbacks
    {
        public void Initialize(Simulation simulation)
        {
        }

        /// <summary>
        ///     Contacts between two bodies that cannot respond (kinematic or static) would be solver work for nothing,
        ///     so they are only generated when one of them is a trigger that wants to report the pair.
        /// </summary>
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b,
            ref float speculativeMargin)
        {
            if (a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic) return true;
            return physicsSystem.IsTriggerCollidable(a) || physicsSystem.IsTriggerCollidable(b);
        }

        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return true;
        }

        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
            out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial = new PairMaterialProperties
                { FrictionCoefficient = 1, MaximumRecoveryVelocity = 2, SpringSettings = new SpringSettings(35, 5) };
            return physicsSystem._contacts.Record(workerIndex, pair, ref manifold);
        }

        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB,
            ref ConvexContactManifold manifold)
        {
            return true;
        }

        public void Dispose()
        {
        }
    }

    private struct PoseIntegratorCallbacks(BepuPhysicsSystem physicsSystem) : IPoseIntegratorCallbacks
    {
        private Vector3Wide _gravityAcceleration;
        private Vector<float> _linearDampingDt;
        private Vector<float> _angularDampingDt;

        public void Initialize(Simulation simulation)
        {
        }

        public void PrepareForIntegration(float dt)
        {
            _linearDampingDt =
                new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - physicsSystem.LinearDamping, 0, 1), dt));
            _angularDampingDt =
                new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - physicsSystem.AngularDamping, 0, 1), dt));
            _gravityAcceleration = Vector3Wide.Broadcast(physicsSystem.GetGravity());
        }

        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
            BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt,
            ref BodyVelocityWide velocity)
        {
            var acceleration = _gravityAcceleration;
            var newVelocity = velocity.Linear + acceleration * dt;
            velocity.Linear = newVelocity;

            // Apply damping to velocity
            // velocity.Linear *= _linearDampingDt;
            // velocity.Angular *= _angularDampingDt;

            if (physicsSystem._timeDilation.Count > 0) ApplyTimeDilation(bodyIndices, integrationMask, dt, ref velocity);
        }

        // IntegrateVelocity fires twice per body per step (PredictBoundingBoxes's result is discardable
        // per Bepu's own source, only Solve's is real); skip the first sighting or gravity double-counts.
        private void ApplyTimeDilation(Vector<int> bodyIndices, Vector<int> integrationMask, Vector<float> dt,
            ref BodyVelocityWide velocity)
        {
            var activeSet = physicsSystem.Simulation.Bodies.ActiveSet;
            for (var lane = 0; lane < Vector<float>.Count; lane++)
            {
                if (GatherScatter.Get(ref integrationMask, lane) == 0) continue;
                var bodyIndex = GatherScatter.Get(ref bodyIndices, lane);
                if (bodyIndex < 0) continue;

                var bodyHandle = activeSet.IndexToHandle[bodyIndex].Value;
                if (!physicsSystem._timeDilation.TryGetValue(bodyHandle, out var dilation)) continue;
                if (physicsSystem._dilatedBodiesSeenThisStep.Add(bodyHandle)) continue;

                var laneDt = GatherScatter.Get(ref dt, lane);
                dilation.TrueLinearVelocity += physicsSystem._gravity * laneDt;

                GatherScatter.Get(ref velocity.Linear.X, lane) = dilation.TrueLinearVelocity.X * dilation.Scale;
                GatherScatter.Get(ref velocity.Linear.Y, lane) = dilation.TrueLinearVelocity.Y * dilation.Scale;
                GatherScatter.Get(ref velocity.Linear.Z, lane) = dilation.TrueLinearVelocity.Z * dilation.Scale;
                GatherScatter.Get(ref velocity.Angular.X, lane) = dilation.TrueAngularVelocity.X * dilation.Scale;
                GatherScatter.Get(ref velocity.Angular.Y, lane) = dilation.TrueAngularVelocity.Y * dilation.Scale;
                GatherScatter.Get(ref velocity.Angular.Z, lane) = dilation.TrueAngularVelocity.Z * dilation.Scale;
            }
        }

        public AngularIntegrationMode AngularIntegrationMode => physicsSystem.AngularIntegrationMode;
        public bool AllowSubstepsForUnconstrainedBodies => physicsSystem.AllowSubstepsForUnconstrainedBodies;
        public bool IntegrateVelocityForKinematics => false;
    }
}
