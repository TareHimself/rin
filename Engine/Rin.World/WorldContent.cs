using System.Runtime.CompilerServices;
using Rin.Core;

namespace Rin.World;

internal sealed class WorldContent
{
    [ModuleInitializer]
    internal static void Init()
    {
        Global.Sources.AddSource(AssemblyContentResource.New<WorldContent>("World"));
        Global.Sources.AddSource(AssemblyContentResource.New<WorldContent>("Shaders/World"));
        // Rin.Shade-authored shaders (Rin.Shade.MSBuild), a sibling of the Shaders/World alias above -
        // never written to disk, but embedded and resolved the same way.
        Global.Sources.AddSource(AssemblyContentResource.New<WorldContent>("Shaders/Rin/World"));
    }
}
