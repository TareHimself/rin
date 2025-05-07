using Rin.Engine.Graphics.Textures;

namespace Rin.Engine.Graphics.Descriptors;

public struct ImageWrite
{
    public readonly IDeviceImage Image;
    public readonly ImageLayout Layout;
    public readonly DescriptorImageType Type;
    public SamplerSpec Sampler;
    public uint Index = 0;

    public ImageWrite(IDeviceImage image, ImageLayout layout, DescriptorImageType type, SamplerSpec? spec = null)
    {
        Image = image;
        Layout = layout;
        Type = type;
        Sampler = spec.GetValueOrDefault(new SamplerSpec
        {
            Filter = ImageFilter.Linear,
            Tiling = ImageTiling.Repeat
        });
    }

    public ImageWrite(in TextureHandle handle)
    {
        if (SGraphicsModule.Get().GetTextureFactory().GetTexture(handle) is { } boundTexture)
        {
            Image = boundTexture.Image!;
            Layout = ImageLayout.ShaderReadOnly;
            Type = DescriptorImageType.Sampled;
            Sampler = new SamplerSpec
            {
                Filter = boundTexture.Filter,
                Tiling = boundTexture.Tiling
            };
        }
        else
        {
            throw new Exception($"Invalid Texture Id [{handle}]");
        }
    }
}