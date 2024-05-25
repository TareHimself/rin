using System.Reflection;

namespace aerox.Runtime;

public sealed class Runtime : Disposable
{
    public static string SHADERS_DIR = @"C:\Users\Taree\Documents\Github\aerox\aerox.Runtime\shaders";
    private static Runtime _instance;
    private static readonly object Padlock = new();

    private readonly List<AssemblyName> _ensureLoadedAssemblies = new();
    private readonly List<RuntimeModule> _modules = [];
    private readonly Dictionary<Type, RuntimeModule> _modulesMap = new();

    private readonly DateTime _startTime;

    private bool _exitRequested;

    private DateTime _lastTickTime;
    private double _lastDeltaSeconds = 0.0;

    public Runtime()
    {
        _startTime = DateTime.UtcNow;
        _lastTickTime = DateTime.UtcNow;
    }

    public static Runtime Instance
    {
        get
        {
            lock (Padlock)
            {
                return _instance ??= new Runtime();
            }
        }
    }

    public event Action<double> OnTick;

    public event Action<Runtime> OnStartup;
    public event Action<Runtime> OnShutdown;

    public void EnsureLoad(AssemblyName assembly)
    {
        _ensureLoadedAssemblies.Add(assembly);
    }
    
    public RuntimeModuleAttribute? GetModuleAttribute(Type type) => (RuntimeModuleAttribute?)Attribute.GetCustomAttribute(type, typeof(RuntimeModuleAttribute));
    public RuntimeModuleAttribute? GetModuleAttribute<T>() => GetModuleAttribute(typeof(T));
    
    /// <summary>
    /// Ensures all the modules needed by <see cref="T"/> have their assemblies loaded
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static void EnsureDependencies<T>() where T : RuntimeModule
    {
        HashSet<Type> checkedModules = new();
        
        Queue<Type> pendingModules = new();
        
        checkedModules.Add(typeof(T));
        
        var attrib = Instance.GetModuleAttribute<T>();
        
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToHashSet();
        
        if(attrib == null) return;
        
        foreach (var attribDependency in attrib.Dependencies)
        {
            if (!checkedModules.Contains(attribDependency))
            {
                pendingModules.Enqueue(attribDependency);
            }
        }
        
        while (pendingModules.Count > 0)
        {
            var dep = pendingModules.Dequeue();
            
            if (!loadedAssemblies.Contains(dep.Assembly))
            {
                Assembly.Load(dep.Assembly.GetName());
                loadedAssemblies.Add(dep.Assembly);
            }

            checkedModules.Add(dep);
            var depAttrib = Instance.GetModuleAttribute(dep);
            if(depAttrib == null) continue;
            foreach (var attribDependency in depAttrib.Dependencies)
            {
                if (!checkedModules.Contains(attribDependency))
                {
                    pendingModules.Enqueue(attribDependency);
                }
            }
        }
    }
    
    
    public void EnsureLoad(params string[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            _ensureLoadedAssemblies.Add(new AssemblyName(assembly));
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
        for (var i = _modules.Count - 1; i >= 0; i--)
        {
            _modules[i].Shutdown(this);
        }
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
        var assemblyNames = assemblies.Select(a => a.GetName().Name);
        var filtered = _ensureLoadedAssemblies.Where(a => !assemblyNames.Contains(a.Name)).ToArray();
        foreach (var assemblyName in filtered) assemblies.Add(Assembly.Load(assemblyName));


        var modulesMap = new Dictionary<Type, ModuleType>();


        foreach (var assembly in assemblies)
        {
            Console.WriteLine("Searching Assembly {0}", assembly.FullName);
            
            foreach (var type in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(RuntimeModule))))
            {
                var attribute = (RuntimeModuleAttribute?)Attribute.GetCustomAttribute(type, typeof(RuntimeModuleAttribute));
                if(attribute == null) continue;
                modulesMap.Add(type,new ModuleType(type,attribute));
            }
        }

        foreach (var mod in modulesMap)
        {
            mod.Value.ResolveAllDependencies(modulesMap);
        }

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
                
                sortedModules.Insert(i,mod.Value);
                shorted = true;
                break;
            }
            
            if(!shorted) sortedModules.Add(mod.Value);
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