using Rin.Core.Graphics;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     The CPU draw loop shared by the direct passes. It issues the draw that the indirect path would
///     have built into a <see cref="DrawIndexedIndirectCommand" />, so the shaders see the same inputs.
/// </summary>
internal static class DirectMeshDraw
{
    public static void DrawBatch(IGraphicsBindContext bindContext, ReadOnlySpan<ProcessedMesh> meshes,
        ReadOnlySpan<int> batch)
    {
        for (var instance = 0; instance < batch.Length; instance++)
        {
            var surface = meshes[batch[instance]].Surface;
            bindContext.DrawIndexed(surface.IndicesCount, 1, surface.IndicesStart, surface.VertexStart,
                (uint)instance);
        }
    }
}
