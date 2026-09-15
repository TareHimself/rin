using System.Runtime.CompilerServices;
using Rin.Core.Shared;

namespace Rin.Core.Extensions;

public static class TaskExtensions
{

    public static IEnumerable<T> WaitAll<T>(this IEnumerable<Task<T>> tasks)
    {
        var taskArray = tasks as Task<T>[] ?? tasks.ToArray();
        Task.WaitAll(taskArray);
        var results = new T[taskArray.Length];
        for (var i = 0; i < taskArray.Length; i++)
        {
            results[i] = taskArray[i].Result;
        }

        return results;
    }

    public static Task<TV> Then<T, TV>(this Task<T> task, Func<T, TV> then)
    {
        return task.Then(static (result, callback) => callback(result), then);
    }

    public static Task<TV> Then<TV>(this Task task, Func<TV> then)
    {
        return task.Then(static callback => callback(), then);
    }

    public static Task Then<T>(this Task<T> task, Action<T> then)
    {
        return task.Then(static (result, callback) => callback(result), then);
    }

    public static Task Then(this Task task, Action then)
    {
        return task.Then(static callback => callback(), then);
    }

    // Deliberately no ExecuteSynchronously: some SetResult call sites (e.g. VulkanGraphicsModule.Resources.cs's
    // pending-resource sweep) do bookkeeping right after completing the task, and ExecuteSynchronously would run
    // the continuation inline before that bookkeeping happens.
    public static Task<TV> Then<T, TState, TV>(this Task<T> task, Func<T, TState, TV> then, TState state)
    {
        return task.ContinueWith(static (a, s) =>
        {
            var (callback, arg) = ((Func<T, TState, TV>, TState))s!;
            return callback(a.Result, arg);
        }, (then, state));
    }

    public static Task<TV> Then<TState, TV>(this Task task, Func<TState, TV> then, TState state)
    {
        return task.ContinueWith(static (_, s) =>
        {
            var (callback, arg) = ((Func<TState, TV>, TState))s!;
            return callback(arg);
        }, (then, state));
    }

    public static Task Then<T, TState>(this Task<T> task, Action<T, TState> then, TState state)
    {
        return task.ContinueWith(static (a, s) =>
        {
            var (callback, arg) = ((Action<T, TState>, TState))s!;
            callback(a.Result, arg);
        }, (then, state));
    }

    public static Task Then<TState>(this Task task, Action<TState> then, TState state)
    {
        return task.ContinueWith(static (_, s) =>
        {
            var (callback, arg) = ((Action<TState>, TState))s!;
            callback(arg);
        }, (then, state));
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
        return task.ContinueWith(static (t, s) =>
        {
            var (dispatcher, then) = ((Dispatcher, Action<T>))s!;
            dispatcher.Enqueue(static state => state.then(state.result), (then, result: t.Result));
        }, (dispatcher, then)).ConfigureAwait(false);
    }

    public static ConfiguredTaskAwaitable Dispatch(this Task task, Dispatcher dispatcher, Action then)
    {
        return task.ContinueWith(static (_, s) =>
        {
            var (dispatcher, then) = ((Dispatcher, Action))s!;
            dispatcher.Enqueue(then);
        }, (dispatcher, then)).ConfigureAwait(false);
    }

    public static ConfiguredTaskAwaitable Dispatch<T, TState>(this Task<T> task, Dispatcher dispatcher,
        Action<T, TState> then, TState state)
    {
        return task.ContinueWith(static (t, s) =>
        {
            var (dispatcher, then, state) = ((Dispatcher, Action<T, TState>, TState))s!;
            dispatcher.Enqueue(static st => st.then(st.result, st.state), (then, result: t.Result, state));
        }, (dispatcher, then, state)).ConfigureAwait(false);
    }

    public static ConfiguredTaskAwaitable Dispatch<TState>(this Task task, Dispatcher dispatcher,
        Action<TState> then, TState state)
    {
        return task.ContinueWith(static (_, s) =>
        {
            var (dispatcher, then, state) = ((Dispatcher, Action<TState>, TState))s!;
            dispatcher.Enqueue(then, state);
        }, (dispatcher, then, state)).ConfigureAwait(false);
    }

    public static ConfiguredTaskAwaitable DispatchMain<T>(this Task<T> task, Action<T> then)
    {
        return task.Dispatch(IApplication.Get().MainDispatcher, then);
    }

    public static ConfiguredTaskAwaitable DispatchMain(this Task task, Action then)
    {
        return task.Dispatch(IApplication.Get().MainDispatcher, then);
    }

    public static ConfiguredTaskAwaitable DispatchMain<T, TState>(this Task<T> task, Action<T, TState> then,
        TState state)
    {
        return task.Dispatch(IApplication.Get().MainDispatcher, then, state);
    }

    public static ConfiguredTaskAwaitable DispatchMain<TState>(this Task task, Action<TState> then, TState state)
    {
        return task.Dispatch(IApplication.Get().MainDispatcher, then, state);
    }

    public static ConfiguredTaskAwaitable DispatchRender<T>(this Task<T> task, Action<T> then)
    {
        return task.Dispatch(IApplication.Get().RenderDispatcher, then);
    }

    public static ConfiguredTaskAwaitable DispatchRender(this Task task, Action then)
    {
        return task.Dispatch(IApplication.Get().RenderDispatcher, then);
    }

    public static ConfiguredTaskAwaitable DispatchRender<T, TState>(this Task<T> task, Action<T, TState> then,
        TState state)
    {
        return task.Dispatch(IApplication.Get().RenderDispatcher, then, state);
    }

    public static ConfiguredTaskAwaitable DispatchRender<TState>(this Task task, Action<TState> then, TState state)
    {
        return task.Dispatch(IApplication.Get().RenderDispatcher, then, state);
    }

    public static T WaitForResult<T>(this Task<T> task)
    {
        task.Wait();
        return task.Result;
    }
}