using BenchmarkDotNet.Attributes;
using Rin.World.Scheduling;

namespace Rin.World.Benchmarks;

/// <summary>
///     A frame system that reads the rows a <c>Changed</c> query reports, with nothing written and with 1% of the rows
///     written every frame, against the same system reading every row of a plain query.
/// </summary>
[MemoryDiagnoser]
public class ChangedQueryBenchmarks
{
    private const int WrittenStride = 100;

    private World _world = null!;
    private Entity[] _written = [];

    [Params(10_000, 100_000)]
    public int Count { get; set; }

    [Params(false, true)]
    public bool TrackChanges { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        var entities = WorldFixture.PopulateFour(_world, Count);
        _written = new Entity[Count / WrittenStride];
        for (var i = 0; i < _written.Length; i++) _written[i] = entities[i * WrittenStride];

        var query = _world.Query<Position, Velocity>();
        _world.AddSystem(FramePhase.Update, new PositionReader(TrackChanges ? query.Changed<Position>() : query, TrackChanges));
        _world.Update(0f);
    }

    [Benchmark]
    public void StillScene() => _world.Update(0f);

    [Benchmark]
    public void OnePercentWritten()
    {
        foreach (var entity in _written) _world.Write<Position>(entity).X += 1f;
        _world.Update(0f);
    }

    private sealed class PositionReader(Query<Position, Velocity> query, bool trackChanges) : FrameSystem
    {
        public float Sum { get; private set; }

        public override void Update(in FrameContext context)
        {
            foreach (var chunk in query)
                for (var i = 0; i < chunk.Length; i++)
                    if (!trackChanges || chunk.HasChanged<Position>(i))
                        Sum += chunk.Components1[i].X + chunk.Components2[i].X;
        }
    }
}
