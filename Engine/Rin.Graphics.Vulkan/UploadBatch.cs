using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan;

internal enum UploadBatchStepKind
{
    TextureWrite,
    BufferWrites,
    Copy
}

/// <summary>
///     One recorded operation of a flush: a texture write, consecutive writes to one buffer, or a copy.
/// </summary>
internal sealed class UploadBatchStep
{
    public UploadBatchStepKind Kind { get; private set; }

    /// <summary>
    ///     The written resource, or the source of a copy.
    /// </summary>
    public ResourceHandle Handle { get; private set; }

    public PendingTextureWrite? TextureWrite { get; private set; }
    public SharedWrite? Shared { get; private set; }
    public List<PendingBufferWrite> BufferWrites { get; } = [];

    /// <summary>
    ///     The texture this step leaves in a transfer layout, or an invalid handle if it writes none.
    /// </summary>
    public ResourceHandle WrittenTexture => Kind switch
    {
        UploadBatchStepKind.TextureWrite => Handle,
        UploadBatchStepKind.Copy when Shared!.Action is TextureCopy or BufferToTextureCopy => Shared.Destination.Handle,
        _ => default
    };

    public void Reset(UploadBatchStepKind kind, in ResourceHandle handle, PendingTextureWrite? textureWrite = null,
        SharedWrite? shared = null)
    {
        Kind = kind;
        Handle = handle;
        TextureWrite = textureWrite;
        Shared = shared;
        BufferWrites.Clear();
    }

    public void Complete()
    {
        TextureWrite?.Complete();
        TextureWrite?.Dispose();
        Shared?.Complete();
        Shared?.Dispose();
        foreach (var write in BufferWrites)
        {
            write.Complete();
            write.Dispose();
        }
    }
}

/// <summary>
///     One flush's worth of queued actions, chosen from the lanes without touching the device: which actions go
///     and in what order, which resources they hold a reference on, and how much staging memory the writes need.
///     Reused every flush, so the lists are cleared rather than reallocated.
/// </summary>
internal sealed class UploadBatch
{
    private readonly List<(ResourceHandle Handle, PendingResourceAction Action)> _taken = [];
    private readonly HashSet<ResourceHandle> _refused = [];
    private readonly List<UploadBatchStep> _pool = [];

    public List<UploadBatchStep> Steps { get; } = [];
    public HashSet<ResourceHandle> Acquired { get; } = [];
    public ulong StagingSize { get; private set; }
    public bool IsEmpty => Steps.Count == 0;

    /// <summary>
    ///     Takes up to <paramref name="max" /> actions in lane order: shared writes first in enqueue order, then whole write
    ///     segments. An action whose resource cannot be acquired is disposed here, which cancels its tasks, and
    ///     its resource is not asked again this batch.
    /// </summary>
    public void Fill(UploadLanes lanes, int max, IResourceAcquirer acquirer)
    {
        Clear();
        lanes.TakeOrdered(max, _taken);

        foreach (var (handle, action) in _taken)
            switch (action)
            {
                case SharedWrite shared:
                    if (Acquire(shared.Source.Handle, shared, acquirer) &&
                        Acquire(shared.Destination.Handle, shared, acquirer))
                        AddStep(UploadBatchStepKind.Copy, shared.Source.Handle, shared: shared);
                    break;
                case PendingTextureWrite write:
                    if (!Acquire(handle, write, acquirer)) break;
                    AddStep(UploadBatchStepKind.TextureWrite, handle, write);
                    StagingSize += write.StagingSize;
                    break;
                case PendingBufferWrite write:
                    if (!Acquire(handle, write, acquirer)) break;
                    GetBufferStep(handle).BufferWrites.Add(write);
                    StagingSize += write.StagingSize;
                    break;
            }

        _taken.Clear();
        _refused.Clear();
    }

    public void CompleteWrites()
    {
        foreach (var step in Steps) step.Complete();
    }

    public void Clear()
    {
        Steps.Clear();
        Acquired.Clear();
        StagingSize = 0;
    }

    private UploadBatchStep GetBufferStep(in ResourceHandle handle)
    {
        if (Steps.Count > 0 && Steps[^1] is { Kind: UploadBatchStepKind.BufferWrites } last && last.Handle.Equals(handle))
            return last;
        return AddStep(UploadBatchStepKind.BufferWrites, handle);
    }

    private UploadBatchStep AddStep(UploadBatchStepKind kind, in ResourceHandle handle,
        PendingTextureWrite? textureWrite = null, SharedWrite? shared = null)
    {
        if (_pool.Count == Steps.Count) _pool.Add(new UploadBatchStep());
        var step = _pool[Steps.Count];
        step.Reset(kind, handle, textureWrite, shared);
        Steps.Add(step);
        return step;
    }

    private bool Acquire(in ResourceHandle handle, PendingResourceAction action, IResourceAcquirer acquirer)
    {
        if (Acquired.Contains(handle)) return true;
        if (!_refused.Contains(handle) && acquirer.TryAcquireResource(handle))
        {
            Acquired.Add(handle);
            return true;
        }

        _refused.Add(handle);
        action.Dispose();
        return false;
    }
}
