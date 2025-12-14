namespace Rin.Framework;

public class ModuleAttribute(params Type[] inDependencies) : Attribute
{
    public readonly Type[] Dependencies = inDependencies;
}