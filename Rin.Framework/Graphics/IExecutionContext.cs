using System.Numerics;
using Rin.Framework.Graphics.Images;

namespace Rin.Framework.Graphics;

public interface IExecutionContext
{
    public string Id { get; }
    
    public IExecutionContext BindIndexBuffer(in DeviceBufferView view);

    public IExecutionContext Barrier(ITexture image, ImageLayout from, ImageLayout to);

    public IExecutionContext Barrier(in DeviceBufferView view, BufferUsage from, BufferUsage to,
        ResourceOperation fromOperation, ResourceOperation toOperation);

    public IExecutionContext CopyToBuffer(in DeviceBufferView src, in DeviceBufferView dest);

    public IExecutionContext CopyToImage(in DeviceBufferView src, ITexture dest);

    public IExecutionContext CopyToImage(ITexture src, in Offset2D srcOffset, in Extent2D srcSize,
        ITexture dest, in Offset2D destOffset, in Extent2D destSize, ImageFilter filter = ImageFilter.Linear);
    
    // public IExecutionContext CopyToImage(IDeviceImage src, in Offset2D srcOffset,
    //     IDeviceImage dest,
    //     in Offset2D destOffset, ImageFilter filter = ImageFilter.Linear);
    
    public IExecutionContext CopyToImage(ITexture src, ITexture dest, ImageFilter filter = ImageFilter.Linear);

    public IExecutionContext EnableBackFaceCulling();
    public IExecutionContext EnableFrontFaceCulling();
    public IExecutionContext DisableFaceCulling();

    public IExecutionContext BeginRendering(in Extent2D extent,
        IEnumerable<ITexture> attachments, ITexture? depthAttachment = null,
        ITexture? stencilAttachment = null, Vector4? clearColor = null);

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
        params ITexture[] images);

    public IExecutionContext ClearStencilImages(uint clearValue,
        params ITexture[] images);

    public IExecutionContext ClearDepthImages(float clearValue,
        params ITexture[] images);
}