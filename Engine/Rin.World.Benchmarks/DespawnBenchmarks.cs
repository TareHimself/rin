using BenchmarkDotNet.Attributes;

namespace Rin.World.Benchmarks;

[MemoryDiagnoser]
public class DespawnBenchmarks
{
    private World _world = null!;
    private Entity[] _entities = null!;

    [Params(10_000, 100_000)]
    public int Count { get; set; }

    [IterationSetup]
    public void Setup()
    {
        _world = new World();
        _entities = WorldFixture.Populate(_world, Count);
    }

    [Benchmark]
    public void DespawnAll()
    {
        for (var i = 0; i < _entities.Length; i++) _world.Despawn(_entities[i]);
    }
}
