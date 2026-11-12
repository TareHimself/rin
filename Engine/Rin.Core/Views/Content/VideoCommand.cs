using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Shared.Buffers;
using Rin.Core.Shared.Video;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;
using Rin.Core.Views.Graphics.Shaders;

namespace Rin.Core.Views.Content;

internal class VideoCommand(in Matrix4x4 transform, in Vector2 size, Buffer<byte> frameData, in Extent2D extent)
    : TCommand<MainPassConfig, VideoCommandHandler>
{
    public readonly Buffer<byte> FrameData = frameData;
    public readonly VideoFramePlanes Planes = IVideoPlayer.PlanesOf(extent);
    // The container does not carry the colour matrix, so HD is taken as BT.709 and SD as BT.601.
    public readonly VideoColorimetry Colorimetry = extent.Height >= 720 ? VideoColorimetry.Bt709 : VideoColorimetry.Bt601;
    public Vector2 Size = size;
    public Matrix4x4 Transform = transform;
}
