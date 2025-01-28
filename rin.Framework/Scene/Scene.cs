using JetBrains.Annotations;
using rin.Framework.Core;
using rin.Framework.Scene.Actors;
using rin.Framework.Scene.Components;
using rin.Framework.Scene.Components.Lights;
using rin.Framework.Scene.Physics;
using rin.Framework.Scene.Systems;

namespace rin.Framework.Scene;

public class Scene : ITickable
{
    private readonly List<ISystem> _tickableSystems = [];
    private readonly Dictionary<string, Actor> _actors = [];
    private IPhysicsSystem? _physicsSystem;
    //private System.Timers.Timer? _physicsTimer;

    private float _remainingPhysicsTime = 0.0f;

    [PublicAPI] public float PhysicsUpdateInterval { get; set; } = 0.01f;//1.0f / 60.0f;
    [PublicAPI]
    public bool Active { get; protected set; }
    
    protected virtual IPhysicsSystem CreatePhysicsSystem() => new Physics.Bepu.BepuPhysics();
    public IPhysicsSystem GetPhysicsSystem()
    {
        if(_physicsSystem == null) throw new InvalidOperationException();
        return _physicsSystem;
    }
    public void Start()
    {
        if(Active) return;
        Active = true;
        _physicsSystem = CreatePhysicsSystem();
        // _physicsTimer = new System.Timers.Timer(PhysicsUpdateInterval);
        // _physicsTimer.Elapsed += (_,__) => _physicsSystem.Update(PhysicsUpdateInterval);
        foreach (var actor in GetActors())
        {
            actor.Start();
        }
        _physicsSystem.Start();
        // _physicsTimer.Start();
    }
    
    public void Stop()
    {
        if(!Active) return;
        Active = false;
        //_physicsTimer?.Stop();
        foreach (var actor in GetActors())
        {
            actor.Stop();
        }
        _physicsSystem?.Dispose();
    }
    
    [PublicAPI]
    public Actor AddActor(Actor actor)
    {
        lock (_actors)
        {
            _actors.Add(actor.Id, actor);
            actor.Scene = this;
        }
        if(Active) actor.Start();
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
        lock (_actors)
        {
            return _actors.Values.ToArray();
        }
    }

    public void Update(double delta)
    {
        if(!Active) return;
        _remainingPhysicsTime += (float)delta;
        while (_remainingPhysicsTime > PhysicsUpdateInterval)
        {
            _physicsSystem?.Update(PhysicsUpdateInterval);
            _remainingPhysicsTime -= PhysicsUpdateInterval;
        }
        //_physicsSystem?.Update(delta);
        foreach (var actor in GetActors())
        {
            if(!actor.Active) continue;
            actor.Update(delta);
        }
    }
    
    [PublicAPI]
    public IEnumerable<SceneComponent> GetRoots()
    {
        lock (_actors)
        {
            foreach (var (key, value) in _actors)
            {
                if (value.RootComponent is { } component)
                {
                    yield return component;
                }
            }
        }
    }

    [PublicAPI]
    public IEnumerable<SceneComponent> GetPureRoots()
    {
        foreach (var root in GetRoots())
        {
            if (root is { TransformParent: null } component)
            {
                yield return component;
            }
        }
    }
}