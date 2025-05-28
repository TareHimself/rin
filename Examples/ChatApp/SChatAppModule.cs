using ChatApp.Views;
using Rin.Engine;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views;

namespace ChatApp;

[Module(typeof(SViewsModule))]
[AlwaysLoad]
public class SChatAppModule : IModule, ISingletonGetter<SChatAppModule>
{
    private IWindow? _window;

    public void Start(SEngine engine)
    {
        _window = SGraphicsModule.Get().CreateWindow(500, 500, "Chat");
        _window.OnClose += _ => { _window.Dispose(); };
        _window.GetViewSurface()?.Add(new MainView());
    }

    public void Stop(SEngine engine)
    {
    }

    public static SChatAppModule Get()
    {
        return SEngine.Get().GetModule<SChatAppModule>();
    }
}