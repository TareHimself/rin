using System.Reflection;
using Rin.Framework.Sources;

namespace Rin.Framework;

public class AssemblyContentResource(Assembly assembly,string alias,string? contentPath = null)
    : AssemblyResource(assembly,  alias,$"Content/{contentPath ?? alias}")
{
    public new static AssemblyContentResource New<TAssemblyType>(string alias,string? contentPath = null)
    {
        return new AssemblyContentResource(typeof(TAssemblyType).Assembly, alias,contentPath);
    }
}