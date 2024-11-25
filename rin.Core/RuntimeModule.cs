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

public abstract class RuntimeModule : IRuntimeModule
{
    private SRuntime? _engine;

    public virtual void Startup(SRuntime runtime)
    {
        _engine = runtime;
        Console.WriteLine($"Starting up {this.GetType().Name}");
    }

    public SRuntime? GetRuntime()
    {
        return _engine;
    }

    public virtual void Shutdown(SRuntime runtime)
    {
    }
}