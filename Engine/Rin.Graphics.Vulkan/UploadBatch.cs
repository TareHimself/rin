using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     One flush's worth of queued writes, chosen from the lanes without touching the device: which writes go,
///     which resources they hold a reference on, and how much staging memory the copies need. Reused every
///     flush, so the lists are cleared rather than reallocated.
/// </summary>
internal sealed class UploadBatch
{
    private readonly List<(ResourceHandle Handle, PendingTextureWrite Write)> _takenTextures = [];
    private readonly List<(ResourceHandle Handle, PendingBufferWrite Write)> _takenBuffers = [];
    private readonly HashSet<ResourceHandle> _refused = [];

    public List<(ResourceHandle Handle, PendingTextureWrite Write)> Textures { get; } = [];
    public Dictionary<ResourceHandle, List<PendingBufferWrite>> Buffers { get; } = [];
    public HashSet<ResourceHandle> Acquired { get; } = [];
    public ulong StagingSize { get; private set; }
    public bool IsEmpty => Textures.Count == 0 && Buffers.Count == 0;

    /// <summary>
    ///     Takes up to <paramref name="maxPerKind" /> writes of each kind. A write whose resource cannot be acquired
    ///     is disposed here, which cancels its tasks, and its resource is not asked again this batch.
    /// </summary>
    public void Fill(TextureWriteLanes textures, BufferWriteLanes buffers, int maxPerKind, IResourceAcquirer acquirer)
    {
        Clear();
        textures.TakeUpTo(maxPerKind, _takenTextures);
        buffers.TakeUpTo(maxPerKind, _takenBuffers);

        foreach (var (handle, write) in _takenTextures)
        {
            if (!Acquire(handle, write, acquirer)) continue;
            Textures.Add((handle, write));
            StagingSize += write.StagingSize;
        }

        foreach (var (handle, write) in _takenBuffers)
        {
            if (!Acquire(handle, write, acquirer)) continue;
            if (!Buffers.TryGetValue(handle, out var writes)) Buffers[handle] = writes = [];
            writes.Add(write);
            StagingSize += write.StagingSize;
        }

        _takenTextures.Clear();
        _takenBuffers.Clear();
        _refused.Clear();
    }

    public void CompleteWrites()
    {
        foreach (var (_, write) in Textures)
        {
            write.Complete();
            write.Dispose();
        }

        foreach (var writes in Buffers.Values)
            foreach (var write in writes)
            {
                write.Complete();
                write.Dispose();
            }
    }

    public void Clear()
    {
        Textures.Clear();
        Buffers.Clear();
        Acquired.Clear();
        StagingSize = 0;
    }

    private bool Acquire(in ResourceHandle handle, PendingWrite write, IResourceAcquirer acquirer)
    {
        if (Acquired.Contains(handle)) return true;
        if (!_refused.Contains(handle) && acquirer.TryAcquireResource(handle))
        {
            Acquired.Add(handle);
            return true;
        }

        _refused.Add(handle);
        write.Dispose();
        return false;
    }
}
