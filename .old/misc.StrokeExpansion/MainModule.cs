using Rin.Framework;
using Rin.Framework.Audio;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views;
using Rin.Framework.Views.Content;
using Rin.Framework.Views.Graphics.Quads;

namespace misc.StrokeExpansion;

[AlwaysLoad,Module(typeof(SViewsModule),typeof(SAudioModule))]
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
                Paint = (@self, transform, cmds) =>
                {
                    cmds.AddText(transform,"Noto Sans", "Yo");
                }
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