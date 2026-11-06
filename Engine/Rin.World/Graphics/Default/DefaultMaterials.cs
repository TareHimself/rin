using Rin.World.Graphics.Surfaces;

namespace Rin.World.Graphics.Default;

public static class DefaultMaterials
{
    private static SurfaceMaterial<PbrMaterial>? _pbr;

    /// <summary>
    ///     White <see cref="PbrMaterial" />, drawn on surfaces that have no material.
    /// </summary>
    public static SurfaceMaterial<PbrMaterial> Pbr => _pbr ??= new SurfaceMaterial<PbrMaterial>(new PbrMaterial());
}
