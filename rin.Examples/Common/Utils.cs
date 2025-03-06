using Rin.Engine.Core;

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
        var mainMutex = new AutoResetEvent(false);
        var renderBegin = new AutoResetEvent(false);

        int mains = 0;
        int renders = 0;
        Task.Factory.StartNew(() =>
        {
            while (SEngine.Get().IsRunning)
            {
                mainMutex.WaitOne();
                try
                {
                    render();
                    renders++;
                }
                catch (Exception e)
                {
                    // ignored
                }
            }
        },TaskCreationOptions.LongRunning);
        
        SEngine.Get().OnUpdate += (dt) =>
        {
            main(dt);
            mains++;
            mainMutex.Set();
        };
    }
    
    public static void RunSingleThreaded(Action<double> main, Action render)
    {
        SEngine.Get().OnUpdate += (dt) =>
        {
            main(dt);
            render();
        };
    }
}