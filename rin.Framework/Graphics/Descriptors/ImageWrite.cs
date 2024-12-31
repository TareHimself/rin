using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.Descriptors;

public struct ImageWrite
{
    public readonly IDeviceImage Image;
    public readonly VkImageLayout Layout;
    public readonly ImageType Type;
    public SamplerSpec Sampler;
    public uint Index = 0;

    public ImageWrite(IDeviceImage image, VkImageLayout layout, ImageType type, SamplerSpec? spec = null)
    {
        Image = image;
        Layout = layout;
        Type = type;
        Sampler = spec.GetValueOrDefault(new SamplerSpec()
        {
            Filter = ImageFilter.Linear,
            Tiling = ImageTiling.Repeat
        });
    }
    
    public ImageWrite(int textureId)
    {
        if (SGraphicsModule.Get().GetResourceManager().GetTexture(textureId) is { } boundTexture)
        {
            Image = boundTexture.Image!;
            Layout = VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
            Type = ImageType.Texture;
            Sampler = new SamplerSpec()
            {
                Filter = boundTexture.Filter,
                Tiling = boundTexture.Tiling
            };
        }
        else
        {
            throw new Exception($"Invalid Texture Id [{textureId}]");
        }
    }
}