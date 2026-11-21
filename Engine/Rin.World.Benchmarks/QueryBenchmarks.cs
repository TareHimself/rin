using BenchmarkDotNet.Attributes;

namespace Rin.World.Benchmarks;

[MemoryDiagnoser]
public class QueryBenchmarks
{
    private World _world = null!;

    [Params(10_000, 100_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        WorldFixture.PopulateFour(_world, Count);
    }

    [Benchmark]
    public void IterateOne()
    {
        foreach (var chunk in _world.Query<Position>())
            for (var i = 0; i < chunk.Length; i++)
                chunk.Write<Position>(i).X += 1f;
    }

    [Benchmark]
    public void IterateTwo()
    {
        foreach (var chunk in _world.Query<Position, Velocity>())
        {
            var velocities = chunk.Components2;
            for (var i = 0; i < chunk.Length; i++) chunk.Write<Position>(i).X += velocities[i].X;
        }
    }

    [Benchmark]
    public void IterateFour()
    {
        foreach (var chunk in _world.Query<Position, Velocity, Health, Mass>())
        {
            var velocities = chunk.Components2;
            var healths = chunk.Components3;
            var masses = chunk.Components4;
            for (var i = 0; i < chunk.Length; i++)
                chunk.Write<Position>(i).X += velocities[i].X * masses[i].Value + healths[i].Value;
        }
    }
}
