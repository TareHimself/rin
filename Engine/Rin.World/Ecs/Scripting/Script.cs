using Rin.World.Ecs.Scheduling;
using Rin.World.Ecs.Tasks;

namespace Rin.World.Ecs.Scripting;

/// <summary>
///     Logic for one entity, attached with <see cref="ScriptFeature.Attach" />. A script holds no state that must survive
///     a snapshot restore: keep that in components and rebuild the tasks from it in <see cref="Restore" />.
/// </summary>
public abstract class Script
{
    private ScriptFeature? _feature;

    public Entity Entity { get; private set; }

    public bool IsAttached => _feature is not null;

    public EntityWorld World => Feature.World;

    /// <summary>
    ///     Starts tasks owned by <see cref="Entity" />. They are cancelled when the script stops.
    /// </summary>
    public TaskContext Tasks => new(Feature.Tasks, Entity, this);

    private ScriptFeature Feature => _feature ?? throw new InvalidOperationException($"{GetType().Name} is not attached.");

    /// <summary>
    ///     Called once when attached.
    /// </summary>
    protected internal virtual void Start()
    {
    }

    /// <summary>
    ///     Called every tick in <see cref="TickPhase.Gameplay" />, in attach order, from the first Gameplay phase that starts after the attach.
    /// </summary>
    protected internal virtual void Tick(in TickContext context)
    {
    }

    /// <summary>
    ///     Called once when detached or despawned, after the script's tasks were cancelled.
    /// </summary>
    protected internal virtual void Stop()
    {
    }

    /// <summary>
    ///     Called by <see cref="ScriptFeature.Restore" /> after every task was cancelled, to restart tasks from component
    ///     state.
    /// </summary>
    protected internal virtual void Restore()
    {
    }

    internal void Bind(ScriptFeature feature, Entity entity)
    {
        _feature = feature;
        Entity = entity;
    }

    internal void Unbind()
    {
        _feature = null;
        Entity = default;
    }
}
