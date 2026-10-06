using Rin.Core.Graphics;

namespace RenderGraphOverlay.Snapshot;

public enum PassCategory
{
    Views,
    World,
    Backend,
    Core,
    App
}

public sealed record ResourceUseSnapshot(uint ResourceId, ResourceOperation Operation, string State);

public sealed record PassSnapshot(
    uint Id,
    string Name,
    PassCategory Category,
    int Stage,
    bool IsBarrier,
    IReadOnlyList<ResourceUseSnapshot> Uses,
    IReadOnlyList<string> Fields);

public sealed record StageSnapshot(int Index, string Title, bool IsBarrier, IReadOnlyList<PassSnapshot> Passes);

public sealed record ResourceSnapshot(uint Id, string Label, bool IsExternal);

public sealed record LinkSnapshot(uint ResourceId, uint FromPassId, uint ToPassId);

public sealed record GraphSnapshot(
    DateTime CapturedAt,
    IReadOnlyList<StageSnapshot> Stages,
    IReadOnlyDictionary<uint, ResourceSnapshot> Resources,
    IReadOnlyList<LinkSnapshot> Links,
    int PrunedPassCount)
{
    public int PassCount => Stages.Where(s => !s.IsBarrier).Sum(s => s.Passes.Count);
}

public sealed record CaptureResult(GraphSnapshot? Snapshot, string? Error);
