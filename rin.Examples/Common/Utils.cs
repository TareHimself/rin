using Rin.Framework;

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
            while (SApplication.Get().IsRunning)
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

        SApplication.Get().OnUpdate += dt =>
        {
            main(dt);
            mains++;
            mainMutex.Set();
        };
    }

    public static void RunSingleThreaded(Action<float> main, Action render)
    {
        SApplication.Get().OnUpdate += dt =>
        {
            main(dt);
            render();
        };
    }
}