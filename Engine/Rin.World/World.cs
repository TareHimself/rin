using Rin.World.Ecs;
using Rin.World.Scheduling;
using Rin.World.Transforms;

namespace Rin.World;

/// <summary>
///     Owns the entity store and runs the systems: fixed ticks first, then the frame phases.
/// </summary>
public sealed class World
{
    private const float MaxFrameDeltaSeconds = 0.25f;

    private readonly EcsStore _store = new();
    private readonly SystemSchedule _schedule = new();
    private float _accumulator;

    public World()
    {
        Commands = new CommandBuffer(this);
        _schedule.Add(FramePhase.Extract, new TransformPropagation(this));
    }

    /// <summary>
    ///     Played back after every tick and after the frame phases.
    /// </summary>
    public CommandBuffer Commands { get; }

    public float TickInterval
    {
        get;
        set => field = value > 0f ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "Must be positive.");
    } = 1f / 60f;

    /// <summary>
    ///     When a frame hits this many ticks the remaining backlog is dropped instead of carried over.
    /// </summary>
    public int MaxTicksPerFrame { get; set; } = 4;

    public float TimeScale
    {
        get;
        set => field = float.Max(0f, value);
    } = 1f;

    public long TickIndex { get; private set; }

    /// <summary>
    ///     How far the current frame is between the last tick and the next, from 0 to 1.
    /// </summary>
    public float InterpolationAlpha { get; private set; }

    public int EntityCount => _store.EntityCount;

    public void AddSystem(TickPhase phase, TickSystem system) => _schedule.Add(phase, system);

    public void AddSystem(FramePhase phase, FrameSystem system) => _schedule.Add(phase, system);

    public void Update(float deltaSeconds)
    {
        var scaledDeltaSeconds = float.Max(0f, deltaSeconds * TimeScale);
        _accumulator += float.Min(scaledDeltaSeconds, MaxFrameDeltaSeconds);

        var ticks = 0;
        while (_accumulator >= TickInterval && ticks < MaxTicksPerFrame)
        {
            RunTick();
            _accumulator -= TickInterval;
            ticks++;
        }

        if (_accumulator >= TickInterval) _accumulator = 0f;
        InterpolationAlpha = _accumulator / TickInterval;

        _schedule.RunFrame(new FrameContext(this, scaledDeltaSeconds, InterpolationAlpha));
        Commands.Playback();
    }

    public Entity Spawn() => _store.Spawn();

    /// <summary>
    ///     Despawns the entity and all of its descendants. Does nothing for an entity that is not alive.
    /// </summary>
    public void Despawn(Entity entity) => _store.Despawn(entity);

    public bool IsAlive(Entity entity) => _store.IsAlive(entity);

    public bool Has<T>(Entity entity) where T : struct, IComponentData => _store.Has<T>(entity);

    public ref T Get<T>(Entity entity) where T : struct, IComponentData => ref _store.Get<T>(entity);

    /// <summary>
    ///     Adds the component, or overwrites it when present. <see cref="OnAdd{T}" /> hooks fire only for a new component.
    /// </summary>
    public void Add<T>(Entity entity, in T component) where T : struct, IComponentData => _store.Add(entity, component);

    /// <summary>
    ///     Overwrites a component the entity already has.
    /// </summary>
    public void Set<T>(Entity entity, in T component) where T : struct, IComponentData => _store.Set(entity, component);

    public bool Remove<T>(Entity entity) where T : struct, IComponentData => _store.Remove<T>(entity);

    public bool HasTag<T>(Entity entity) where T : struct, ITagData => _store.HasTag<T>(entity);

    public void AddTag<T>(Entity entity) where T : struct, ITagData => _store.AddTag<T>(entity);

    public void RemoveTag<T>(Entity entity) where T : struct, ITagData => _store.RemoveTag<T>(entity);

    public void SetParent(Entity child, Entity parent)
    {
        _store.SetParent(child, parent);
        MarkTransformDirty(child);
    }

    public void ClearParent(Entity child)
    {
        _store.ClearParent(child);
        MarkTransformDirty(child);
    }

    /// <summary>
    ///     The parent, or <c>default</c> for a root.
    /// </summary>
    public Entity GetParent(Entity child) => _store.GetParent(child);

    public EntityChildren GetChildren(Entity parent) => _store.GetChildren(parent);

    public void OnAdd<T>(ComponentHook<T> handler) where T : struct, IComponentData => _store.OnAdd(handler);

    /// <summary>
    ///     Fires before the component is removed, including when its entity is despawned.
    /// </summary>
    public void OnRemove<T>(ComponentHook<T> handler) where T : struct, IComponentData => _store.OnRemove(handler);

    public Query<T1> Query<T1>()
        where T1 : struct, IComponentData
        => _store.Query<T1>();

    public Query<T1, T2> Query<T1, T2>()
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        => _store.Query<T1, T2>();

    public Query<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        where T3 : struct, IComponentData
        => _store.Query<T1, T2, T3>();

    public Query<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        where T3 : struct, IComponentData
        where T4 : struct, IComponentData
        => _store.Query<T1, T2, T3, T4>();

    private void RunTick()
    {
        _schedule.RunTick(new TickContext(this, TickIndex, TickInterval));
        Commands.Playback();
        TickIndex++;
    }

    private void MarkTransformDirty(Entity entity)
    {
        if (_store.Has<LocalTransform>(entity)) _store.Get<LocalTransform>(entity).IsDirty = true;
    }
}
