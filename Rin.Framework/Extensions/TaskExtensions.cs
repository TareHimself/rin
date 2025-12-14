using System.Runtime.CompilerServices;
using Rin.Framework.Shared;

namespace Rin.Framework.Extensions;

public static class TaskExtensions
{
    public static IEnumerable<T> WaitAll<T>(this IEnumerable<Task<T>> tasks)
    {
        return tasks.Select(c => c.WaitForResult());
    }
    

    public static Task<TV> Then<T, TV>(this Task<T> task, Func<T, TV> then)
    {
        return task.ContinueWith(a => then(a.Result));
    }

    public static Task<TV> Then<TV>(this Task task, Func<TV> then)
    {
        return task.ContinueWith(_ => then());
    }

    public static Task Then<T>(this Task<T> task, Action<T> then)
    {
        return task.ContinueWith(a => then(a.Result));
    }

    public static Task Then(this Task task, Action then)
    {
        return task.ContinueWith(_ => then());
    }

    public static ConfiguredTaskAwaitable After<T>(this Task<T> task, Action<T> then)
    {
        return task.Then(then).ConfigureAwait(false);
    }

    public static ConfiguredTaskAwaitable After(this Task task, Action then)
    {
        return task.Then(then).ConfigureAwait(false);
    }

    public static ConfiguredTaskAwaitable Dispatch<T>(this Task<T> task, Dispatcher dispatcher, Action<T> then)
    {
        return task.ContinueWith(c => { dispatcher.Enqueue(() => then(c.Result)); }).ConfigureAwait(false);
    }

    public static ConfiguredTaskAwaitable Dispatch(this Task task, Dispatcher dispatcher, Action then)
    {
        return task.ContinueWith(_ => { dispatcher.Enqueue(then); }).ConfigureAwait(false);
    }

    public static ConfiguredTaskAwaitable DispatchMain<T>(this Task<T> task, Action<T> then)
    {
        return task.Dispatch(IApplication.Get().MainDispatcher, then);
    }

    public static ConfiguredTaskAwaitable DispatchMain(this Task task, Action then)
    {
        return task.Dispatch(IApplication.Get().MainDispatcher, then);
    }

    public static ConfiguredTaskAwaitable DispatchRender<T>(this Task<T> task, Action<T> then)
    {
        return task.Dispatch(IApplication.Get().RenderDispatcher, then);
    }

    public static ConfiguredTaskAwaitable DispatchRender(this Task task, Action then)
    {
        return task.Dispatch(IApplication.Get().RenderDispatcher, then);
    }

    public static T WaitForResult<T>(this Task<T> task)
    {
        task.Wait();
        return task.Result;
    }
}