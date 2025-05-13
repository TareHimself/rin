using Rin.Engine.Graphics.Descriptors;

namespace Rin.Engine.Graphics.Textures;

public interface IBindlessImageFactory : IDisposable
{
    public DescriptorSet GetDescriptorSet();

    public ImageHandle CreateTexture(in Extent3D size, ImageFormat format,
        ImageFilter filter = ImageFilter.Linear,
        ImageTiling tiling = ImageTiling.Repeat, bool mips = false, ImageUsage usage = ImageUsage.None,
        string? debugName = null);

    public (ImageHandle handle, Task task) CreateTexture(Buffer<byte> data, Extent3D size, ImageFormat format,
        ImageFilter filter = ImageFilter.Linear,
        ImageTiling tiling = ImageTiling.Repeat, bool mips = false, ImageUsage usage = ImageUsage.None,
        string? debugName = null);

    public Task? GetPendingTexture(in ImageHandle imageHandle);
    public bool IsTextureReady(in ImageHandle imageHandle);

    //public void WaitTextureReady(int textureId);

    // public Task<int> CreateTextures(NativeBuffer<byte> data, Extent3D size, ImageFormat format,
    //     ImageFilter filter = ImageFilter.Linear,
    //     ImageTiling tiling = ImageTiling.Repeat, bool mipMapped = false, string debugName = "Texture");

    public void FreeTextures(params ImageHandle[] textureIds);

    public ITexture? GetTexture(ImageHandle imageHandle);

    public IDeviceImage? GetTextureImage(ImageHandle imageHandle);

    public bool IsHandleValid(in ImageHandle imageHandle);

    public uint GetMaxTextures();

    public uint GetTexturesCount();
}