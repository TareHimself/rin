using Rin.Framework.Audio;
using Rin.Framework.Graphics;
using Rin.Framework.Views;

namespace Rin.Framework;

public interface IApplication : IDisposable
{
    
    public event Action? OnPreUpdate;
    public event Action<float>? OnUpdate;
    public event Action? OnPostUpdate;
    public event Action? OnCollect;
    public event Action? OnPreRender;
    public event Action? OnRender;
    public event Action? OnPostRender;
    public event Action<IApplication>? OnStartup;
    public event Action<IApplication>? OnShutdown;
    
    public float TimeSeconds { get; }
    public float LastDeltaSeconds { get; }
    public Dispatcher MainDispatcher { get; }
    public Dispatcher RenderDispatcher { get; }
    
    public IGraphicsModule CreateGraphicsModule();
    public IViewsModule CreateViewsModule();
    public IAudioModule  CreateAudioModule();
    
    public void Run();

    public void RequestExit();
    
    public static IApplication Get() => SFramework.Provider.Get<IApplication>(); 
}