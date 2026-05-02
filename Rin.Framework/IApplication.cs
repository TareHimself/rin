using Rin.Framework.Audio;
using Rin.Framework.Graphics;
using Rin.Framework.Shared;
using Rin.Framework.Views;

namespace Rin.Framework;

public interface IApplication : IDisposable
{
    public float TimeSeconds { get; }
    public float LastDeltaSeconds { get; }
    public Dispatcher MainDispatcher { get; }
    public Dispatcher RenderDispatcher { get; }
    public event Action? OnPreUpdate;

    /// <summary>
    ///     Called on the main thread every update
    /// </summary>
    public event Action<float>? OnUpdate;

    public event Action? OnPostUpdate;

    /// <summary>
    ///     Called after <see cref="OnPostUpdate" /> on the main thread to collect data for the render thread
    /// </summary>
    public event Action? OnCollect;

    public event Action? OnPreRender;

    /// <summary>
    ///     Called on the render thread if we collected valid data
    /// </summary>
    public event Action? OnRender;

    public event Action? OnPostRender;

    public IGraphicsModule CreateGraphicsModule();
    public IAudioModule CreateAudioModule();
    public IViewsModule CreateViewsModule();

    public void Run();

    public void RequestExit();

    public static IApplication Get()
    {
        return SFramework.Provider.Get<IApplication>();
    }
}