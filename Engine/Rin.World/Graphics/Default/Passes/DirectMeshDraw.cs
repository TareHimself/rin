using Rin.Core.Graphics;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     The CPU draw loop shared by the direct passes. It issues the draw that the indirect path would
///     have built into a <see cref="DrawIndexedIndirectCommand" />, so the shaders see the same inputs.
/// </summary>
internal static class DirectMeshDraw
{
    public static void DrawGroup(IGraphicsBindContext bindContext, List<ProcessedMesh> group)
    {
        for (var instance = 0; instance < group.Count; instance++)
        {
            var surface = group[instance].Surface;
            bindContext.DrawIndexed(surface.IndicesCount, 1, surface.IndicesStart, surface.VertexStart,
                (uint)instance);
        }
    }
}
