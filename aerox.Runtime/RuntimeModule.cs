namespace aerox.Runtime;

public class RuntimeModuleAttribute(params Type[] inDependencies) : Attribute
{
    public readonly Type[] Dependencies = inDependencies;
    public bool isNativeModule;
}

public class NativeRuntimeModuleAttribute : RuntimeModuleAttribute
{
    public NativeRuntimeModuleAttribute(params Type[] inDependencies) : base(inDependencies)
    {
        isNativeModule = true;
    }
}

public class RuntimeModule
{
    private Runtime? _engine;

    public virtual async void Startup(Runtime runtime)
    {
        _engine = runtime;
    }

    protected Runtime? GetEngine()
    {
        return _engine;
    }

    public virtual void Shutdown(Runtime runtime)
    {
    }
    
    
}