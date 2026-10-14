using System.Runtime.CompilerServices;
using Rin.Core;

namespace Rin.World;

internal sealed class WorldContent
{
    [ModuleInitializer]
    internal static void Init()
    {
        Global.Sources.AddSource(AssemblyContentResource.New<WorldContent>("World"));
    }
}
