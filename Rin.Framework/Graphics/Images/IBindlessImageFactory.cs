using Rin.Framework.Graphics.Vulkan.Descriptors;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics.Images;

public interface IBindlessImageFactory : IDisposable
{
    public ImageHandle CreateTexture(in Extent2D size, ImageFormat format,
        bool mips = false, ImageUsage usage = ImageUsage.None);
    public ImageHandle CreateTextureArray(in Extent2D size, ImageFormat format,
        uint count,
        bool mips = false, ImageUsage usage = ImageUsage.None);
    public ImageHandle CreateCubemap(in Extent2D size, ImageFormat format,
        bool mips = false, ImageUsage usage = ImageUsage.None);
    
    public Task CreateTexture(out ImageHandle handle,IReadOnlyBuffer<byte> data,in Extent2D size, ImageFormat format,
        bool mips = false, ImageUsage usage = ImageUsage.None);
    public Task CreateTextureArray(out ImageHandle handle, IReadOnlyBuffer<byte> data, in Extent2D size,
        ImageFormat format,
        uint count,
        bool mips = false, ImageUsage usage = ImageUsage.None);
    public Task CreateCubemap(out ImageHandle handle,IReadOnlyBuffer<byte> data,in Extent2D size, ImageFormat format,
        bool mips = false, ImageUsage usage = ImageUsage.None);
    
    public ITexture? GetTexture(in ImageHandle handle);
    public ITextureArray? GetTextureArray(in ImageHandle handle);
    public ICubemap? GetCubemap(in ImageHandle handle);
    
    public void FreeHandles(params ImageHandle[] handles);

    public bool IsValid(in ImageHandle handle);

    public uint GetMaxTextures();
    public uint GetMaxTextureArrays();
    public uint GetMaxCubemaps();
    public uint GetTextureCount();
    public uint GetTextureArrayCount();
    public uint GetCubemapCount();
}