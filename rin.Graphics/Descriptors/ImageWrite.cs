using TerraFX.Interop.Vulkan;

namespace rin.Graphics.Descriptors;

public class ImageWrite
{
    public readonly DeviceImage Image;
    public readonly VkImageLayout Layout;
    public readonly ImageType Type;
    public SamplerSpec Sampler;

    public ImageWrite(DeviceImage image, VkImageLayout layout, ImageType type, SamplerSpec spec)
    {
        Image = image;
        Layout = layout;
        Type = type;
        Sampler = spec;
    }
    
    public ImageWrite(int textureId)
    {
        if (SGraphicsModule.Get().GetResourceManager().GetTextureInfo(textureId) is { } boundTexture)
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