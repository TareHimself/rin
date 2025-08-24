using Rin.Framework.Graphics.Textures;

namespace Rin.Framework.Graphics.Descriptors;

public struct ImageWrite
{
    public readonly IDeviceImage Image;
    public readonly ImageLayout Layout;
    public SamplerSpec? Sampler;
    public uint Index = 0;

    public ImageWrite(IDeviceImage image, ImageLayout layout, SamplerSpec? spec = null)
    {
        Image = image;
        Layout = layout;
        Sampler = spec;
    }

    public ImageWrite(in ImageHandle handle)
    {
        if (SGraphicsModule.Get().GetImageFactory().GetTexture(handle) is { } boundTexture)
        {
            Image = boundTexture.Image!;
            Layout = ImageLayout.ShaderReadOnly;
        }
        else
        {
            throw new Exception($"Invalid Texture Id [{handle}]");
        }
    }
}