using System.Numerics;
using JetBrains.Annotations;
using Rin.Core;
using Rin.Core.Shared.Math;
using Rin.World.Actors;
using Rin.World.Components;
using Rin.World.Graphics;
using Rin.World.Physics;
using Rin.World.Systems;

namespace Rin.World;

public class World : IUpdatable
{
    private readonly Dictionary<string, Actor> _actors = [];
    private readonly List<ISystem> _tickableSystems = [];
    private readonly Dictionary<PhysicsBodyHandle, IWorldComponent> _physicsOwners = [];

    private float _remainingPhysicsTime;

    public World(IRenderSystem renderSystem, IPhysicsSystem physicsSystem)
    {
        RenderSystem = renderSystem;
        PhysicsSystem = physicsSystem;
    }

    [PublicAPI] public IRenderSystem RenderSystem { get; }
    [PublicAPI] public IPhysicsSystem PhysicsSystem { get; }

    public Matrix4x4 WorldTransform { get; set; } = Matrix4x4.Identity;

    [PublicAPI] public float PhysicsUpdateInterval { get; set; } = 1.0f / 60.0f;

    /// <summary>Ceiling on fixed physics steps per frame — when hit, the backlog is dropped rather than spiralling.</summary>
    [PublicAPI] public int MaxPhysicsStepsPerFrame { get; set; } = 4;

    /// <summary>Scales simulated time for the whole world; per-body <see cref="IPhysicsSystem.SetTimeScale"/> is relative to this.</summary>
    [PublicAPI] public float TimeScale { get; set; } = 1.0f;

    [PublicAPI] public bool Active { get; protected set; }

    public void Update(float deltaSeconds)
    {
        if (!Active) return;

        var scaledDeltaSeconds = deltaSeconds * TimeScale;

        foreach (var actor in GetActors())
        {
            if (!actor.Active) continue;
            actor.PrePhysicsUpdate();
        }

        // Clamp the frame delta so a hitch doesn't schedule a huge catch-up, then run a bounded
        // number of fixed steps. If we still can't keep up, drop the remainder instead of
        // accumulating an ever-growing backlog (spiral of death).
        _remainingPhysicsTime += float.Min(scaledDeltaSeconds, 0.25f);
        var steps = 0;
        while (_remainingPhysicsTime >= PhysicsUpdateInterval && steps < MaxPhysicsStepsPerFrame)
        {
            PhysicsSystem.Update(PhysicsUpdateInterval);
            _remainingPhysicsTime -= PhysicsUpdateInterval;
            steps++;
        }

        if (steps == MaxPhysicsStepsPerFrame) _remainingPhysicsTime = 0f;

        foreach (var actor in GetActors())
        {
            if (!actor.Active) continue;
            actor.Update(scaledDeltaSeconds);
        }

        foreach (var actor in GetActors())
        {
            if (!actor.Active) continue;
            actor.LateUpdate(scaledDeltaSeconds);
        }
    }

    public void Start()
    {
        if (Active) return;
        Active = true;
        foreach (var actor in GetActors()) actor.Start();
    }

    public void Stop()
    {
        if (!Active) return;
        Active = false;
        foreach (var actor in GetActors()) actor.Stop();
        PhysicsSystem.Destroy();
        RenderSystem.Dispose();
    }

    [PublicAPI]
    public Actor AddActor(Actor actor)
    {
        _actors.Add(actor.Id, actor);
        actor.World = this;

        if (Active) actor.Start();
        return actor;
    }

    [PublicAPI]
    public T AddActor<T>() where T : Actor
    {
        var actor = Activator.CreateInstance<T>();
        AddActor(actor);
        return actor;
    }

    [PublicAPI]
    public Actor[] GetActors()
    {
        return _actors.Values.ToArray();
    }

    [PublicAPI]
    public WorldComponent[] GetRoots()
    {
        return _actors.Values.Select(actor => actor.RootComponent).OfType<WorldComponent>().ToArray();
    }

    [PublicAPI]
    public WorldComponent[] GetPureRoots()
    {
        return GetRoots().Where(root => root.TransformParent is null).ToArray();
    }

    [PublicAPI] public void RegisterPhysicsBody(PhysicsBodyHandle handle, IWorldComponent owner) => _physicsOwners[handle] = owner;
    [PublicAPI] public void UnregisterPhysicsBody(PhysicsBodyHandle handle) => _physicsOwners.Remove(handle);
    [PublicAPI] public IWorldComponent? FindPhysicsOwner(PhysicsBodyHandle handle) => _physicsOwners.GetValueOrDefault(handle);

    [PublicAPI] public Vector3 GetGravity() => PhysicsSystem.GetGravity();
    [PublicAPI] public void SetGravity(in Vector3 gravity) => PhysicsSystem.SetGravity(gravity);
}
