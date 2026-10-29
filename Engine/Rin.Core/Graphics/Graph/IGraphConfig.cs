namespace Rin.Core.Graphics.Graph;

public interface IGraphConfig
{
    /// <inheritdoc cref="IGraphBuilder.AddExternalImage" />
    public uint AddExternalImage(ResourceHandle handle, GraphResourceFlags flags = GraphResourceFlags.None,
        Action? onDispose = null);

    /// <inheritdoc cref="IGraphBuilder.AddExternalImage" />
    public uint AddExternalBuffer(in DeviceBufferView view, GraphResourceFlags flags = GraphResourceFlags.None,
        Action? onDispose = null);

    public uint CreateTexture(in Extent2D extent, ImageFormat format, ImageLayout layout,
        GraphResourceFlags flags = GraphResourceFlags.None);

    public uint CreateTextureArray(in Extent2D extent, ImageFormat format, uint count, ImageLayout layout,
        GraphResourceFlags flags = GraphResourceFlags.None);

    public uint CreateCubemap(in Extent2D extent, ImageFormat format, ImageLayout layout,
        GraphResourceFlags flags = GraphResourceFlags.None);

    public uint CreateBuffer(ulong size, GraphBufferUsage usage, GraphResourceFlags flags = GraphResourceFlags.None);

    public uint CreateBuffer<T>(GraphBufferUsage usage, GraphResourceFlags flags = GraphResourceFlags.None)
        where T : unmanaged
    {
        return CreateBuffer(Utils.ByteSizeOf<T>(), usage, flags);
    }

    public uint CreateBuffer<T>(int count, GraphBufferUsage usage, GraphResourceFlags flags = GraphResourceFlags.None)
        where T : unmanaged
    {
        return CreateBuffer(Utils.ByteSizeOf<T>(count), usage, flags);
    }

    public uint CreateBuffer<T>(uint count, GraphBufferUsage usage, GraphResourceFlags flags = GraphResourceFlags.None)
        where T : unmanaged
    {
        return CreateBuffer(Utils.ByteSizeOf<T>() * count, usage, flags);
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