using System.Collections.Frozen;
using System.Reflection;

namespace aerox.Runtime;

public sealed class SRuntime : Disposable
{
    private static SRuntime? _instance;
    private static readonly object Padlock = new();

    private readonly List<RuntimeModule> _modules = [];
    private readonly Dictionary<Type, RuntimeModule> _modulesMap = new();

    private readonly DateTime _startTime;

    private bool _exitRequested;
    private double _lastDeltaSeconds;

    private DateTime _lastTickTime;

    public SRuntime()
    {
        _startTime = DateTime.UtcNow;
        _lastTickTime = DateTime.UtcNow;
    }

    public event Action<double> OnTick;

    public event Action<SRuntime> OnStartup;
    public event Action<SRuntime> OnShutdown;

    public static SRuntime Get()
    {
        return _instance ??= new SRuntime();
    }

    public RuntimeModuleAttribute? GetModuleAttribute(Type type)
    {
        return (RuntimeModuleAttribute?)Attribute.GetCustomAttribute(type, typeof(RuntimeModuleAttribute));
    }

    public RuntimeModuleAttribute? GetModuleAttribute<T>()
    {
        return GetModuleAttribute(typeof(T));
    }

    public static void LoadRequiredModules<T>(Dictionary<Type, ModuleType> loadedModules) where T : RuntimeModule
    {
        LoadRequiredModules(typeof(T), loadedModules);
    }

    /// <summary>
    ///     Ensures all the modules needed by <see cref="T" /> have their assemblies loaded
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static void LoadRequiredModules(Type type, Dictionary<Type, ModuleType> loadedModules)
    {
        HashSet<Type> checkedModules = new();

        Queue<Type> pendingModules = new();

        checkedModules.Add(type);

        var attrib = Get().GetModuleAttribute(type);

        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToHashSet();

        if (attrib == null) return;

        foreach (var attribDependency in attrib.Dependencies)
            if (!checkedModules.Contains(attribDependency))
                pendingModules.Enqueue(attribDependency);

        while (pendingModules.Count > 0)
        {
            var dependency = pendingModules.Dequeue();
            
            if(loadedModules.ContainsKey(dependency)) continue;
            
            if (!loadedAssemblies.Contains(dependency.Assembly))
            {
                Assembly.Load(dependency.Assembly.GetName());
                loadedAssemblies.Add(dependency.Assembly);
            }

            loadedModules.Add(dependency, new ModuleType(dependency, Get().GetModuleAttribute(dependency)!));
            checkedModules.Add(dependency);
            var dependencyAttrib = Get().GetModuleAttribute(dependency);
            if (dependencyAttrib == null) continue;
            foreach (var attribDependency in dependencyAttrib.Dependencies)
                if (!checkedModules.Contains(attribDependency))
                    pendingModules.Enqueue(attribDependency);
        }
    }

    public double GetTimeSinceCreation()
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
        InitializeModules();
        OnStartup?.Invoke(this);
    }

    private void Shutdown()
    {
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

            OnTick?.Invoke(_lastDeltaSeconds);

            _lastTickTime = tickStart;
        }

        Shutdown();
    }


    private void LoadModules()
    {
        Console.WriteLine("Searching assemblies for Engine subsystems");
        // Get all assemblies loaded in the current application domain
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();

        var modulesMap = new Dictionary<Type, ModuleType>();


        foreach (var assembly in assemblies)
        {
            Console.WriteLine("Searching Assembly {0}", assembly.FullName);

            foreach (var type in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(RuntimeModule))))
            {
                var attribute =
                    (RuntimeModuleAttribute?)Attribute.GetCustomAttribute(type, typeof(RuntimeModuleAttribute));
                if (attribute == null) continue;
                modulesMap.Add(type, new ModuleType(type, attribute));
            }
        }

        foreach (var mod in modulesMap.ToFrozenDictionary()) LoadRequiredModules(mod.Key, modulesMap);


        foreach (var mod in modulesMap) mod.Value.ResolveAllDependencies(modulesMap);

        var sortedModules = new List<ModuleType>();

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
                if (!sortedModules[i].Dependencies.Contains(mod.Key) &&
                    (!mod.Value.Attribute.isNativeModule || sortedModules[i].Attribute.isNativeModule)) continue;

                sortedModules.Insert(i, mod.Value);
                shorted = true;
                break;
            }

            if (!shorted) sortedModules.Add(mod.Value);
        }

        Console.WriteLine("Creation Order");

        // Print out the static fields of the found types
        foreach (var mod in sortedModules)
        {
            Console.WriteLine($"{mod.Module.FullName}:");
            var instance = (RuntimeModule?)Activator.CreateInstance(mod.Module);
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


    public T GetModule<T>() where T : RuntimeModule
    {
        return (T)_modulesMap[typeof(T)];
    }

    protected override void OnDispose(bool isManual)
    {
    }
}