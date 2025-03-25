using Rin.Engine.Core;
using Rin.Engine.Graphics.Descriptors;

namespace Rin.Engine.Graphics.Textures;

public interface ITextureFactory : IDisposable
{
    public DescriptorSet GetDescriptorSet();

    public Pair<int,Task> CreateTexture(Buffer<byte> data, Extent3D size, ImageFormat format,
        ImageFilter filter = ImageFilter.Linear,
        ImageTiling tiling = ImageTiling.Repeat, bool mips = false, string debugName = "Texture");
    
    public Task? GetPendingTexture(int textureId);
    public bool IsTextureReady(int textureId);
    
    //public void WaitTextureReady(int textureId);

    // public Task<int> CreateTextures(NativeBuffer<byte> data, Extent3D size, ImageFormat format,
    //     ImageFilter filter = ImageFilter.Linear,
    //     ImageTiling tiling = ImageTiling.Repeat, bool mipMapped = false, string debugName = "Texture");

    public void FreeTextures(params int[] textureIds);

    public ITexture? GetTexture(int textureId);

    public IDeviceImage? GetTextureImage(int textureId);

    public bool IsTextureIdValid(int textureId);

    public uint GetMaxTextures();

    public uint GetTexturesCount();
}