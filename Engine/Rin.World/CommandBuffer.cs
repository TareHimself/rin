using System.Runtime.InteropServices;

namespace Rin.World;

/// <summary>
///     Records structural changes and applies them in record order on <see cref="Playback" />. Commands for an entity that
///     is no longer alive at playback are skipped.
/// </summary>
public sealed class CommandBuffer
{
    private readonly World _world;
    private readonly List<Command> _commands = [];
    private TypedCommands?[] _typed = [];

    public CommandBuffer(World world) => _world = world;

    public int Count => _commands.Count;

    /// <summary>
    ///     Creates the entity right away so ids follow record order. Only its components wait for playback.
    /// </summary>
    public Entity Spawn() => _world.Spawn();

    public void Despawn(Entity entity) => _commands.Add(new Command(CommandKind.Despawn, entity, -1, -1));

    public void Add<T>(Entity entity, in T component) where T : struct, IComponentData =>
        Record(CommandKind.Add, entity, component);

    public void Set<T>(Entity entity, in T component) where T : struct, IComponentData =>
        Record(CommandKind.Set, entity, component);

    public void Remove<T>(Entity entity) where T : struct, IComponentData =>
        RecordWithoutValue<ComponentCommands<T>>(CommandKind.Remove, entity, ComponentCommands<T>.Slot);

    public void AddTag<T>(Entity entity) where T : struct, ITagData =>
        RecordWithoutValue<TagCommands<T>>(CommandKind.AddTag, entity, TagCommands<T>.Slot);

    public void RemoveTag<T>(Entity entity) where T : struct, ITagData =>
        RecordWithoutValue<TagCommands<T>>(CommandKind.RemoveTag, entity, TagCommands<T>.Slot);

    public void Playback()
    {
        try
        {
            // Hooks may record more commands while this runs; they are applied in the same playback.
            for (var i = 0; i < _commands.Count; i++)
            {
                var command = _commands[i];
                if (!_world.IsAlive(command.Entity)) continue;
                if (command.Kind == CommandKind.Despawn) _world.Despawn(command.Entity);
                else _typed[command.Slot]!.Apply(_world, command);
            }
        }
        finally
        {
            Clear();
        }
    }

    internal void Clear()
    {
        _commands.Clear();
        foreach (var typed in _typed) typed?.Clear();
    }

    private void Record<T>(CommandKind kind, Entity entity, in T component) where T : struct, IComponentData
    {
        var components = GetTyped<ComponentCommands<T>>(ComponentCommands<T>.Slot);
        _commands.Add(new Command(kind, entity, ComponentCommands<T>.Slot, components.Store(component)));
    }

    private void RecordWithoutValue<TCommands>(CommandKind kind, Entity entity, int slot) where TCommands : TypedCommands, new()
    {
        GetTyped<TCommands>(slot);
        _commands.Add(new Command(kind, entity, slot, -1));
    }

    private TCommands GetTyped<TCommands>(int slot) where TCommands : TypedCommands, new()
    {
        if (slot >= _typed.Length) Array.Resize(ref _typed, slot + 1);
        return (TCommands)(_typed[slot] ??= new TCommands());
    }

    private enum CommandKind : byte
    {
        Despawn,
        Add,
        Set,
        Remove,
        AddTag,
        RemoveTag
    }

    private readonly record struct Command(CommandKind Kind, Entity Entity, int Slot, int ValueIndex);

    /// <summary>
    ///     Applies the commands of one component or tag type, so values are stored unboxed.
    /// </summary>
    private abstract class TypedCommands
    {
        private static int _slotCount;

        public abstract void Apply(World world, in Command command);

        public virtual void Clear()
        {
        }

        protected static int NextSlot() => Interlocked.Increment(ref _slotCount) - 1;
    }

    private sealed class ComponentCommands<T> : TypedCommands where T : struct, IComponentData
    {
        public static readonly int Slot = NextSlot();

        private readonly List<T> _values = [];

        public int Store(in T component)
        {
            _values.Add(component);
            return _values.Count - 1;
        }

        public override void Apply(World world, in Command command)
        {
            switch (command.Kind)
            {
                case CommandKind.Add:
                    world.Add(command.Entity, CollectionsMarshal.AsSpan(_values)[command.ValueIndex]);
                    break;
                case CommandKind.Set:
                    world.Set(command.Entity, CollectionsMarshal.AsSpan(_values)[command.ValueIndex]);
                    break;
                case CommandKind.Remove:
                    world.Remove<T>(command.Entity);
                    break;
            }
        }

        public override void Clear() => _values.Clear();
    }

    private sealed class TagCommands<T> : TypedCommands where T : struct, ITagData
    {
        public static readonly int Slot = NextSlot();

        public override void Apply(World world, in Command command)
        {
            if (command.Kind == CommandKind.AddTag) world.AddTag<T>(command.Entity);
            else world.RemoveTag<T>(command.Entity);
        }
    }
}
