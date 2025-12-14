namespace Rin.Framework;

public class ModuleType
{
    public readonly HashSet<Type> Dependencies;
    public readonly Type Module;
    public readonly ModuleAttribute ModuleAttribute;
    public bool AlwaysLoad;
    public bool HasResolvedDependencies;

    public ModuleType(Type module, ModuleAttribute moduleAttribute)
    {
        Module = module;
        ModuleAttribute = moduleAttribute;
        HasResolvedDependencies = false;

        Dependencies = ModuleAttribute.Dependencies.ToHashSet();
        {
            AlwaysLoad = Attribute.GetCustomAttribute(module, typeof(AlwaysLoadAttribute)) != null;
        }
    }

    public void ResolveAllDependencies(Dictionary<Type, ModuleType> types)
    {
        if (HasResolvedDependencies) return;

        foreach (var dep in ModuleAttribute.Dependencies) Dependencies.Add(dep);

        foreach (var dep in Dependencies.ToArray())
        {
            types[dep].ResolveAllDependencies(types);
            foreach (var nestedDep in types[dep].Dependencies) Dependencies.Add(nestedDep);
        }

        HasResolvedDependencies = true;
    }
}