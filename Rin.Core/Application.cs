using System.Runtime.InteropServices;
using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Core.Shared;
using Rin.Core.Views;
using Rin.Core.Extensions;

namespace Rin.Core;

public abstract class Application : IApplication
{
    private readonly AutoResetEvent _mainUpdateEvent = new(false);
    private readonly AutoResetEvent _renderFinishedEvent = new(true);

    private readonly DateTime _startTime = DateTime.UtcNow;
    private IAudioModule? _audioModule;

    private bool _exitRequested;
    private IGraphicsModule? _graphicsModule;

    private DateTime _lastTickTime = DateTime.UtcNow;

    private List<IModule> _modules = [];

    private Thread? _renderTask;
    private IViewsModule? _viewsModule;

    public Application()
    {
        Global.Provider.AddSingle<IApplication>(this);
    }

    public event Action? OnPreUpdate;
    public event Action<float>? OnUpdate;
    public event Action? OnPostUpdate;
    public event Action? OnCollect;
    public event Action? OnPreRender;
    public event Action? OnRender;
    public event Action? OnPostRender;

    public float TimeSeconds => (float)(DateTime.UtcNow - _startTime).TotalSeconds;
    public float LastDeltaSeconds { get; private set; }

    public Dispatcher MainDispatcher { get; } = new();
    public Dispatcher RenderDispatcher { get; } = new();
    public abstract IGraphicsModule CreateGraphicsModule();

    public abstract IViewsModule CreateViewsModule();

    public abstract IAudioModule CreateAudioModule();

    public void Run()
    {
        Start();

        _renderTask = new Thread(Render) { IsBackground = true, Name = "Render Thread" };
        _renderTask.Start();
        _lastTickTime = DateTime.UtcNow;

        while (!_exitRequested)
        {
            Profiling.Measure("Engine.PreUpdate", OnPreUpdate);
            Profiling.Measure("Engine.DispatchPending", MainDispatcher.DispatchPending);

            Profiling.Begin("Engine.Update");
            var tickStart = DateTime.UtcNow;
            LastDeltaSeconds = (float)(tickStart - _lastTickTime).TotalSeconds;
            OnUpdate?.Invoke(LastDeltaSeconds);
            _lastTickTime = tickStart;
            Profiling.End("Engine.Update");

            Profiling.Measure("Engine.PostUpdate", OnPostUpdate);
            _renderFinishedEvent.WaitOne();
            Profiling.Measure("Engine.Collect", OnCollect);
            _mainUpdateEvent.Set();
        }

        _mainUpdateEvent.Set();
        _renderTask.Join();
    }

    public void RequestExit()
    {
        _exitRequested = true;
    }
    
    private class SelectPathCallbackContainer
    {
        public readonly List<string> Paths = [];
    }

    [UnmanagedCallersOnly]
    private static unsafe void PlatformSelectCallback(char* path, IntPtr context)
    {
        var handle = GCHandle.FromIntPtr(context);
        if (handle.Target is SelectPathCallbackContainer resultContainer)
        {
            resultContainer.Paths.Add(Marshal.PtrToStringUTF8((nint)path) ?? "");
        }
    }

    public string[] SelectFile(string title = "Select File\'s", bool multiple = false, string filter = "")
    {
        unsafe
        {
            var resultContainer = new SelectPathCallbackContainer();
            var handle = GCHandle.Alloc(resultContainer, GCHandleType.Normal);
            try
            {
                Native.platformSelectFile(title, multiple, filter,&PlatformSelectCallback,GCHandle.ToIntPtr(handle));
            }
            finally
            {
                handle.Free();
            }
            return resultContainer.Paths.ToArray();
        }
    }

    public string[] SelectPath(string title = "Select Path\'s", bool multiple = false)
    {
        unsafe
        {
            var resultContainer = new SelectPathCallbackContainer();
            var handle = GCHandle.Alloc(resultContainer, GCHandleType.Normal);
            try
            {
                Native.platformSelectPath(title, multiple,&PlatformSelectCallback,GCHandle.ToIntPtr(handle));
            }
            finally
            {
                handle.Free();
            }
            return resultContainer.Paths.ToArray();
        }
    }


    public void Dispose()
    {
        OnShutdown();
        foreach (var module in _modules.AsReversed()) module.Stop(this);
        ShutdownPlatform();
    }

    protected abstract void OnStartup();
    protected abstract void OnShutdown();

    protected virtual void InitializePlatform()
    {
        Native.platformInit();
    }

    protected virtual void ShutdownPlatform()
    {
        Native.platformShutdown();
    }

    private void Start()
    {
        InitializePlatform();

        _audioModule = Global.Provider.AddSingle(CreateAudioModule());
        _graphicsModule = Global.Provider.AddSingle(CreateGraphicsModule());
        _viewsModule = Global.Provider.AddSingle(CreateViewsModule());
        _modules = [_audioModule, _graphicsModule, _viewsModule];


        foreach (var module in _modules) module.Start(this);

        OnStartup();
    }

    private void Render()
    {
        while (!_exitRequested)
        {
            _mainUpdateEvent.WaitOne();
            if (_exitRequested) return;
            RenderDispatcher.DispatchPending();
            Profiling.Measure("Engine.PreRender", OnPreRender);
            Profiling.Measure("Engine.Rendering", OnRender);
            Profiling.Measure("Engine.PostRender", OnPostRender);
            _renderFinishedEvent.Set();
        }
    }
}