namespace rin.Framework.Core;

public class ModuleType
{
    public readonly Type Module;
    public readonly ModuleAttribute ModuleAttribute;
    public readonly HashSet<Type> Dependencies;
    public bool HasResolvedDependencies;
    public bool AlwaysLoad;

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