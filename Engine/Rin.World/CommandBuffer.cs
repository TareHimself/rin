using System.Runtime.InteropServices;

namespace Rin.World;

/// <summary>
///     Records structural changes and applies them in record order on <see cref="Playback" />. Commands for an entity that
///     is no longer alive at playback are skipped.
/// </summary>
/// <remarks>
///     When a command throws, it and the commands before it are dropped and the rest stay for the next playback.
/// </remarks>
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

    public void Add<T>(Entity entity, in T component) where T : unmanaged, IComponentData =>
        Record(CommandKind.Add, entity, component);

    public void Set<T>(Entity entity, in T component) where T : unmanaged, IComponentData =>
        Record(CommandKind.Set, entity, component);

    public void Remove<T>(Entity entity) where T : unmanaged, IComponentData
    {
        CommandsOf<T>();
        _commands.Add(new Command(CommandKind.Remove, entity, ComponentCommands<T>.Slot, -1));
    }

    public void Playback()
    {
        var index = 0;
        try
        {
            // Hooks may record more commands while this runs; they are applied in the same playback.
            for (; index < _commands.Count; index++) Apply(_commands[index]);
        }
        catch
        {
            // Stored values stay, because the remaining commands index into them.
            _commands.RemoveRange(0, index + 1);
            throw;
        }

        Clear();
    }

    internal void Clear()
    {
        _commands.Clear();
        foreach (var typed in _typed) typed?.Clear();
    }

    private void Apply(in Command command)
    {
        if (!_world.IsAlive(command.Entity)) return;
        if (command.Kind == CommandKind.Despawn) _world.Despawn(command.Entity);
        else _typed[command.Slot]!.Apply(_world, command);
    }

    private void Record<T>(CommandKind kind, Entity entity, in T component) where T : unmanaged, IComponentData =>
        _commands.Add(new Command(kind, entity, ComponentCommands<T>.Slot, CommandsOf<T>().Store(component)));

    private ComponentCommands<T> CommandsOf<T>() where T : unmanaged, IComponentData
    {
        var slot = ComponentCommands<T>.Slot;
        if (slot >= _typed.Length) Array.Resize(ref _typed, slot + 1);
        return (ComponentCommands<T>)(_typed[slot] ??= new ComponentCommands<T>());
    }

    private enum CommandKind : byte
    {
        Despawn,
        Add,
        Set,
        Remove
    }

    private readonly record struct Command(CommandKind Kind, Entity Entity, int Slot, int ValueIndex);

    /// <summary>
    ///     Applies the commands of one component type, so values are stored unboxed.
    /// </summary>
    private abstract class TypedCommands
    {
        private static int _slotCount;

        public abstract void Apply(World world, in Command command);

        public abstract void Clear();

        protected static int NextSlot() => Interlocked.Increment(ref _slotCount) - 1;
    }

    private sealed class ComponentCommands<T> : TypedCommands where T : unmanaged, IComponentData
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
}
