using System.Diagnostics;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Core;
using Rin.World.Actors;
using Rin.World.Components;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;
using Rin.World.Systems;

namespace Rin.World;

public class World : IUpdatable
{
    private readonly Dictionary<string, Actor> _actors = [];
    private readonly HashSet<IPhysicsComponent> _physicsComponents = [];
    private readonly List<ISystem> _tickableSystems = [];
    private IPhysicsSystem? _physicsSystem;

    private float _remainingPhysicsTime;
    //private System.Timers.Timer? _physicsTimer;

    public Matrix4x4 WorldTransform { get; set; } = Matrix4x4.Identity;

    [PublicAPI] public float PhysicsUpdateInterval { get; set; } = 1.0f / 60.0f;

    /// <summary>Ceiling on fixed physics steps per frame — when hit, the backlog is dropped rather than spiralling.</summary>
    [PublicAPI] public int MaxPhysicsStepsPerFrame { get; set; } = 4;

    [PublicAPI] public bool Active { get; protected set; }

    public void Update(float deltaSeconds)
    {
        if (!Active) return;
        Debug.Assert(_physicsSystem != null);
        foreach (var physicsComponent in _physicsComponents) physicsComponent.PrePhysicsUpdate();

        // Clamp the frame delta so a hitch doesn't schedule a huge catch-up, then run a bounded
        // number of fixed steps. If we still can't keep up, drop the remainder instead of
        // accumulating an ever-growing backlog (spiral of death).
        _remainingPhysicsTime += float.Min(deltaSeconds, 0.25f);
        var steps = 0;
        while (_remainingPhysicsTime >= PhysicsUpdateInterval && steps < MaxPhysicsStepsPerFrame)
        {
            _physicsSystem.Update(PhysicsUpdateInterval);
            _remainingPhysicsTime -= PhysicsUpdateInterval;
            steps++;
        }

        if (steps == MaxPhysicsStepsPerFrame) _remainingPhysicsTime = 0f;

        foreach (var actor in GetActors())
        {
            if (!actor.Active) continue;
            actor.Update(deltaSeconds);
        }
    }

    public void AddPhysicsComponent(IPhysicsComponent component)
    {
        _physicsComponents.Add(component);
    }

    public void RemovePhysicsComponent(IPhysicsComponent component)
    {
        _physicsComponents.Remove(component);
    }

    protected virtual IPhysicsSystem CreatePhysicsSystem()
    {
        return new BepuPhysicsSystem();
    }

    public IPhysicsSystem GetPhysicsSystem()
    {
        if (_physicsSystem == null) throw new InvalidOperationException();
        return _physicsSystem;
    }

    public void Start()
    {
        if (Active) return;
        Active = true;
        _physicsSystem = CreatePhysicsSystem();
        foreach (var actor in GetActors()) actor.Start();
    }

    public void Stop()
    {
        if (!Active) return;
        Active = false;
        foreach (var actor in GetActors()) actor.Stop();
        _physicsSystem?.Destroy();
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
}