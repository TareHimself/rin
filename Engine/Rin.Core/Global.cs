using JetBrains.Annotations;
using Rin.Core.Shared.Providers;
using Rin.Core.Sources;

namespace Rin.Core;

public sealed class Global
{
    public static readonly string Directory = AppContext.BaseDirectory;

    [PublicAPI] public static SourceResolver Sources = new()
    {
        Sources =
        [
            new FileSystemSource(),
            AssemblyContentResource.New<Global>("Core")
        ]
    };

    public static IProvider Provider { get; } = new DefaultProvider();
}