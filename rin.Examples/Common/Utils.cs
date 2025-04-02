using Rin.Engine.Core;

namespace rin.Examples.Common;

public static class Utils
{
    public static void RunMultithreaded(Action<float> main, Action render)
    {
        var mainMutex = new AutoResetEvent(false);
        var mains = 0;
        var renders = 0;
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
        }, TaskCreationOptions.LongRunning);

        SEngine.Get().OnUpdate += dt =>
        {
            main(dt);
            mains++;
            mainMutex.Set();
        };
    }

    public static void RunSingleThreaded(Action<float> main, Action render)
    {
        SEngine.Get().OnUpdate += dt =>
        {
            main(dt);
            render();
        };
    }
}