using rin.Framework.Core;
using rin.Framework.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics;

public interface ITextureManager : IDisposable
{
    public DescriptorSet GetDescriptorSet();

    public Task<int> CreateTexture(NativeBuffer<byte> data, VkExtent3D size, ImageFormat format,
        ImageFilter filter = ImageFilter.Linear,
        ImageTiling tiling = ImageTiling.Repeat, bool mipMapped = false, string debugName = "Texture");

    public void FreeTextures(params int[] textureIds);
    
    public ITexture? GetTexture(int textureId);

    public IDeviceImage? GetTextureImage(int textureId);

    public bool IsTextureIdValid(int textureId);
}