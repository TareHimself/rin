using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Windows;
using Rin.Graphics.Vulkan.Windows;

namespace Rin.Graphics.Vulkan.Tests;

public class FrameCollectorTests
{
    private FrameCollector _frames = null!;
    private readonly List<RinWindow> _windows = [];

    [SetUp]
    public void CreateCollector()
    {
        FakeNative.Reset();
        _frames = new FrameCollector();
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
        _frames.Execute();

        Assert.That(renderer.Executed, Has.Count.EqualTo(1));
    }

    private sealed class RecordingWindowRenderer(IWindow window) : IWindowRenderer
    {
        public List<Data> Executed { get; } = [];
        public bool VsyncEnabled => false;
        public event Action<IGraphCollector>? OnCollect;

        public IRenderData? Collect()
        {
            OnCollect?.Invoke(null!);
            return new Data(this);
        }

        public void Execute(IRenderData context) => Executed.Add((Data)context);
        public IWindow GetWindow() => window;
        public Extent2D GetRenderExtent() => new(640, 480);
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
