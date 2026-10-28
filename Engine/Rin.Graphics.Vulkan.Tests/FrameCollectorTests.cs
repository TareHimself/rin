using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Tests;

public class FrameCollectorTests
{
    private FrameCollector _frames = null!;

    [SetUp]
    public void CreateCollector()
    {
        _frames = new FrameCollector();
    }

    [Test]
    public void Collect_ThenExecute_RunsEachRendererOnItsOwnData()
    {
        var first = new RecordingRenderer();
        var second = new RecordingRenderer();
        _frames.Add(first);
        _frames.Add(second);

        _frames.Collect();
        var executed = _frames.Execute();

        Assert.Multiple(() =>
        {
            Assert.That(executed, Is.True);
            Assert.That(first.Executed, Has.Count.EqualTo(1));
            Assert.That(second.Executed, Has.Count.EqualTo(1));
            Assert.That(first.Executed[0].Renderer, Is.SameAs(first));
            Assert.That(first.Executed[0].Disposed, Is.True);
        });
    }

    [Test]
    public void Execute_WithoutACollect_DoesNothing()
    {
        var renderer = new RecordingRenderer();
        _frames.Add(renderer);

        Assert.Multiple(() =>
        {
            Assert.That(_frames.Execute(), Is.False);
            Assert.That(renderer.Executed, Is.Empty);
        });
    }

    [Test]
    public void Collect_Twice_ExecutesTheNewestAndDisposesTheSkippedBatch()
    {
        var renderer = new RecordingRenderer();
        _frames.Add(renderer);

        _frames.Collect();
        _frames.Collect();
        _frames.Execute();

        Assert.Multiple(() =>
        {
            Assert.That(renderer.Collected, Has.Count.EqualTo(2));
            Assert.That(renderer.Executed, Is.EqualTo(new[] { renderer.Collected[1] }));
            Assert.That(renderer.Collected[0].Disposed, Is.True);
        });
    }

    [Test]
    public void Collect_SkipsARendererWithNothingToDraw()
    {
        var idle = new RecordingRenderer { Draws = false };
        var busy = new RecordingRenderer();
        _frames.Add(idle);
        _frames.Add(busy);

        _frames.Collect();
        _frames.Execute();

        Assert.Multiple(() =>
        {
            Assert.That(idle.Executed, Is.Empty);
            Assert.That(busy.Executed, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Remove_StopsCollectingFromThatRenderer()
    {
        var renderer = new RecordingRenderer();
        _frames.Add(renderer);
        _frames.Remove(renderer);

        _frames.Collect();

        Assert.Multiple(() =>
        {
            Assert.That(renderer.Collected, Is.Empty);
            Assert.That(_frames.GetRenderers(), Is.Empty);
        });
    }

    [Test]
    public void DisposeRemaining_DisposesABatchNobodyExecuted()
    {
        var renderer = new RecordingRenderer();
        _frames.Add(renderer);
        _frames.Collect();

        _frames.DisposeRemaining();

        Assert.Multiple(() =>
        {
            Assert.That(renderer.Collected[0].Disposed, Is.True);
            Assert.That(renderer.Executed, Is.Empty);
        });
    }

    private sealed class RecordingRenderer : IRenderer
    {
        public bool Draws { get; init; } = true;
        public List<Data> Collected { get; } = [];
        public List<Data> Executed { get; } = [];

        public IRenderData? Collect()
        {
            if (!Draws) return null;
            var data = new Data(this);
            Collected.Add(data);
            return data;
        }

        public void Execute(IRenderData context) => Executed.Add((Data)context);

        public void Dispose()
        {
        }
    }

    private sealed class Data(IRenderer renderer) : IRenderData, IDisposable
    {
        public IRenderer Renderer { get; } = renderer;
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }
}
