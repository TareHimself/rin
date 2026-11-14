using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.Passes;

namespace Rin.Core.Tests.Views.Graphics.Passes;

public class ViewsDrawPassTests
{
    private static ViewsDrawPass PassOver(List<ICommandHandler> handlers)
    {
        var context = new SurfaceContext(new Extent2D { Width = 1, Height = 1 });
        return new ViewsDrawPass(context, null!, handlers.ToArray());
    }

    [Test]
    public void KeepsItsHandlersAfterTheCallerReusesItsList()
    {
        var handler = new FakeHandler();
        List<ICommandHandler> handlers = [handler];
        var pass = PassOver(handlers);

        handlers.Clear();
        pass.PreAdd(null!);

        Assert.That(handler.PreAdds, Is.EqualTo(1));
    }

    [Test]
    public void DisposesEachHandlerOnce()
    {
        var first = new FakeHandler();
        var second = new FakeHandler();
        var pass = PassOver([first, second]);

        pass.Dispose();
        pass.Dispose();

        Assert.That(first.Disposals, Is.EqualTo(1));
        Assert.That(second.Disposals, Is.EqualTo(1));
    }

    [Test]
    public void StopsUsingItsHandlersOnceDisposed()
    {
        var handler = new FakeHandler();
        var pass = PassOver([handler]);

        pass.Dispose();
        pass.PreAdd(null!);

        Assert.That(handler.PreAdds, Is.Zero);
    }

    private sealed class FakeHandler : ICommandHandlerWithPreAdd, IDisposable
    {
        public int PreAdds { get; private set; }
        public int Disposals { get; private set; }

        public void Init(ReadOnlySpan<ICommand> commands)
        {
        }

        public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
        {
        }

        public void Execute(IPassConfig passConfig, SurfaceContext surfaceContext, ICompiledGraph graph,
            IExecutionContext ctx)
        {
        }

        public void PreAdd(IGraphBuilder builder)
        {
            PreAdds++;
        }

        public void Dispose()
        {
            Disposals++;
        }
    }
}
