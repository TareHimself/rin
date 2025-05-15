using Rin.Engine.Graphics.Textures;

namespace Rin.Engine.Graphics.Descriptors;

public struct ImageWrite
{
    public readonly IDeviceImage Image;
    public readonly ImageLayout Layout;
    public readonly DescriptorImageType Type;
    public SamplerSpec? Sampler;
    public uint Index = 0;

    public ImageWrite(IDeviceImage image, ImageLayout layout, DescriptorImageType type, SamplerSpec? spec = null)
    {
        Image = image;
        Layout = layout;
        Type = type;
        Sampler = spec;
    }

    public ImageWrite(in ImageHandle handle)
    {
        if (SGraphicsModule.Get().GetImageFactory().GetTexture(handle) is { } boundTexture)
        {
            Image = boundTexture.Image!;
            Layout = ImageLayout.ShaderReadOnly;
            Type = DescriptorImageType.Sampled;
        }
        else
        {
            throw new Exception($"Invalid Texture Id [{handle}]");
        }
    }
}