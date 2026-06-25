using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Content;
using Rin.Core.Views.Graphics.Quads;

namespace misc.StrokeExpansion;

public class MainModule : IModule, ISingletonGetter<MainModule>
{
    public void Start(IApplication app)
    {
        IGraphicsModule.Get().OnWindowCreated += window =>
        {
            window.OnClose += _ =>
            {
                window.Dispose();
                SFramework.Get().RequestExit();
            };
        };
        SViewsModule.Get().OnSurfaceCreated += surface =>
        {
            surface.Add(new CanvasView
            {
                Paint = (self, transform, cmds) => { cmds.AddText(transform, "Noto Sans", "Yo"); }
            });
        };
        IGraphicsModule.Get().CreateWindow("Stroke Expansion", new Extent2D(500),
            WindowFlags.Focused | WindowFlags.Resizable | WindowFlags.Visible);
    }

    public void Stop(IApplication app)
    {
    }

    public static MainModule Get()
    {
        return SFramework.Get().GetModule<MainModule>();
    }
}