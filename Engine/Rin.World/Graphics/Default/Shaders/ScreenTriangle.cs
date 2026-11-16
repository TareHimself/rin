using System.Numerics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

[ShadeExport]
public static class ScreenTriangle
{
    public const uint VertexCount = 3;

    /// <summary>
    ///     The screen position of a vertex, where the visible screen spans -1 to 1 on both axes.
    /// </summary>
    public static Vector2 Corner(int vertexId)
    {
        return new Vector2(vertexId == 1 ? 3f : -1f, vertexId == 2 ? 3f : -1f);
    }
}
