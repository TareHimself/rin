using Rin.World.Scheduling;
using Rin.World.Tasks;

namespace Rin.World.Scripting;

/// <summary>
///     Attaches managed <see cref="Script" />s to entities through a <see cref="ScriptRef" /> component, ticks them in
///     <see cref="TickPhase.Gameplay" /> in attach order and owns the <see cref="GameTaskScheduler" /> their tasks run on.
/// </summary>
public sealed class ScriptFeature
{
    private readonly List<Script?> _slots = [];
    private readonly Stack<int> _freeSlots = [];
    private readonly List<Script?> _attachOrder = [];

    public ScriptFeature(World world)
    {
        World = world;
        world.OnRemove<ScriptRef>(OnScriptRefRemoved);
        world.AddSystem(TickPhase.Gameplay, new ScriptTick(this));
        Tasks = new GameTaskScheduler(world);
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

        var slot = _freeSlots.Count > 0 ? _freeSlots.Pop() : AddSlot();
        _slots[slot] = script;
        _attachOrder.Add(script);
        Count++;
        script.Bind(this, entity);
        World.Add(entity, new ScriptRef(slot));
        script.Start();
    }

    /// <summary>
    ///     Removes the <see cref="ScriptRef" />, which cancels the script's tasks and calls <see cref="Script.Stop" />.
    /// </summary>
    public bool Detach(Entity entity) => World.Remove<ScriptRef>(entity);

    public Script? GetScript(Entity entity) => World.Has<ScriptRef>(entity) ? _slots[World.Get<ScriptRef>(entity).Slot] : null;

    /// <summary>
    ///     The entry point for a snapshot restore: cancels every task, then calls <see cref="Script.Restore" /> on each
    ///     script in attach order.
    /// </summary>
    public void Restore()
    {
        Tasks.CancelAll();
        var count = _attachOrder.Count;
        for (var i = 0; i < count; i++) _attachOrder[i]?.Restore();
    }

    internal void TickScripts(in TickContext context)
    {
        // Scripts attached while this runs start ticking next tick; detached ones leave a hole, compacted after.
        var count = _attachOrder.Count;
        for (var i = 0; i < count; i++) _attachOrder[i]?.Tick(context);
        if (_attachOrder.Count != Count) _attachOrder.RemoveAll(static script => script is null);
    }

    private int AddSlot()
    {
        _slots.Add(null);
        return _slots.Count - 1;
    }

    private void OnScriptRefRemoved(Entity entity, ref ScriptRef scriptRef)
    {
        var slot = scriptRef.Slot;
        var script = _slots[slot]!;
        _slots[slot] = null;
        _freeSlots.Push(slot);
        _attachOrder[_attachOrder.IndexOf(script)] = null;
        Count--;

        Tasks.Cancel(script);
        script.Stop();
        script.Unbind();
    }
}
