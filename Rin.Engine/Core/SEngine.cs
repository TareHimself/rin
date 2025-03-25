using System.Collections.Frozen;
using System.Reflection;
using JetBrains.Annotations;
using Rin.Engine.Core.Extensions;
using Rin.Sources;

namespace Rin.Engine.Core;

public sealed class SEngine : Disposable
{
    public static readonly string
        Directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? "") ?? "";

    public static readonly string
        AssetsDirectory = Path.Join(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? "") ?? "", "assets");

    public static readonly string
        FrameworkAssetsDirectory = Path.Join(AssetsDirectory, "rin");

    
    public TempSource Temp { get; private set; } = new TempSource();
    
    [PublicAPI] public SourceResolver Sources = new SourceResolver()
    {
        Sources =
        [
            new FileSystemSource(),
            new ResourcesSource(typeof(SEngine).Assembly, "Engine", ".Content.Engine.")
        ]
    };

    private static SEngine? _instance;
    private readonly List<IModule> _modules = [];
    private readonly Dictionary<Type, IModule> _modulesMap = new();

    private readonly DateTime _startTime = DateTime.UtcNow;

    private bool _exitRequested;
    private float _lastDeltaSeconds;

    private DateTime _lastTickTime = DateTime.UtcNow;

    private Task? _renderTask;

    public string CachePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rin");

    private readonly Dispatcher _mainDispatcher = new Dispatcher();
    private readonly Dispatcher _renderDispatcher = new Dispatcher();
    private readonly AutoResetEvent _mainUpdateEvent = new(false);
    public bool IsRunning { get; private set; }
    public event Action? OnPreUpdate;
    public event Action<float>? OnUpdate;
    public event Action? OnPostUpdate;

    public event Action? OnPreRender;
    public event Action? OnRender;
    public event Action? OnPostRender;

    public event Action<SEngine>? OnStartup;
    public event Action<SEngine>? OnShutdown;

    public static SEngine Get()
    {
        return _instance ??= new SEngine();
    }

    private static ModuleAttribute? GetModuleAttribute(Type type)
    {
        return (ModuleAttribute?)Attribute.GetCustomAttribute(type, typeof(ModuleAttribute));
    }

    public static ModuleAttribute? GetModuleAttribute<T>()
    {
        return GetModuleAttribute(typeof(T));
    }

    public static void LoadRequiredModules<T>(Dictionary<Type, ModuleType> loadedModules) where T : IModule
    {
        LoadRequiredModules(typeof(T), loadedModules);
    }

    /// <summary>
    ///     Ensures all the modules needed by <see cref="T" /> have their assemblies loaded
    /// </summary>
    private static void LoadRequiredModules(Type type, Dictionary<Type, ModuleType> loadedModules)
    {
        HashSet<Type> checkedModules = new();

        Queue<Type> pendingModules = new();

        checkedModules.Add(type);

        var attrib = GetModuleAttribute(type);

        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToHashSet();

        if (attrib == null) return;

        foreach (var attribDependency in attrib.Dependencies)
            if (!checkedModules.Contains(attribDependency))
                pendingModules.Enqueue(attribDependency);

        while (pendingModules.Count > 0)
        {
            var dependency = pendingModules.Dequeue();

            if (loadedModules.ContainsKey(dependency)) continue;

            if (!loadedAssemblies.Contains(dependency.Assembly))
            {
                Assembly.Load(dependency.Assembly.GetName());
                loadedAssemblies.Add(dependency.Assembly);
            }

            loadedModules.Add(dependency, new ModuleType(dependency, GetModuleAttribute(dependency)!));
            checkedModules.Add(dependency);
            var dependencyAttrib = GetModuleAttribute(dependency);
            if (dependencyAttrib == null) continue;
            foreach (var attribDependency in dependencyAttrib.Dependencies)
                if (!checkedModules.Contains(attribDependency))
                    pendingModules.Enqueue(attribDependency);
        }
    }

    public SEngine()
    {
        Sources.AddSource(Temp);
    }

    public void DispatchMain(Action action)
    {
        _mainDispatcher.Enqueue(action);
    }
    
    public void DispatchRender(Action action)
    {
        _renderDispatcher.Enqueue(action);
    }
    
    public Dispatcher GetMainDispatcher()
    {
       return _mainDispatcher;
    }
    
    public Dispatcher GetRenderDispatcher()
    {
        return _renderDispatcher;
    }

    /// <summary>
    ///     Returns the time elapsed since the runtime started
    /// </summary>
    /// <returns></returns>
    public float GetTimeSeconds()
    {
        return (float)(DateTime.UtcNow - _startTime).TotalSeconds;
    }

    public float GetLastDeltaSeconds()
    {
        return _lastDeltaSeconds;
    }

    public void RequestExit()
    {
        _exitRequested = true;
    }

    private void Startup()
    {
        Platform.Init();
        LoadModules();
        IsRunning = true;
        InitializeModules();
        OnStartup?.Invoke(this);
    }

    private void Shutdown()
    {
        IsRunning = false;
        OnShutdown?.Invoke(this);
        for (var i = _modules.Count - 1; i >= 0; i--) _modules[i].Stop(this);
        Dispose();
    }


    private void Render()
    {
        while (!_exitRequested)
        {
            _mainUpdateEvent.WaitOne();
            if (_exitRequested) return;
            _renderDispatcher.DispatchPending();
            OnPreRender?.Invoke();
            OnRender?.Invoke();
            OnPostRender?.Invoke();
        }
    }

    public void Run()
    {
        Startup();
        
        _renderTask = Task.Factory.StartNew(Render, TaskCreationOptions.LongRunning);
        _lastTickTime = DateTime.UtcNow;

        while (!_exitRequested)
        {
            
            OnPreUpdate?.Invoke();
            var tickStart = DateTime.UtcNow;
            _mainDispatcher.DispatchPending();
            _lastDeltaSeconds = (float)(tickStart - _lastTickTime).TotalSeconds;
            OnUpdate?.Invoke(_lastDeltaSeconds);
            OnPostUpdate?.Invoke();
            _lastTickTime = tickStart;

            _mainUpdateEvent.Set();
        }

        _renderTask.Wait();
        Shutdown();
    }


    private void LoadModules()
    {
        var interfaceType = typeof(IModule);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        var modulesMap = new Dictionary<Type, ModuleType>();
        foreach (var assembly in assemblies)
        foreach (var type in assembly.GetTypes().Where(t => interfaceType.IsAssignableFrom(t)))
        {
            var attribute =
                (ModuleAttribute?)Attribute.GetCustomAttribute(type, typeof(ModuleAttribute));
            if (attribute == null) continue;
            modulesMap.Add(type, new ModuleType(type, attribute));
        }

        foreach (var mod in modulesMap.ToFrozenDictionary()) LoadRequiredModules(mod.Key, modulesMap);

        foreach (var mod in modulesMap) mod.Value.ResolveAllDependencies(modulesMap);

        var sortedModules = new List<ModuleType>();


        // One day I wrote this, One day I will understand what it does
        foreach (var mod in modulesMap)
        {
            if (sortedModules.Count == 0)
            {
                sortedModules.Add(mod.Value);
                continue;
            }

            var shorted = false;
            for (var i = 0; i < sortedModules.Count; i++)
            {
                if (!sortedModules[i].Dependencies.Contains(mod.Key)) continue;

                sortedModules.Insert(i, mod.Value);
                shorted = true;
                break;
            }

            if (!shorted) sortedModules.Add(mod.Value);
        }

        var dependencies = new HashSet<Type>();
        foreach (var sortedModule in sortedModules.AsReversed())
            if (sortedModule.AlwaysLoad || dependencies.Contains(sortedModule.Module))
            {
                dependencies.Add(sortedModule.Module);
                foreach (var sortedModuleDependency in sortedModule.Dependencies)
                    dependencies.Add(sortedModuleDependency);
            }

        foreach (var mod in sortedModules.Where(c => dependencies.Contains(c.Module)))
        {
            var instance = (IModule?)Activator.CreateInstance(mod.Module);
            if (instance != null)
            {
                _modules.Add(instance);
                _modulesMap.Add(mod.Module, instance);
            }
        }
    }

    private void InitializeModules()
    {
        foreach (var module in _modules) module.Start(this);
    }


    public T GetModule<T>() where T : IModule
    {
        return (T)_modulesMap[typeof(T)];
    }

    public bool IsModuleLoaded<T>() where T : IModule
    {
        return _modulesMap.ContainsKey(typeof(T));
    }

    protected override void OnDispose(bool isManual)
    {
    }
}