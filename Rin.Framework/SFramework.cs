using System.Reflection;
using JetBrains.Annotations;
using Provide;
using Rin.Sources;

namespace Rin.Framework;

public sealed class SFramework
{
    public static readonly string Directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? "") ?? "";
    
    [PublicAPI] public static SourceResolver Sources = new()
    {
        Sources =
        [
            new FileSystemSource(),
            AssemblyContentResource.New<SFramework>("Framework"),
        ]
    };
    public static IProvider Provider { get; } = new DefaultProvider();
}