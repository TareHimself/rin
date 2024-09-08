namespace aerox.Runtime;

public struct ModuleType
{
    public Type Module;
    public RuntimeModuleAttribute Attribute;
    public readonly HashSet<Type> Dependencies;
    public bool HasResolvedDependencies;

    public ModuleType(Type module, RuntimeModuleAttribute attribute)
    {
        Module = module;
        Attribute = attribute;
        HasResolvedDependencies = false;
        Dependencies = Attribute.Dependencies.ToHashSet();
    }

    public void ResolveAllDependencies(Dictionary<Type, ModuleType> types)
    {
        if (HasResolvedDependencies) return;

        foreach (var dep in Attribute.Dependencies) Dependencies.Add(dep);

        foreach (var dep in Dependencies.ToArray())
        {
            types[dep].ResolveAllDependencies(types);
            foreach (var nestedDep in types[dep].Dependencies) Dependencies.Add(nestedDep);
        }

        HasResolvedDependencies = true;
    }
}