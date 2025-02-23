using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using rin.Framework.Audio;
using rin.Framework.Core;
using rin.Framework.Graphics;
using rin.Framework.Views;

namespace rin.Framework;

public abstract class App
{
    private DateTime _startTime = DateTime.UtcNow;
    private float _lastUpdateTime;
    private float _lastDelta;
    [PublicAPI]
    public bool PendingExit { get; private set; }
    
    [PublicAPI]
    public bool Active { get; private set; }
    public IFileSystem FileSystem { get; } = new DefaultFileSystem("");
    
    [PublicAPI]
    public abstract IGraphicsModule GraphicsModule { get; }
    
    [PublicAPI]
    public abstract IAudioModule AudioModule { get; }
    
    [PublicAPI]
    public abstract IViewsModule ViewsModule { get; }

    [PublicAPI] public string AssetsDirectory { get; protected set; } = string.Empty;

    [PublicAPI] public string FrameworkAssetsDirectory { get; protected set; } = string.Empty;

    protected virtual void Start()
    {
        AssetsDirectory = Path.Join(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? "") ?? "", "assets");
        FrameworkAssetsDirectory = Path.Join(AssetsDirectory, "rin");
        AudioModule.Start(this);
        GraphicsModule.Start(this);
        ViewsModule.Start(this);
    }

    protected virtual void Stop()
    {
        ViewsModule.Stop(this);
        GraphicsModule.Stop(this);
        AudioModule.Stop(this);
    }
    
    protected abstract void Update(float deltaSeconds);
    
    [PublicAPI]
    public float GetLastDeltaSeconds() => _lastDelta;
    
    [PublicAPI]
    public float GetTimeSeconds() => (float)DateTime.UtcNow.Subtract(_startTime).TotalSeconds;
    
    public virtual void RequestExit()
    {
        PendingExit = true;
    }

    public void Run()
    {
        _startTime = DateTime.UtcNow;
        Active = true;
        
        Start();
        while (!PendingExit)
        {
            var delta = GetTimeSeconds() - _lastUpdateTime;
            _lastDelta = delta;
            Update(delta);
            _lastUpdateTime = GetTimeSeconds();
        }
        Stop();
        Active = false;
    }
}