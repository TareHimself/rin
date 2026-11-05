using Rin.World.Scheduling;
using Rin.World.Tasks;

namespace Rin.World.Scripting;

/// <summary>
///     Attaches managed <see cref="Script" />s to entities through a <see cref="ScriptRef" /> component, ticks them in
///     <see cref="TickPhase.Gameplay" /> in attach order and owns the <see cref="GameTaskScheduler" /> their tasks run on.
/// </summary>
/// <remarks>
///     Scripts are not in a snapshot. After a restore a script stays attached when its entity exists and had a
///     <see cref="ScriptRef" /> in the snapshot; every other script is stopped and detached (its entity is gone or got the
///     script after the save). Then <see cref="Restore" /> runs. An entity that had a script at save time whose script
///     was detached since gets none back.
/// </remarks>
public sealed class ScriptFeature : IWorldRestoreListener
{
    private readonly List<Script?> _slots = [];
    private readonly Stack<int> _freeSlots = [];
    private readonly List<Script?> _attachOrder = [];
    private readonly Query<ScriptRef> _scriptRefs;
    private readonly List<Entity> _unboundRefs = [];

    public ScriptFeature(World world)
    {
        World = world;
        world.OnRemove<ScriptRef>(OnScriptRefRemoved);
        world.AddSystem(TickPhase.Gameplay, new ScriptTick(this));
        Tasks = new GameTaskScheduler(world);
        world.AddRestoreListener(this);
        _scriptRefs = world.Query<ScriptRef>();
    }

    public World World { get; }

    public GameTaskScheduler Tasks { get; }

    public int Count { get; private set; }

    /// <summary>
    ///     Adds a <see cref="ScriptRef" /> and calls <see cref="Script.Start" /> right away. One script per entity, use
    ///     child entities for more. A structural change, so not for use inside a query loop.
    /// </summary>
    public void Attach(Entity entity, Script script)
    {
        if (script.IsAttached) throw new InvalidOperationException($"{script.GetType().Name} is already attached.");
        if (World.Has<ScriptRef>(entity)) throw new InvalidOperationException($"{entity} already has a script.");

        // Added first, so a throwing Add leaves the feature untouched.
        var slot = _freeSlots.TryPeek(out var free) ? free : _slots.Count;
        World.Add(entity, new ScriptRef(slot));
        if (slot == _slots.Count) _slots.Add(null);
        else _freeSlots.Pop();

        _slots[slot] = script;
        _attachOrder.Add(script);
        Count++;
        script.Bind(this, entity);
        script.Start();
    }

    /// <summary>
    ///     Removes the <see cref="ScriptRef" />, which cancels the script's tasks and calls <see cref="Script.Stop" />.
    /// </summary>
    public bool Detach(Entity entity) => World.Remove<ScriptRef>(entity);

    public Script? GetScript(Entity entity) =>
        World.Has<ScriptRef>(entity) && World.Get<ScriptRef>(entity) is { IsBound: true } scriptRef ? _slots[scriptRef.Slot] : null;

    /// <summary>
    ///     The entry point for a snapshot restore: cancels every task with <see cref="CancelReason.Restored" />, then
    ///     calls <see cref="Script.Restore" /> on each script in attach order.
    /// </summary>
    public void Restore()
    {
        Tasks.CancelAll(CancelReason.Restored);
        var count = _attachOrder.Count;
        for (var i = 0; i < count; i++) _attachOrder[i]?.Restore();
    }

    internal void TickScripts(in TickContext context)
    {
        // Scripts attached while this runs start ticking next tick; detached ones leave a hole, compacted after.
        var count = _attachOrder.Count;
        for (var i = 0; i < count; i++) _attachOrder[i]?.Tick(context);
        CompactAttachOrder();
    }

    void IWorldRestoreListener.BeforeRestore()
    {
    }

    void IWorldRestoreListener.AfterRestore()
    {
        for (var slot = 0; slot < _slots.Count; slot++)
        {
            if (_slots[slot] is not { } script) continue;
            var entity = script.Entity;
            if (World.IsAlive(entity) && World.Has<ScriptRef>(entity)) World.Get<ScriptRef>(entity) = new ScriptRef(slot);
            else Release(script, slot, CancelReason.Restored);
        }

        foreach (var (scriptRefs, entities) in _scriptRefs)
            for (var i = 0; i < scriptRefs.Length; i++)
                if (!scriptRefs[i].IsBound) _unboundRefs.Add(entities[i]);
        foreach (var entity in _unboundRefs) World.Remove<ScriptRef>(entity);
        _unboundRefs.Clear();

        CompactAttachOrder();
        Restore();
    }

    private void CompactAttachOrder()
    {
        if (_attachOrder.Count != Count) _attachOrder.RemoveAll(static script => script is null);
    }

    private void OnScriptRefRemoved(Entity entity, ref ScriptRef scriptRef)
    {
        if (!scriptRef.IsBound) return;
        var reason = World.IsDespawning(entity) ? CancelReason.Despawned : CancelReason.Detached;
        Release(_slots[scriptRef.Slot]!, scriptRef.Slot, reason);
    }

    private void Release(Script script, int slot, CancelReason reason)
    {
        _slots[slot] = null;
        _freeSlots.Push(slot);
        _attachOrder[_attachOrder.IndexOf(script)] = null;
        Count--;

        Tasks.Cancel(script, reason);
        script.Stop();
        script.Unbind();
    }
}
