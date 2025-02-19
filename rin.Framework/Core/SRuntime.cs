using System.Collections.Frozen;
using System.Reflection;
using rin.Framework.Core.Extensions;

namespace rin.Framework.Core;

public sealed class SRuntime : Disposable
{
    public static readonly string
        AssetsDirectory = Path.Join(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? "") ?? "", "assets");

    public static readonly string
        FrameworkAssetsDirectory = Path.Join(AssetsDirectory, "rin");

    private static SRuntime? _instance;

    private readonly List<IModule> _modules = [];
    private readonly Dictionary<Type, IModule> _modulesMap = new();

    private readonly DateTime _startTime = DateTime.UtcNow;

    private bool _exitRequested;
    private double _lastDeltaSeconds;

    private DateTime _lastTickTime = DateTime.UtcNow;

    public string CachePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rin");

    public IFileSystem FileSystem { get; set; } = new DefaultFileSystem();

    public bool IsRunning { get; private set; }

    public event Action<double>? OnUpdate;

    public event Action<SRuntime>? OnStartup;
    public event Action<SRuntime>? OnShutdown;

    public static SRuntime Get()
    {
        return _instance ??= new SRuntime();
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

    /// <summary>
    ///     Returns the time elapsed since the runtime started
    /// </summary>
    /// <returns></returns>
    public double GetTimeSeconds()
    {
        return (DateTime.UtcNow - _startTime).TotalSeconds;
    }

    public double GetLastDeltaSeconds()
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
        for (var i = _modules.Count - 1; i >= 0; i--) _modules[i].Shutdown(this);
        Dispose();
    }


    public void Run()
    {
        Startup();

        _lastTickTime = DateTime.UtcNow;

        while (!_exitRequested)
        {
            var tickStart = DateTime.UtcNow;

            _lastDeltaSeconds = (tickStart - _lastTickTime).TotalSeconds;

            OnUpdate?.Invoke(_lastDeltaSeconds);

            _lastTickTime = tickStart;
        }

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
        foreach (var module in _modules) module.Startup(this);
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