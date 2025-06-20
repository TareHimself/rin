using System.Diagnostics;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.World.Actors;
using Rin.Engine.World.Components;
using Rin.Engine.World.Physics;
using Rin.Engine.World.Physics.Bepu;
using Rin.Engine.World.Systems;

namespace Rin.Engine.World;

public class World : IReceivesUpdate
{
    private readonly Dictionary<string, Actor> _actors = [];
    private readonly HashSet<IPhysicsComponent> _physicsComponents = [];
    private readonly List<ISystem> _tickableSystems = [];
    private IPhysicsSystem? _physicsSystem;

    private float _remainingPhysicsTime;
    //private System.Timers.Timer? _physicsTimer;

    public Matrix4x4 WorldTransform { get; set; } = Matrix4x4.Identity;

    [PublicAPI] public float PhysicsUpdateInterval { get; set; } = 0.01f; //1.0f / 60.0f;

    [PublicAPI] public bool Active { get; protected set; }

    public void Update(float deltaSeconds)
    {
        if (!Active) return;
        Debug.Assert(_physicsSystem != null);
        foreach (var physicsComponent in _physicsComponents) physicsComponent.PrePhysicsUpdate();

        _remainingPhysicsTime += deltaSeconds;
        while (_remainingPhysicsTime > PhysicsUpdateInterval)
        {
            _physicsSystem.Update(PhysicsUpdateInterval);
            _remainingPhysicsTime -= PhysicsUpdateInterval;
        }

        //_physicsSystem?.Update(delta);
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
    public IEnumerable<WorldComponent> GetRoots()
    {
        foreach (var (key, value) in _actors)
            if (value.RootComponent is { } component)
                yield return component;
    }

    [PublicAPI]
    public IEnumerable<WorldComponent> GetPureRoots()
    {
        foreach (var root in GetRoots())
            if (root is { TransformParent: null } component)
                yield return component;
    }
}