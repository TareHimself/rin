using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.Graphics.Vulkan.Images;

/// <summary>
///     Generational handle table for one resource type. Slot 0 is never handed out, a freed slot keeps a placeholder
///     one generation past its last occupant, and the slot's next occupant gets a generation past that, so a handle
///     captured before a free never resolves again. Reads are lock-free; callers serialize Reserve, Set and Free.
/// </summary>
internal sealed class ResourceTable<T> where T : BindlessResource, new()
{
    private readonly ResourceType _type;
    private readonly IdFactory<uint> _ids = new();
    private readonly ResourceSlots<T> _slots;

    public ResourceTable(ResourceType type, int chunkSize, int maxChunks = 1)
    {
        _type = type;
        _slots = new ResourceSlots<T>(chunkSize, maxChunks);
        _ids.NewId();
        _slots.Set(0, new T());
    }

    public int Count => _slots.Count;

    public T? Get(uint id) => _slots.Get(id);

    /// <summary>
    ///     Takes a slot and says which generation its new occupant must carry. Follow with <see cref="Set" />.
    /// </summary>
    public (uint Id, uint Generation) Reserve()
    {
        var id = _ids.NewId(out var isNewSlot);
        var generation = isNewSlot ? 0u : unchecked(_slots.Get(id)!.Generation + 1);
        return (id, generation);
    }

    public void Set(uint id, T resource) => _slots.Set(id, resource);

    /// <summary>
    ///     The occupant the handle was issued for, in any state, or null for the wrong type, id 0 or a stale handle.
    /// </summary>
    public T? Find(in ResourceHandle handle)
    {
        if (handle.Type != _type || handle.Id == 0) return null;
        return _slots.Get(handle.Id) is { } resource && resource.Generation == handle.Generation ? resource : null;
    }

    /// <summary>
    ///     Like <see cref="Find" />, but only once the occupant is ready to use.
    /// </summary>
    public T? Resolve(in ResourceHandle handle) =>
        Find(handle) is { State: BindlessResourceState.Ready } resource ? resource : null;

    /// <summary>
    ///     Retires the handle's slot and returns what was in it, or null when the handle no longer matches.
    /// </summary>
    public T? Free(in ResourceHandle handle)
    {
        if (Find(handle) is not { } resource) return null;
        _slots.Set(handle.Id, new T { Generation = unchecked(resource.Generation + 1) });
        _ids.FreeId(handle.Id);
        return resource;
    }
}
