using Rin.Framework.Graphics.Images;

namespace Rin.Framework.Graphics.Graph;

public interface IGraphConfig
{
    public uint SwapchainImageId { get; }

    public uint AddExternalTexture(ITexture texture, Action? onDispose = null);
    public uint AddExternalTextureArray(ITextureArray textureArray, Action? onDispose = null);
    public uint AddExternalCubemap(ICubemap cubemap, Action? onDispose = null);
    public uint CreateTexture(in Extent2D extent,ImageFormat format,ImageLayout layout);
    public uint CreateTextureArray(in Extent2D extent,ImageFormat format,int count,ImageLayout layout);
    public uint CreateCubemap(in Extent2D extent,ImageFormat format,ImageLayout layout);
    
    public uint CreateBuffer(ulong size, GraphBufferUsage usage);

    public uint CreateBuffer<T>(GraphBufferUsage usage) where T : unmanaged
    {
        return CreateBuffer(Utils.ByteSizeOf<T>(), usage);
    }

    public uint CreateBuffer<T>(int count, GraphBufferUsage usage) where T : unmanaged
    {
        return CreateBuffer(Utils.ByteSizeOf<T>(count), usage);
    }

    public uint CreateBuffer<T>(uint count, GraphBufferUsage usage) where T : unmanaged
    {
        return CreateBuffer(Utils.ByteSizeOf<T>() * count, usage);
    }

    public uint UseTexture(uint id, ImageLayout layout, ResourceOperation operation);
    public uint UseTextureArray(uint id, ImageLayout layout, ResourceOperation operation);
    public uint UseCubemap(uint id, ImageLayout layout, ResourceOperation operation);
    public uint UseBuffer(uint id, GraphBufferUsage usage, ResourceOperation operation);

    public uint ReadTexture(uint id, ImageLayout layout)
    {
        return UseTexture(id, layout, ResourceOperation.Read);
    }

    public uint WriteTexture(uint id, ImageLayout layout)
    {
        return UseTexture(id, layout, ResourceOperation.Write);
    }
    
    public uint ReadTextureArray(uint id, ImageLayout layout)
    {
        return UseTexture(id, layout, ResourceOperation.Read);
    }

    public uint WriteTextureArray(uint id, ImageLayout layout)
    {
        return UseTexture(id, layout, ResourceOperation.Write);
    }
    
    public uint ReadCubemap(uint id, ImageLayout layout)
    {
        return UseTexture(id, layout, ResourceOperation.Read);
    }

    public uint WriteCubemap(uint id, ImageLayout layout)
    {
        return UseTexture(id, layout, ResourceOperation.Write);
    }
    
    public uint ReadBuffer(uint id, GraphBufferUsage usage)
    {
        return UseBuffer(id, usage, ResourceOperation.Read);
    }

    public uint WriteBuffer(uint id, GraphBufferUsage usage)
    {
        return UseBuffer(id, usage, ResourceOperation.Write);
    }

    /// <summary>
    ///     Adds an explicit dependency to the specified pass ID (NOTE. this may not be what you need)
    /// </summary>
    /// <param name="passId"></param>
    /// <returns></returns>
    public uint DependOn(uint passId);
}