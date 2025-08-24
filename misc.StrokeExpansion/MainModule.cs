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
    public void Start(SApplication application)
    {

        SGraphicsModule.Get().OnWindowCreated += window =>
        {
            window.OnClose += _ =>
            {
                window.Dispose();
                SApplication.Get().RequestExit();
            };
        };
        SViewsModule.Get().OnSurfaceCreated += surface =>
        {
            surface.Add(new Canvas
            {
                Paint = (@self, transform, cmds) =>
                {
                    cmds.AddText(transform,"Noto Sans", "Yo");
                }
            });
        };
        SGraphicsModule.Get().CreateWindow("Stroke Expansion", new Extent2D(500),
            WindowFlags.Focused | WindowFlags.Resizable | WindowFlags.Visible);
        
    }

    public void Stop(SApplication application)
    {
        
    }

    public static MainModule Get()
    {
        return SApplication.Get().GetModule<MainModule>();
    }
}