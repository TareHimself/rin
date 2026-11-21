using BenchmarkDotNet.Attributes;

namespace Rin.World.Benchmarks;

[MemoryDiagnoser]
public class SnapshotBenchmarks
{
    private World _world = null!;
    private WorldSnapshot _snapshot = null!;

    [Params(10_000, 100_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        WorldFixture.Populate(_world, Count);
        _snapshot = new WorldSnapshot();
        _world.SaveSnapshot(_snapshot);
    }

    [Benchmark]
    public void Save() => _world.SaveSnapshot(_snapshot);

    [Benchmark]
    public void Restore() => _world.RestoreSnapshot(_snapshot);
}
