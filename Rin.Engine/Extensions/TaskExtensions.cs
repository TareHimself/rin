﻿using System.Runtime.CompilerServices;

namespace Rin.Engine.Extensions;

public static class TaskExtensions
{
    public static IEnumerable<T> WaitAll<T>(this IEnumerable<Task<T>> tasks)
    {
        return tasks.Select(c => c.WaitForResult());
    }
    public static Task<TV> Then<T, TV>(this Task<T> task, Func<T, TV> then) => task.ContinueWith(a => then(a.Result));
    public static Task<TV> Then<TV>(this Task task, Func<TV> then) => task.ContinueWith(_ => then());
    public static Task Then<T>(this Task<T> task, Action<T> then)  => task.ContinueWith(a => then(a.Result));
    public static Task Then(this Task task, Action then) => task.ContinueWith(_ => then());
    public static ConfiguredTaskAwaitable After<T>(this Task<T> task, Action<T> then) => task.Then(then).ConfigureAwait(false);
    public static ConfiguredTaskAwaitable After(this Task task, Action then) => task.Then(then).ConfigureAwait(false);
    public static ConfiguredTaskAwaitable Dispatch<T>(this Task<T> task, Dispatcher dispatcher, Action<T> then) => task.ContinueWith(c =>
    {
        dispatcher.Enqueue(() => then(c.Result));
    }).ConfigureAwait(false);
    public static ConfiguredTaskAwaitable Dispatch(this Task task, Dispatcher dispatcher, Action then) => task.ContinueWith(_ =>
    {
        dispatcher.Enqueue(then);
    }).ConfigureAwait(false);
    public static ConfiguredTaskAwaitable DispatchMain<T>(this Task<T> task, Action<T> then) => task.Dispatch(SEngine.Get().GetMainDispatcher(), then);
    public static ConfiguredTaskAwaitable DispatchMain(this Task task, Action then) => task.Dispatch(SEngine.Get().GetMainDispatcher(), then);
    public static ConfiguredTaskAwaitable DispatchRender<T>(this Task<T> task, Action<T> then) => task.Dispatch(SEngine.Get().GetRenderDispatcher(), then);
    public static ConfiguredTaskAwaitable DispatchRender(this Task task, Action then) => task.Dispatch(SEngine.Get().GetRenderDispatcher(), then);

    public static T WaitForResult<T>(this Task<T> task)
    {
        task.Wait();
        return task.Result;
    }
}