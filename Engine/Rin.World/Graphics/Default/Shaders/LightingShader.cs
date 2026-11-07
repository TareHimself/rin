using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

[NoReorder]
public struct LightingInfo
{
    public GBufferHandles GBuffer;
    public Vector3 Eye;
    public BufferRef<LightGridInfo> Grid;
}

[Shader]
public partial class LightingShader : Shader
{
    public struct PushConstants
    {
        public BufferRef<LightingInfo> Data;
    }

    public struct VertexIn
    {
        [VertexId] public int VertexId;
    }

    public struct VertexOut
    {
        [Semantic("UV")] public Vector2 Uv;
        [Position] public Vector4 Position;
    }

    public struct FragmentIn
    {
        [Semantic("UV")] public Vector2 Uv;
    }

    [Push] protected PushConstants Push;
    protected static BindlessData Bindless;

    protected override BlendState BlendState => BlendState.Alpha;

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
        output.Uv = (corner + new Vector2(1f)) / 2f;
        output.Position = new Vector4(corner.X, corner.Y, 0f, 1f);
        return output;
    }

    [Fragment, Attachment(AttachmentFormat.RGBA16), Stencil]
    public Vector4 Fragment(FragmentIn input)
    {
        var data = Push.Data[0];
        var grid = data.Grid[0];
        var surface = Bindless.SampleGBuffer(data.GBuffer, input.Uv);

        var color = LightMath.Rgb2Lin(new Vector3(surface.Emissive));
        for (var i = 0; i < grid.DirectionalLightCount; i++)
            color += DisneyModel.Eval(surface, data.Eye, grid.DirectionalLights[i]);

        var depth = Vector4.Transform(new Vector4(surface.Location, 1f), grid.View).Z;
        var bin = grid.Bins[LightGrid.BinOf(depth, grid.Near, grid.BinScale)];
        var first = bin & 0xFFFFu;
        var last = bin >> 16;
        if (first <= last)
        {
            var tileX = (uint)(input.Uv.X * grid.Width) / LightGrid.TileSize;
            var tileY = (uint)(input.Uv.Y * grid.Height) / LightGrid.TileSize;
            var masks = (tileY * grid.TilesX + tileX) * grid.WordCount;
            for (var word = first / 32u; word <= last / 32u; word++)
            {
                var bits = grid.TileMasks[masks + word] & LightGrid.RangeMask(word, first, last);
                while (bits != 0u)
                {
                    color += DisneyModel.Eval(surface, data.Eye, grid.PointLights[word * 32u + FirstBitLow(bits)]);
                    bits &= bits - 1u;
                }
            }
        }

        return new Vector4(LightMath.Lin2Rgb(color), 1f);
    }
}
