using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Examples.RenderGraphOverlay.Snapshot;

public sealed class GraphSnapshotService : IDisposable
{
    private readonly IWindowRenderer _renderer;
    private int _requested;
    private CaptureResult? _result;

    public GraphSnapshotService(IWindowRenderer renderer)
    {
        _renderer = renderer;
        _renderer.OnCollect += OnCollect;
    }

    public void Dispose()
    {
        _renderer.OnCollect -= OnCollect;
    }

    public void Request()
    {
        Interlocked.Exchange(ref _requested, 1);
    }

    public CaptureResult? TakeResult()
    {
        return Interlocked.Exchange(ref _result, null);
    }

    private void OnCollect(IGraphCollector collector)
    {
        if (Interlocked.Exchange(ref _requested, 0) == 1) collector.Add(new ProbeData(this));
    }

    private void Publish(ICompiledGraph graph, IGraphConfig? config, IGraphBuilder builder, uint probePassId)
    {
        try
        {
            var snapshot = GraphSnapshotBuilder.Build(graph, config!, builder, probePassId);
            Volatile.Write(ref _result, new CaptureResult(snapshot, null));
        }
        catch (Exception e)
        {
            Volatile.Write(ref _result, new CaptureResult(null, $"{e.GetType().Name}: {e.Message}"));
        }
    }

    private sealed class ProbeData(GraphSnapshotService owner) : ICollectedData
    {
        public void Write(IGraphBuilder builder)
        {
            IGraphConfig? config = null;
            builder.AddPass(new TerminalActionPass(
                (_, passConfig) => config = passConfig,
                (pass, graph, _) => owner.Publish(graph, config, builder, pass.Id),
                "GraphSnapshotProbe"));
        }
    }
}
