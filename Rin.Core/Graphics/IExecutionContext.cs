using System.Numerics;

namespace Rin.Core.Graphics;

public interface IExecutionContext
{
    public string Id { get; }

    public IExecutionContext BindIndexBuffer(in DeviceBufferView view);

    public IExecutionContext Barrier(ResourceHandle image, ImageLayout from, ImageLayout to);

    public IExecutionContext Barrier(ReadOnlySpan<TextureBarrier> barriers);

    public IExecutionContext Barrier(in DeviceBufferView view, BufferUsage from, BufferUsage to,
        ResourceOperation fromOperation, ResourceOperation toOperation);

    public IExecutionContext Barrier(ReadOnlySpan<BufferBarrier> barriers);

    public IExecutionContext CopyToBuffer(in DeviceBufferView src, in DeviceBufferView dest);

    public IExecutionContext CopyToImage(in DeviceBufferView src, ResourceHandle dest);

    public IExecutionContext CopyToImage(ResourceHandle src, in Offset2D srcOffset, in Extent2D srcSize,
        ResourceHandle dest, in Offset2D destOffset, in Extent2D destSize, ImageFilter filter = ImageFilter.Linear);

    public IExecutionContext CopyToImage(ResourceHandle src, ResourceHandle dest, ImageFilter filter = ImageFilter.Linear);

    public IExecutionContext EnableBackFaceCulling();
    public IExecutionContext EnableFrontFaceCulling();
    public IExecutionContext DisableFaceCulling();

    public IExecutionContext BeginRendering(in Extent2D extent,
        ReadOnlySpan<ResourceHandle> attachments, ResourceHandle depthAttachment = default,
        ResourceHandle stencilAttachment = default, Vector4? clearColor = null);

    public IExecutionContext EndRendering();

    public IExecutionContext EnableDepthTest();
    public IExecutionContext DisableDepthTest();

    public IExecutionContext EnableDepthWrite();
    public IExecutionContext DisableDepthWrite();
    public IExecutionContext StencilWriteOnly();
    public IExecutionContext StencilCompareOnly();
    public IExecutionContext SetStencilWriteMask(uint mask);
    public IExecutionContext SetStencilWriteValue(uint value);
    public IExecutionContext SetStencilCompareMask(uint mask);

    //public IExecutionContext

    public IExecutionContext ClearColorImages(in Vector4 clearColor,
        ReadOnlySpan<ResourceHandle> images);

    public IExecutionContext ClearStencilImages(uint clearValue,
        ReadOnlySpan<ResourceHandle> images);

    public IExecutionContext ClearDepthImages(float clearValue,
        ReadOnlySpan<ResourceHandle> images);

    public IExecutionContext WriteBuffer(in ResourceHandle handle, ReadOnlySpan<byte> data, ulong offset = 0);
}