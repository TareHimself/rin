using System.Runtime.CompilerServices;

namespace aerox.Runtime.Extensions;

public static class TaskExtensions
{
    public static async Task<TV> Then<T, TV>(this Task<T> task, Func<T, TV> then)
    {
        var result = await task;
        return then(result);
    }

    public static async Task<TV> Then<TV>(this Task task, Func<TV> then)
    {
        await task;
        return then();
    }

    public static async Task Then<T>(this Task<T> task, Action<T> then)
    {
        then.Invoke(await task);
    }

    public static async Task Then(this Task task, Action then)
    {
        await task;
        then();
    }

    public static ConfiguredTaskAwaitable After<T>(this Task<T> task, Action<T> then) => task.Then(then).ConfigureAwait(false);
    
    public static ConfiguredTaskAwaitable After(this Task task, Action then) => task.Then(then).ConfigureAwait(false);

    public static T WaitForResult<T>(this Task<T> task)
    {
        task.Wait();
        return task.Result;
    }
}