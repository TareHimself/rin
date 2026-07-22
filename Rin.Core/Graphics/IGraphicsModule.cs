// using Rin.Core.Graphics.Meshes;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Buffers;

namespace Rin.Core.Graphics;

public interface  IGraphicsModule : IModule, IUpdatable
{
    public event Action<IWindow>? OnWindowClosed;
    public event Action<IWindow>? OnWindowCreated;
    public event Action<IWindowRenderer>? OnWindowRendererCreated;
    public event Action<IWindowRenderer>? OnWindowRendererDestroyed;


    public void AddRenderer(IRenderer renderer);
    public void RemoveRenderer(IRenderer renderer);
    public IWindowRenderer? GetWindowRenderer(IWindow window);
    public IRenderer[] GetRenderers();
    public IWindowRenderer[] GetWindowRenderers();
    public IGraphicsShader MakeGraphics(string path);
    public IComputeShader MakeCompute(string path);

    public IWindow CreateWindow(string name, in Extent2D extent, WindowFlags flags = WindowFlags.Visible,
        IWindow? parent = null);

    public void WaitIdle();

    public DeviceBufferView NewTransferBuffer(ulong size, bool sequentialWrite = true,
        string debugName = "Transfer Buffer");

    public DeviceBufferView NewStorageBuffer<T>(bool sequentialWrite = true)
        where T : unmanaged
    {
        return NewStorageBuffer(Utils.ByteSizeOf<T>(), sequentialWrite);
    }

    public DeviceBufferView NewStorageBuffer(ulong size, bool sequentialWrite = true);
    public DeviceBufferView NewUniformBuffer(ulong size, bool sequentialWrite = true);

    public ResourceHandle CreateTexture(in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None);

    public ResourceHandle CreateTextureArray(in Extent2D extent, ImageFormat format, uint count,
        bool mips = false, ImageUsage usage = ImageUsage.None);

    public ResourceHandle CreateCubemap(in Extent2D extent, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None);

    public Task<ResourceHandle> CreateTexture(out ResourceHandle handle, ReadOnlySpan<byte> data, in Extent2D extent,
        ImageFormat format, bool mips = false, ImageUsage usage = ImageUsage.None);

    public Task<ResourceHandle> CreateTextureArray(out ResourceHandle handle, ReadOnlySpan<byte> data,
        in Extent2D extent,
        ImageFormat format, uint count, bool mips = false, ImageUsage usage = ImageUsage.None);

    public Task<ResourceHandle> CreateCubemap(out ResourceHandle handle, ReadOnlySpan<byte> data, in Extent2D extent,
        ImageFormat format, bool mips = false, ImageUsage usage = ImageUsage.None);

    public bool IsValidResourceHandle(in ResourceHandle handle);
    public Extent2D GetExtent(in ResourceHandle handle);
    public ImageFormat GetFormat(in ResourceHandle handle);
    public void FreeResourceHandles(params ReadOnlySpan<ResourceHandle> handles);

    public void WriteBuffer(in ResourceHandle handle, ReadOnlySpan<byte> data, ulong offset = 0);
    public ulong GetBufferAddress(in ResourceHandle handle);

    
    // public async Task AsyncCreateVertexBuffer<TVertexFormat>(ReadOnlySpan<TVertexFormat> vertices, ReadOnlySpan<uint> indices) where TVertexFormat : unmanaged
    // {
    //     using (vertices)
    //     using (indices)
    //     {
    //         if (_disposed) return;
    //
    //         var verticesByteSize = vertices.GetByteSize();
    //         var indicesByteSize = indices.GetByteSize();
    //         var vertexBuffer = SGraphicsModule.Get().GetAllocator().NewBuffer(verticesByteSize,
    //             VkBufferUsageFlags.VK_BUFFER_USAGE_STORAGE_BUFFER_BIT |
    //             VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT |
    //             VkBufferUsageFlags.VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT,
    //             VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT, debugName: "Vertex Buffer");
    //
    //         var indexBuffer = SGraphicsModule.Get().GetAllocator().NewBuffer(indicesByteSize,
    //             VkBufferUsageFlags.VK_BUFFER_USAGE_INDEX_BUFFER_BIT |
    //             VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT,
    //             VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT, debugName: "Vertex Index Buffer");
    //
    //         using var stagingBuffer = SGraphicsModule.Get().NewTransferBuffer(verticesByteSize + indicesByteSize);
    //             
    //         var stagingView = stagingBuffer.GetView();
    //         stagingView.Write(vertices);
    //         stagingView.Write(indices, verticesByteSize);
    //
    //         await SGraphicsModule.Get().TransferSubmit(cmd =>
    //         {
    //             cmd
    //                 .CopyToBuffer(stagingBuffer.GetView(0, verticesByteSize), vertexBuffer.GetView())
    //                 .CopyToBuffer(stagingBuffer.GetView(verticesByteSize, indicesByteSize), indexBuffer.GetView());
    //         });
    //
    //         var mesh = new DeviceMesh(vertexBuffer, indexBuffer, surfaces, Utils.ByteSizeOf<TVertexFormat>());
    //
    //         TaskCompletionSource? toComplete;
    //         lock (_sync)
    //         {
    //             _meshes[id] = mesh;
    //             _pendingMeshes.TryGetValue(id, out toComplete);
    //             _pendingMeshes.Remove(id);
    //         }
    //
    //         toComplete?.SetResult();
    //     }
    // }
    //
    // public async Task AsyncCreateVertexBuffer<TVertexFormat>(ReadOnlySpan<TVertexFormat> vertices, ReadOnlySpan<uint> indices) where TVertexFormat : unmanaged
    // {
    //     using (vertices)
    //     using (indices)
    //     {
    //         if (_disposed) return;
    //
    //         var verticesByteSize = vertices.GetByteSize();
    //         var indicesByteSize = indices.GetByteSize();
    //         var vertexBuffer = SGraphicsModule.Get().GetAllocator().NewBuffer(verticesByteSize,
    //             VkBufferUsageFlags.VK_BUFFER_USAGE_STORAGE_BUFFER_BIT |
    //             VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT |
    //             VkBufferUsageFlags.VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT,
    //             VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT, debugName: "Vertex Buffer");
    //
    //         var indexBuffer = SGraphicsModule.Get().GetAllocator().NewBuffer(indicesByteSize,
    //             VkBufferUsageFlags.VK_BUFFER_USAGE_INDEX_BUFFER_BIT |
    //             VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT,
    //             VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT, debugName: "Vertex Index Buffer");
    //
    //         using var stagingBuffer = SGraphicsModule.Get().NewTransferBuffer(verticesByteSize + indicesByteSize);
    //             
    //         var stagingView = stagingBuffer.GetView();
    //         stagingView.Write(vertices);
    //         stagingView.Write(indices, verticesByteSize);
    //
    //         await SGraphicsModule.Get().TransferSubmit(cmd =>
    //         {
    //             cmd
    //                 .CopyToBuffer(stagingBuffer.GetView(0, verticesByteSize), vertexBuffer.GetView())
    //                 .CopyToBuffer(stagingBuffer.GetView(verticesByteSize, indicesByteSize), indexBuffer.GetView());
    //         });
    //
    //         var mesh = new DeviceMesh(vertexBuffer, indexBuffer, surfaces, Utils.ByteSizeOf<TVertexFormat>());
    //
    //         TaskCompletionSource? toComplete;
    //         lock (_sync)
    //         {
    //             _meshes[id] = mesh;
    //             _pendingMeshes.TryGetValue(id, out toComplete);
    //             _pendingMeshes.Remove(id);
    //         }
    //
    //         toComplete?.SetResult();
    //     }
    // }
    
    public void Collect();
    public void Execute();

    public static IGraphicsModule Get()
    {
        return Global.Provider.Get<IGraphicsModule>();
    }
}