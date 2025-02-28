namespace Rin.Engine.Graphics;

public static class Utils
{
    // public static void RunSingleThreaded()
    // {
    //     SRuntime.Get().OnUpdate += (_) =>
    //     {
    //         SGraphicsModule.Get().PollWindows();
    //         SGraphicsModule.Get().Collect();
    //         SGraphicsModule.Get().Execute();
    //     };
    // }
    // public static void RunMultithreaded()
    // {
    //     var mainMutex = new ();
    //     var drawMutex = new Mutex();
    //     
    //     SRuntime.Get().OnUpdate += (_) =>
    //     {
    //         SGraphicsModule.Get().PollWindows();
    //         SGraphicsModule.Get().Collect();
    //         drawMutex.ReleaseMutex();
    //     };
    //     
    //     var drawThreadTask = Task.Factory.StartNew(() =>
    //     {
    //         while (SRuntime.Get().IsRunning)
    //         {
    //             drawMutex.WaitOne();
    //             SGraphicsModule.Get().Execute();
    //         }
    //     },TaskCreationOptions.LongRunning);
    // }
    // public static void RunDrawOnTick()
    // {
    //     SRuntime.Get().OnUpdate += (_) =>
    //     {
    //         SGraphicsModule.Get().Execute();
    //     };
    // }

    // public static Task RunDrawOnThread()
    // {
    //     return Task.Factory.StartNew(() =>
    //     {
    //         while (SRuntime.Get().IsRunning)
    //         {
    //             SGraphicsModule.Get().Execute();
    //         }
    //     },TaskCreationOptions.LongRunning);
    // }
}