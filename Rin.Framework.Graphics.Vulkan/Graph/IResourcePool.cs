using Rin.Framework.Graphics.Vulkan.Images;

namespace Rin.Framework.Graphics.Vulkan.Graph;

public interface IResourcePool : IDisposable
{
    public IDisposableVulkanTexture CreateTexture(TextureResourceDescriptor descriptor, Frame frame);
    public IDisposableVulkanTextureArray CreateTextureArray(TextureArrayResourceDescriptor descriptor, Frame frame);
    public IDisposableVulkanCubemap CreateCubemap(CubemapResourceDescriptor descriptor, Frame frame);
    
    public IVulkanDeviceBuffer CreateBuffer(BufferResourceDescriptor descriptor, Frame frame);

    public void OnFrameStart(ulong newFrame);
}