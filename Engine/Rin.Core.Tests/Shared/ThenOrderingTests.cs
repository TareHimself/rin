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
            "outer continuation should have run once TextureHandleTask completed, inner should still be pending");

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
    ///     Regression guard for a real bug: VulkanGraphicsModule.Resources.cs's pending-resource sweep calls
    ///     <c>TaskCompletionSource.SetResult</c> and only afterwards flips the resource's state to Ready
    ///     (Resources.cs:605 then :609). If Then ever runs its continuation with
    ///     <see cref="TaskContinuationOptions.ExecuteSynchronously" />, the continuation executes inline as part of
    ///     the SetResult call - i.e. it can observe the resource still mid-transition, before the "state = Ready"
    ///     line runs - which is exactly what produced an "IsValid ... still Uploading" assertion failure in
    ///     MtsdfPageManager. <paramref name="resource" /> here is a mutable reference read live inside the
    ///     continuation (like the real <c>resource.State</c> field), not captured by value, so this actually
    ///     distinguishes inline-synchronous execution from deferred execution.
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
        resource.Value = "Ready"; // mirrors VulkanGraphicsModule.Resources.cs:609, right after SetResult at :605

        continuation.Wait();

        Assert.That(observed.Value, Is.EqualTo("Ready"),
            "continuation observed the resource mid-transition (still \"Uploading\") - Then must not run inline " +
            "during SetResult, or it races the producer's post-completion bookkeeping");
    }
}
