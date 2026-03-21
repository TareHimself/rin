using Rin.Framework.Shared;

namespace Rin.Framework.Tests.Shared;

public class DispatcherTests
{
    [Test]
    public void EnqueueAndDispatchOnMain([Random(1, 500, 5)] int dispatchCount)
    {
        var count = 0;

        var dispatcher = new Dispatcher();
        for (var i = 0; i < dispatchCount; i++) dispatcher.Enqueue(() => { count++; });

        Assert.That(count, Is.EqualTo(0), "dispatch happened before DispatchPending was called");
        dispatcher.DispatchPending();
        Assert.That(count, Is.EqualTo(dispatchCount), $"Expected {dispatchCount} dispatches but got {count}");
    }

    [Test]
    public void EnqueueInThreadAndDispatchOnMain([Random(1, 500, 5)] int dispatchCount)
    {
        var count = 0;

        var dispatcher = new Dispatcher();

        Task.Run(() =>
        {
            for (var i = 0; i < dispatchCount; i++) dispatcher.Enqueue(() => { count++; });
        }).Wait();

        Assert.That(count, Is.EqualTo(0), "dispatch happened before DispatchPending was called");
        dispatcher.DispatchPending();
        Assert.That(count, Is.EqualTo(dispatchCount), $"Expected {dispatchCount} dispatches but got {count}");
    }

    [Test]
    public void EnqueueInMainAndDispatchOnThread([Random(1, 500, 5)] int dispatchCount)
    {
        var count = 0;

        var dispatcher = new Dispatcher();
        for (var i = 0; i < dispatchCount; i++) dispatcher.Enqueue(() => { count++; });

        Assert.That(count, Is.EqualTo(0), "dispatch happened before DispatchPending was called");
        Task.Run(dispatcher.DispatchPending).Wait();
        Assert.That(count, Is.EqualTo(dispatchCount), $"Expected {dispatchCount} dispatches but got {count}");
    }
}