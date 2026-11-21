using BenchmarkDotNet.Attributes;

namespace Rin.World.Benchmarks;

[MemoryDiagnoser]
public class EntityBenchmarks
{
    private World _world = null!;
    private Entity[] _entities = null!;
    private int[] _order = null!;

    [Params(10_000, 100_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        _entities = WorldFixture.Populate(_world, Count);
        _order = WorldFixture.ShuffledIndices(Count);
    }

    [Benchmark]
    public World SpawnAndAddThreeComponents()
    {
        var world = new World();
        for (var i = 0; i < Count; i++) WorldFixture.SpawnWithThree(world, i);
        return world;
    }

    [Benchmark]
    public void AddThenRemoveOneComponent()
    {
        var entities = _entities;
        var world = _world;
        for (var i = 0; i < entities.Length; i++) world.Add(entities[i], new Marker { Value = i });
        for (var i = 0; i < entities.Length; i++) world.Remove<Marker>(entities[i]);
    }

    [Benchmark]
    public float RandomGet()
    {
        var sum = 0f;
        for (var i = 0; i < _order.Length; i++) sum += _world.Get<Position>(_entities[_order[i]]).X;
        return sum;
    }

    [Benchmark]
    public int RandomHas()
    {
        var found = 0;
        for (var i = 0; i < _order.Length; i++)
            if (_world.Has<Health>(_entities[_order[i]])) found++;
        return found;
    }
}
