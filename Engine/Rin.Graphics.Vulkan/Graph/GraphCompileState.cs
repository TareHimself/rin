using Rin.Core.Graphics.Graph;

namespace Rin.Graphics.Vulkan.Graph;

/// <summary>
///     The working collections of one compile. A builder keeps one and clears it, so they keep their capacity.
/// </summary>
internal sealed class GraphCompileState
{
    private readonly CollectionPool<List<BufferResourceSync>, BufferResourceSync> _bufferSyncLists = new();
    private readonly CollectionPool<HashSet<uint>, uint> _idSets = new();
    private readonly CollectionPool<List<ImageResourceSync>, ImageResourceSync> _imageSyncLists = new();

    public readonly HashSet<uint> Roots = [];

    /// <summary>
    ///     Each live pass and the passes it depends on.
    /// </summary>
    public readonly Dictionary<uint, HashSet<uint>> Dependencies = [];

    /// <summary>
    ///     Each pass and the live passes that depend on it.
    /// </summary>
    public readonly Dictionary<uint, HashSet<uint>> Dependents = [];

    public readonly Queue<uint> Pending = [];
    public readonly HashSet<uint> Visited = [];
    public readonly Dictionary<uint, List<BufferResourceSync>> BufferSyncs = [];
    public readonly Dictionary<uint, List<ImageResourceSync>> ImageSyncs = [];
    public readonly Dictionary<uint, int> Levels = [];
    public readonly Dictionary<uint, int> RemainingDependencies = [];
    public readonly List<List<IPass>> PassesByLevel = [];

    public bool IsLive(uint passId)
    {
        return Dependencies.ContainsKey(passId);
    }

    public HashSet<uint> AddLivePass(uint passId)
    {
        var dependencies = _idSets.Rent();
        Dependencies[passId] = dependencies;
        return dependencies;
    }

    public void AddDependent(uint passId, uint dependentId)
    {
        if (!Dependents.TryGetValue(passId, out var dependents)) Dependents[passId] = dependents = _idSets.Rent();
        dependents.Add(dependentId);
    }

    public void AddSync(uint passId, in BufferResourceSync sync)
    {
        if (!BufferSyncs.TryGetValue(passId, out var syncs)) BufferSyncs[passId] = syncs = _bufferSyncLists.Rent();
        syncs.Add(sync);
    }

    public void AddSync(uint passId, in ImageResourceSync sync)
    {
        if (!ImageSyncs.TryGetValue(passId, out var syncs)) ImageSyncs[passId] = syncs = _imageSyncLists.Rent();
        syncs.Add(sync);
    }

    public void Clear()
    {
        Roots.Clear();
        _idSets.ReturnAll(Dependencies);
        _idSets.ReturnAll(Dependents);
        Pending.Clear();
        Visited.Clear();
        _bufferSyncLists.ReturnAll(BufferSyncs);
        _imageSyncLists.ReturnAll(ImageSyncs);
        Levels.Clear();
        RemainingDependencies.Clear();
        foreach (var passes in PassesByLevel) passes.Clear();
    }
}
