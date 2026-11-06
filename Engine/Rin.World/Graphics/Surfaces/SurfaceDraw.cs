using JetBrains.Annotations;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics.Surfaces;

/// <summary>
///     One draw's data as <see cref="SurfaceShader{TMaterial}" /> reads it: the mesh instance and the material struct.
/// </summary>
[NoReorder]
[ShadeExport]
public struct SurfaceDraw<TMaterial> where TMaterial : unmanaged
{
    public MeshInstance Instance;
    public TMaterial Material;
}
