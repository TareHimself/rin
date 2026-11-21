using BenchmarkDotNet.Attributes;

namespace Rin.World.Benchmarks;

[MemoryDiagnoser]
public class CommandBufferBenchmarks
{
    private World _world = null!;
    private Entity[] _entities = null!;

    [Params(10_000, 100_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        _entities = WorldFixture.Populate(_world, Count);
    }

    [Benchmark]
    public void RecordAndPlayBackAddThenRemove()
    {
        var commands = _world.Commands;
        for (var i = 0; i < _entities.Length; i++) commands.Add(_entities[i], new Marker { Value = i });
        commands.Playback();
        for (var i = 0; i < _entities.Length; i++) commands.Remove<Marker>(_entities[i]);
        commands.Playback();
    }
}
