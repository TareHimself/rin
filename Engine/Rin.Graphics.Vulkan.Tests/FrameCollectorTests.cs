using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Windows;
using Rin.Graphics.Vulkan.Windows;

namespace Rin.Graphics.Vulkan.Tests;

public class FrameCollectorTests
{
    private FrameCollector _frames = null!;
    private readonly List<IRenderData> _taken = [];
    private readonly List<RinWindow> _windows = [];

    [SetUp]
    public void CreateCollector()
    {
        FakeNative.Reset();
        _frames = new FrameCollector();
        _taken.Clear();
    }

    [TearDown]
    public void DisposeWindows()
    {
        foreach (var window in _windows) window.Dispose();
        _windows.Clear();
    }

    private RinWindow NewWindow()
    {
        var window = new RinWindow(Native.CreateWindow("test", new Extent2D(640, 480), WindowFlags.None), null);
        _windows.Add(window);
        return window;
    }

    [Test]
    public void Collect_ThenTryTake_HandsOverEachRenderersOwnData()
    {
        var first = new RecordingRenderer();
        var second = new RecordingRenderer();
        _frames.Add(first);
        _frames.Add(second);

        _frames.Collect();
        var taken = _frames.TryTake(_taken);

        Assert.Multiple(() =>
        {
            Assert.That(taken, Is.True);
            Assert.That(_taken, Is.EqualTo(new[] { first.Collected[0], second.Collected[0] }));
            Assert.That(first.Collected[0].Disposed, Is.False);
        });
    }

    [Test]
    public void TryTake_WithoutACollect_HandsOverNothing()
    {
        _frames.Add(new RecordingRenderer());

        Assert.Multiple(() =>
        {
            Assert.That(_frames.TryTake(_taken), Is.False);
            Assert.That(_taken, Is.Empty);
        });
    }

    [Test]
    public void Collect_Twice_HandsOverTheNewestAndDisposesTheSkippedBatch()
    {
        var renderer = new RecordingRenderer();
        _frames.Add(renderer);

        _frames.Collect();
        _frames.Collect();
        _frames.TryTake(_taken);

        Assert.Multiple(() =>
        {
            Assert.That(renderer.Collected, Has.Count.EqualTo(2));
            Assert.That(_taken, Is.EqualTo(new[] { renderer.Collected[1] }));
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
        _frames.TryTake(_taken);

        Assert.That(_taken, Is.EqualTo(new[] { busy.Collected[0] }));
    }

    [Test]
    public void TryTake_DropsAndDisposesTheDataOfARendererRemovedAfterCollect()
    {
        var removed = new RecordingRenderer();
        var kept = new RecordingRenderer();
        _frames.Add(removed);
        _frames.Add(kept);
        _frames.Collect();

        _frames.Remove(removed);
        _frames.TryTake(_taken);

        Assert.Multiple(() =>
        {
            Assert.That(_taken, Is.EqualTo(new[] { kept.Collected[0] }));
            Assert.That(removed.Collected[0].Disposed, Is.True);
            Assert.That(kept.Collected[0].Disposed, Is.False);
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
    public void DisposeRemaining_DisposesABatchNobodyTook()
    {
        var renderer = new RecordingRenderer();
        _frames.Add(renderer);
        _frames.Collect();

        _frames.DisposeRemaining();

        Assert.That(renderer.Collected[0].Disposed, Is.True);
    }

    [Test]
    public void DisposeBatch_DisposesEveryEntry()
    {
        var renderer = new RecordingRenderer();
        _frames.Add(renderer);
        _frames.Collect();
        _frames.TryTake(_taken);

        FrameCollector.DisposeBatch(_taken);

        Assert.That(renderer.Collected[0].Disposed, Is.True);
    }

    [Test]
    public void Add_IndexesAWindowRendererByItsWindow()
    {
        var window = NewWindow();
        var other = NewWindow();
        var plain = new RecordingRenderer();
        var forWindow = new RecordingWindowRenderer(window);
        _frames.Add(plain);
        _frames.Add(forWindow);

        Assert.Multiple(() =>
        {
            Assert.That(_frames.GetWindowRenderer(window), Is.SameAs(forWindow));
            Assert.That(_frames.GetWindowRenderer(other), Is.Null);
            Assert.That(_frames.GetWindowRenderers(), Is.EqualTo(new[] { forWindow }));
            Assert.That(_frames.GetRenderers(), Is.EquivalentTo(new IRenderer[] { plain, forWindow }));
        });
    }

    [Test]
    public void Remove_ForgetsTheWindowRenderer()
    {
        var window = NewWindow();
        var renderer = new RecordingWindowRenderer(window);
        _frames.Add(renderer);

        _frames.Remove(renderer);

        Assert.Multiple(() =>
        {
            Assert.That(_frames.GetWindowRenderer(window), Is.Null);
            Assert.That(_frames.GetWindowRenderers(), Is.Empty);
            Assert.That(_frames.GetRenderers(), Is.Empty);
        });
    }

    [Test]
    public void Collect_IncludesWindowRenderersLikeAnyOther()
    {
        var renderer = new RecordingWindowRenderer(NewWindow());
        _frames.Add(renderer);

        _frames.Collect();
        _frames.TryTake(_taken);

        Assert.That(_taken, Has.Count.EqualTo(1));
    }

    private sealed class RecordingWindowRenderer(IWindow window) : IWindowRenderer
    {
        public bool VsyncEnabled => false;
        public event Action<IGraphCollector>? OnCollect;

        public IRenderData? Collect()
        {
            OnCollect?.Invoke(null!);
            return new Data(this);
        }

        public bool BeginFrame(IRenderData data, IGraphBuilder builder) => true;

        public void EndFrame(IExecutionContext ctx)
        {
        }

        public IWindow GetWindow() => window;
        public Extent2D GetRenderExtent() => new(640, 480);
        public uint DestinationImageId => 0;
        public void SetVsyncEnabled(bool enabled)
        {
        }

        public uint GetNumFramesInFlight() => 2;
        public void SetFramesInFlight(uint framesInFlight)
        {
        }

        public uint GetMaxTrueFramesInFlight() => 3;

        public void Dispose()
        {
        }
    }

    private sealed class RecordingRenderer : IRenderer
    {
        public bool Draws { get; init; } = true;
        public List<Data> Collected { get; } = [];

        public IRenderData? Collect()
        {
            if (!Draws) return null;
            var data = new Data(this);
            Collected.Add(data);
            return data;
        }

        public bool BeginFrame(IRenderData data, IGraphBuilder builder) => true;

        public void EndFrame(IExecutionContext ctx)
        {
        }

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
