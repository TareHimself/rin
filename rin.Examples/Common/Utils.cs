using rin.Framework.Core;

namespace rin.Examples.Common;

public static class Utils
{
    public static void RunMultithreaded(Action<double> main, Action render)
    {
        // var mainMutex = new ManualResetEvent(false);
        // var renderMutex = new ManualResetEvent(true);
        //
        // SRuntime.Get().OnUpdate += (dt) =>
        // {
        //     renderMutex.WaitOne();
        //     main(dt);
        //     mainMutex.Set();
        // };
        //
        // Task.Factory.StartNew(() =>
        // {
        //     while (SRuntime.Get().IsRunning)
        //     {
        //         mainMutex.WaitOne();
        //         try
        //         {
        //             render();
        //         }
        //         catch (Exception e)
        //         {
        //             // ignored
        //         }
        //
        //         renderMutex.Set();
        //     }
        // },TaskCreationOptions.LongRunning);
        var mainMutex = new ManualResetEvent(false);
        var renderMutex = new ManualResetEvent(true);
        
        SRuntime.Get().OnUpdate += (dt) =>
        {
            main(dt);
            mainMutex.Set();
        };
        
        Task.Factory.StartNew(() =>
        {
            while (SRuntime.Get().IsRunning)
            {
                mainMutex.WaitOne();
                try
                {
                    render();
                }
                catch (Exception e)
                {
                    // ignored
                }
            }
        },TaskCreationOptions.LongRunning);
    }
    
    public static void RunSingleThreaded(Action<double> main, Action render)
    {
        SRuntime.Get().OnUpdate += (dt) =>
        {
            main(dt);
            render();
        };
    }
}