namespace rin.Core;

public class RuntimeModuleAttribute(params Type[] inDependencies) : Attribute
{
    public readonly Type[] Dependencies = inDependencies;
    public bool IsNativeModule;
}

public class NativeRuntimeModuleAttribute : RuntimeModuleAttribute
{
    public NativeRuntimeModuleAttribute(params Type[] inDependencies) : base(inDependencies)
    {
        IsNativeModule = true;
    }
}

public class RuntimeModule
{
    private SRuntime? _engine;

    public virtual async void Startup(SRuntime runtime)
    {
        _engine = runtime;
        Console.WriteLine($"Starting up {this.GetType().Name}");
    }

    protected SRuntime? GetEngine()
    {
        return _engine;
    }

    public virtual void Shutdown(SRuntime runtime)
    {
    }
}