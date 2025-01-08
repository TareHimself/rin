using rin.Framework.Core;

namespace rin.Framework.Graphics;

public static class Utils
{
    public static void RunWindowsOnTick()
    {
        SRuntime.Get().OnTick += (_) =>
        {
            SGraphicsModule.Get().PollWindows();
        };

    }
    public static void RunDrawOnTick()
    {
        SRuntime.Get().OnTick += (_) =>
        {
            SGraphicsModule.Get().DrawWindows();
        };
    }

    public static Task RunDrawOnThread()
    {
        return Task.Factory.StartNew(() =>
        {
            while (SRuntime.Get().IsRunning)
            {
                SGraphicsModule.Get().DrawWindows();
            }
        },TaskCreationOptions.LongRunning);
    }
}