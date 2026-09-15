using System.Runtime.CompilerServices;
using Rin.Core;
using Rin.World.Graphics.Mesh;

namespace Rin.World;

internal sealed class WorldContent
{
    [ModuleInitializer]
    internal static void Init()
    {
        Global.Sources.AddSource(AssemblyContentResource.New<WorldContent>("World"));
        Global.Sources.AddSource(AssemblyContentResource.New<WorldContent>("Shaders/World"));
        Global.Provider.AddSingle<IMeshFactory>(new MeshFactory());
    }
}
