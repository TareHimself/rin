using System.Numerics;
using Rin.Shade;

namespace Rin.Core.Views.Graphics.Shaders;

/// <summary>
///     Writes the stencil mask for a batch of clip regions: one instance per clip, drawn as a quad, with the
///     fragments outside the clip's rounded rectangle discarded. No color attachment is written.
/// </summary>
[Shader("Shaders/Rin/Core/Views/stencil_batch.slang")]
public partial class StencilBatchShader : Shader
{
    public struct PushConstants
    {
        public Matrix4x4 Projection;
        public BufferRef<StencilClip> Clips;
    }

    public struct VertexIn
    {
        [Semantic("SV_InstanceID")] public int InstanceId;
        [Semantic("SV_VertexID")] public int VertexId;
    }

    public struct VertexOut
    {
        [Semantic("QUAD_INDEX")] public int QuadIndex;
        [Semantic("SV_Position")] public Vector4 Position;
    }

    public struct FragmentIn
    {
        [Semantic("QUAD_INDEX")] public int QuadIndex;
        [Semantic("SV_Position")] public Vector2 Coordinate;
    }

    [Push] protected PushConstants Push;

    [Vertex]
    public VertexOut Vertex(VertexIn input)
    {
        var corners = new[]
        {
            new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(1f, 1f),
            new Vector2(-1f, -1f), new Vector2(1f, 1f), new Vector2(-1f, 1f)
        };

        var corner = corners[input.VertexId];

        VertexOut output;
        output.QuadIndex = input.InstanceId;
        output.Position = new Vector4(corner.X, corner.Y, 0f, 1f);
        return output;
    }

    [Fragment, Stencil]
    public void Fragment(FragmentIn input)
    {
        var clip = Push.Clips[input.QuadIndex];
        var result = ViewShaderMath.ApplyBorderRadius(input.Coordinate, new Vector4(0f, 0f, 0f, 1f),
            new Vector4(0f, 0f, 0f, 0f), clip.Size, clip.InverseTransform, 1f);

        if (result.W > 0.01f) Discard();
    }
}
