using System.Numerics;

namespace Rin.Framework.Graphics;

public interface IExecutionContext
{
    public string Id { get; }

    /// <summary>
    ///     Try to find a named descriptor set, useful for global descriptor sets
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    public IExecutionContext BindIndexBuffer(in DeviceBufferView view);

    public IExecutionContext Barrier(IImage2D image, ImageLayout from, ImageLayout to);

    public IExecutionContext Barrier(in DeviceBufferView view, BufferUsage from, BufferUsage to,
        ResourceOperation fromOperation, ResourceOperation toOperation);

    public IExecutionContext CopyToBuffer(in DeviceBufferView src, in DeviceBufferView dest);

    public IExecutionContext CopyToImage(in DeviceBufferView src, IImage2D dest);

    public IExecutionContext CopyToImage(IImage2D src, in Offset2D srcOffset, in Extent2D srcSize,
        IImage2D dest, in Offset2D destOffset, in Extent2D destSize, ImageFilter filter = ImageFilter.Linear);
    
    // public IExecutionContext CopyToImage(IDeviceImage src, in Offset2D srcOffset,
    //     IDeviceImage dest,
    //     in Offset2D destOffset, ImageFilter filter = ImageFilter.Linear);
    
    public IExecutionContext CopyToImage(IImage2D src, IImage2D dest, ImageFilter filter = ImageFilter.Linear);

    public IExecutionContext EnableBackFaceCulling();
    public IExecutionContext EnableFrontFaceCulling();
    public IExecutionContext DisableFaceCulling();

    public IExecutionContext BeginRendering(in Extent2D extent,
        IEnumerable<IImage2D> attachments, IImage2D? depthAttachment = null,
        IImage2D? stencilAttachment = null, Vector4? clearColor = null);

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

    public IExecutionContext ClearColorImages(in Vector4 clearColor, ImageLayout layout,
        params IImage2D[] images);

    public IExecutionContext ClearStencilImages(uint clearValue, ImageLayout layout,
        params IImage2D[] images);

    public IExecutionContext ClearDepthImages(float clearValue, ImageLayout layout,
        params IImage2D[] images);
}