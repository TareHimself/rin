using Rin.World.Physics;

namespace Rin.World.Ecs.Physics;

/// <summary>
///     Maps body handles to entities through a dense array indexed by <see cref="PhysicsBodyHandle.Index" />, so a
///     lookup is an array read and nothing is hashed.
/// </summary>
internal sealed class BodyEntityMap
{
    private Slot[] _slots = new Slot[64];

    public void Add(PhysicsBodyHandle handle, Entity entity)
    {
        var index = (int)handle.Index;
        if (index >= _slots.Length) Array.Resize(ref _slots, int.Max(index + 1, _slots.Length * 2));
        _slots[index] = new Slot(handle, entity);
    }

    public void Remove(PhysicsBodyHandle handle)
    {
        var index = (int)handle.Index;
        if (index < _slots.Length && _slots[index].Handle == handle) _slots[index] = default;
    }

    public Entity Find(PhysicsBodyHandle handle)
    {
        var index = (int)handle.Index;
        if (!handle.IsValid || index >= _slots.Length) return default;
        ref readonly var slot = ref _slots[index];
        return slot.Handle == handle ? slot.Entity : default;
    }

    private readonly record struct Slot(PhysicsBodyHandle Handle, Entity Entity);
}
