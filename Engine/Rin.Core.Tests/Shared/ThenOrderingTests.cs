using System.Runtime.CompilerServices;
using Rin.Core.Extensions;

namespace Rin.Core.Tests.Shared;

public class ThenOrderingTests
{
    private sealed class ChainState(Task uploadTask, List<string> log, TaskCompletionSource done)
    {
        public readonly Task UploadTask = uploadTask;
        public readonly List<string> Log = log;
        public readonly TaskCompletionSource Done = done;
    }

    [Test]
    public async Task NestedStateThenWaitsForBothAntecedents()
    {
        var textureReady = new TaskCompletionSource<int>();
        var uploadReady = new TaskCompletionSource();
        var done = new TaskCompletionSource();
        var log = new List<string>();

        var state = new ChainState(uploadReady.Task, log, done);

        _ = textureReady.Task.Then(static (handle, s) =>
        {
            s.Log.Add($"outer-start:{handle}");
            s.UploadTask.Then(static s2 =>
            {
                s2.Log.Add("inner-ran");
                s2.Done.SetResult();
            }, s);
        }, state);

        // Give the continuations every chance to run early/incorrectly before we complete anything.
        await Task.Delay(50);
        Assert.That(log, Is.Empty, "continuation ran before its antecedent completed");

        textureReady.SetResult(42);
        await Task.Delay(50);
        Assert.That(log, Is.EqualTo(new[] { "outer-start:42" }),
            "outer continuation should have run once its antecedent completed, inner should still be pending");

        uploadReady.SetResult();
        await Task.Delay(50);
        Assert.That(log, Is.EqualTo(new[] { "outer-start:42", "inner-ran" }),
            "inner continuation should only run once the upload task completed");
    }

    /// <summary>
    ///     Then must not run its continuation inline during <c>SetResult</c>, because producers do bookkeeping
    ///     right after completing the task.
    /// </summary>
    [Test]
    public void ThenDoesNotRunItsContinuationInlineDuringSetResult()
    {
        var continuationThread = new StrongBox<int>();
        var completionSource = new TaskCompletionSource<int>();
        var continuation = completionSource.Task.Then(
            static (_, thread) => thread.Value = Environment.CurrentManagedThreadId, continuationThread);

        completionSource.SetResult(0);
        continuation.Wait();

        Assert.That(continuationThread.Value, Is.Not.EqualTo(Environment.CurrentManagedThreadId));
    }
}
