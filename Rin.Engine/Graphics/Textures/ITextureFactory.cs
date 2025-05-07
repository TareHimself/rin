using Rin.Engine.Graphics.Descriptors;

namespace Rin.Engine.Graphics.Textures;

public interface ITextureFactory : IDisposable
{
    public DescriptorSet GetDescriptorSet();

    public Pair<TextureHandle, Task> CreateTexture(Buffer<byte> data, Extent3D size, ImageFormat format,
        ImageFilter filter = ImageFilter.Linear,
        ImageTiling tiling = ImageTiling.Repeat, bool mips = false, string debugName = "Texture");

    public Task? GetPendingTexture(in TextureHandle textureHandle);
    public bool IsTextureReady(in TextureHandle textureHandle);

    //public void WaitTextureReady(int textureId);

    // public Task<int> CreateTextures(NativeBuffer<byte> data, Extent3D size, ImageFormat format,
    //     ImageFilter filter = ImageFilter.Linear,
    //     ImageTiling tiling = ImageTiling.Repeat, bool mipMapped = false, string debugName = "Texture");

    public void FreeTextures(params TextureHandle[] textureIds);

    public ITexture? GetTexture(TextureHandle textureHandle);

    public IDeviceImage? GetTextureImage(TextureHandle textureHandle);

    public bool IsHandleValid(in TextureHandle textureHandle);

    public uint GetMaxTextures();

    public uint GetTexturesCount();
}