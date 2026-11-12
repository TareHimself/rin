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

    private sealed class Box<T>
    {
        public T? Value;
    }

    /// <summary>
    ///     Then must not run its continuation inline during <c>SetResult</c>, or the continuation sees state the
    ///     producer updates right after completing the task. The continuation reads <c>resource</c> live so the test
    ///     can tell inline execution from deferred execution.
    /// </summary>
    [Test]
    public void ThenDoesNotObserveBookkeepingThatRunsAfterSetResult()
    {
        var resource = new Box<string> { Value = "Uploading" };
        var observed = new Box<string>();
        var completionSource = new TaskCompletionSource<int>();

        var continuation = completionSource.Task.Then(static (_, s) => s.observed.Value = s.resource.Value,
            (resource, observed));

        completionSource.SetResult(0);
        resource.Value = "Ready"; // the producer's bookkeeping after SetResult

        continuation.Wait();

        Assert.That(observed.Value, Is.EqualTo("Ready"),
            "continuation observed the resource mid-transition (still \"Uploading\") - Then must not run inline " +
            "during SetResult, or it races the producer's post-completion bookkeeping");
    }
}
