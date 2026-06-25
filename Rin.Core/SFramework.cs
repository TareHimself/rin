using JetBrains.Annotations;
using Rin.Core.Shared.Providers;
using Rin.Core.Sources;

namespace Rin.Core;

public sealed class SFramework
{
    public static readonly string Directory = AppContext.BaseDirectory;

    [PublicAPI] public static SourceResolver Sources = new()
    {
        Sources =
        [
            new FileSystemSource(),
            AssemblyContentResource.New<SFramework>("Framework")
        ]
    };

    public static IProvider Provider { get; } = new DefaultProvider();
}