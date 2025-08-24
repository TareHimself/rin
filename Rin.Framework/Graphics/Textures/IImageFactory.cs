using Rin.Framework.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics.Textures;

public interface IImageFactory : IDisposable
{
    public void Bind(in VkCommandBuffer cmd);
    public DescriptorSet GetDescriptorSet();
    public VkPipelineLayout GetPipelineLayout();

    public (ImageHandle handle, IDeviceImage image) CreateTexture(in Extent3D size, ImageFormat format,
        bool mips = false, ImageUsage usage = ImageUsage.None,
        string? debugName = null);

    public (ImageHandle handle, Task task) CreateTexture(Buffer<byte> data, Extent3D size, ImageFormat format,
        bool mips = false, ImageUsage usage = ImageUsage.None,
        string? debugName = null);

    public Task? GetPendingTexture(in ImageHandle imageHandle);
    public bool IsTextureReady(in ImageHandle imageHandle);

    //public void WaitTextureReady(int textureId);

    // public Task<int> CreateTextures(NativeBuffer<byte> data, Extent3D size, ImageFormat format,
    //     ImageFilter filter = ImageFilter.Linear,
    //     ImageTiling tiling = ImageTiling.Repeat, bool mipMapped = false, string debugName = "Texture");

    public void FreeHandles(params ImageHandle[] textureIds);

    public IBindlessImage? GetTexture(ImageHandle imageHandle);

    public IDeviceImage? GetTextureImage(ImageHandle imageHandle);

    public bool IsHandleValid(in ImageHandle imageHandle);

    public uint GetMaxTextures();

    public uint GetTexturesCount();
}