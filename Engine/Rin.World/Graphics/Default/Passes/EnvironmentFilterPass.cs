using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Fills an environment map's cubemap from its panorama, every mip of every face, then frees the panorama. Added
///     to a frame only while the map is not filtered yet, so it runs once per map.
/// </summary>
public partial class EnvironmentFilterPass(DeviceEnvironmentMap map) : IPass
{
    private const int FaceCount = 6;

    private uint _panoramaId;
    private uint _cubemapId;

    [GraphicsShader<EnvironmentFilterShader>]
    private partial IGraphicsShader Shader { get; }

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        _panoramaId = config.AddExternalImage(map.Panorama);
        _cubemapId = config.AddExternalImage(map.Cubemap);
        if (_panoramaId == 0 || _cubemapId == 0) return;

        config.ReadTexture(_panoramaId, ImageLayout.ShaderReadOnly);
        config.WriteCubemap(_cubemapId, ImageLayout.ColorAttachment);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (_panoramaId == 0 || _cubemapId == 0 || map.IsFiltered) return;

        var graphics = IGraphicsModule.Get();
        var panoramaExtent = graphics.GetExtent(map.Panorama);
        var faceExtent = new Extent2D(map.FaceSize, map.FaceSize);
        for (var mip = 0u; mip < map.MipCount; mip++)
        {
            var mipSize = faceExtent.MipExtent(mip).Width;
            var push = new EnvironmentFilterShader.PushConstants
            {
                Panorama = map.Panorama,
                Roughness = EnvironmentMap.MipRoughness((int)mip, (int)map.MipCount),
                // A face spans a quarter of the panorama's width.
                SharpestLevel = float.Max(float.Log2(panoramaExtent.Width / (4f * mipSize)), 0f),
                PanoramaSize = new Vector2(panoramaExtent.Width, panoramaExtent.Height)
            };

            for (var face = 0; face < FaceCount; face++)
            {
                ctx.BeginRendering(map.Cubemap, ImageRegion.OfFace(faceExtent, (CubeFace)face, mip))
                    .DisableFaceCulling();
                // The shader may still be compiling, in which case the map stays unfiltered and a later frame retries.
                if (Shader.Bind(ctx) is not { } bindContext)
                {
                    ctx.EndRendering();
                    return;
                }

                push.Face = face;
                bindContext.Push(push).Draw(ScreenTriangle.VertexCount);
                ctx.EndRendering();
            }
        }

        map.MarkFiltered();
        graphics.FreeResourceHandles(map.Panorama);
    }
}
