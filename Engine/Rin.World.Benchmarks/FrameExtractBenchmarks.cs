using System.Numerics;
using BenchmarkDotNet.Attributes;
using Rin.World.Graphics;
using Rin.World.Transforms;

namespace Rin.World.Benchmarks;

/// <summary>
///     One frame of transform propagation plus render extract over a scene of static meshes, with nothing moving and
///     with 1% of the meshes moving every frame.
/// </summary>
[MemoryDiagnoser]
public class FrameExtractBenchmarks
{
    private const int MoverStride = 100;

    private World _world = null!;
    private Entity[] _movers = [];

    [Params(10_000, 100_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        _ = new RenderFeature(_world, new NullRenderSystem(), new NullMeshFactory());

        _movers = new Entity[Count / MoverStride];
        for (var i = 0; i < Count; i++)
        {
            var entity = _world.Spawn();
            _world.SetPosition(entity, new Vector3(i, 0, 0));
            _world.Add(entity, new StaticMesh { Mesh = NullMeshFactory.Mesh });
            if (i % MoverStride == 0) _movers[i / MoverStride] = entity;
        }

        _world.Update(0f);
        _world.Update(0f);
    }

    [Benchmark]
    public void StillScene() => _world.Update(0f);

    [Benchmark]
    public void OnePercentMoves()
    {
        foreach (var mover in _movers) _world.Write<LocalTransform>(mover).Position.Y += 1f;
        _world.Update(0f);
    }
}
