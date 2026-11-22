using System.Runtime.InteropServices;
using Rin.World;

namespace Examples.Editor.Model;

/// <summary>
///     One component of one entity, read and written as bytes through the world so every write is stamped as a change.
/// </summary>
public sealed class ComponentAccess(World world, Entity entity, ComponentId id)
{
    public ComponentInfo Info => world.Schema[id];

    public ReadOnlySpan<byte> Read(int offset, int size) => world.Read(entity, id).Slice(offset, size);

    public Span<byte> Write(int offset, int size) => world.Write(entity, id).Slice(offset, size);

    public double ReadLane(Lane lane) => FieldLanes.Read(lane.Kind, world.Read(entity, id)[lane.Offset..]);

    public void WriteLane(Lane lane, double value) => FieldLanes.Write(lane.Kind, world.Write(entity, id)[lane.Offset..], value);

    public T ReadValue<T>(int offset) where T : unmanaged => MemoryMarshal.Read<T>(world.Read(entity, id)[offset..]);

    public void WriteValue<T>(int offset, T value) where T : unmanaged => MemoryMarshal.Write(world.Write(entity, id)[offset..], in value);

    public void Remove() => world.Remove(entity, id);
}
