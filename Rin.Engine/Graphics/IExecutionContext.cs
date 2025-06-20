using System.Numerics;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Graphics.Shaders;

namespace Rin.Engine.Graphics;

public interface IExecutionContext
{
    public string Id { get; }
    public DescriptorAllocator DescriptorAllocator { get; }

    public DescriptorSet AllocateDescriptorSet(IShader shader, uint set);
    public IExecutionContext BindDescriptorSets(IShader shader, uint offset, params DescriptorSet[] sets);

    public IExecutionContext BindIndexBuffer(in DeviceBufferView buffer);

    public IExecutionContext Draw(uint vertices, uint instances = 1, uint firstVertex = 0,
        uint firstInstance = 0);

    public IExecutionContext DrawIndexed(uint indexCount,
        uint instanceCount = 1,
        uint firstIndex = 0,
        uint firstVertex = 0,
        uint firstInstance = 0);

    public IExecutionContext Dispatch(uint x, uint y = 1, uint z = 1);

    public IExecutionContext Invoke(IComputeShader computeShader, uint x, uint y = 1, uint z = 1)
    {
        return Dispatch((uint)float.Ceiling(x / (float)computeShader.GroupSizeX),
            (uint)float.Ceiling(y / (float)computeShader.GroupSizeY),
            (uint)float.Ceiling(z / (float)computeShader.GroupSizeZ));
    }

    public IExecutionContext Barrier(IDeviceImage image, ImageLayout from, ImageLayout to);

    public IExecutionContext Barrier(in DeviceBufferView view, BufferUsage from, BufferUsage to,
        ResourceOperation fromOperation, ResourceOperation toOperation);

    public IExecutionContext CopyToBuffer(in DeviceBufferView dest, in DeviceBufferView src);
    
    public IExecutionContext CopyToImage(IDeviceImage dest, in DeviceBufferView src);

    public IExecutionContext CopyToImage(IDeviceImage dest, IDeviceImage src, ImageFilter filter = ImageFilter.Linear);

    public IExecutionContext DrawIndexedIndirect(in DeviceBufferView commands, uint drawCount, uint stride,
        uint commandsOffset = 0);

    public IExecutionContext DrawIndexedIndirectCount(in DeviceBufferView commands, in DeviceBufferView drawCount,
        uint maxDrawCount, uint stride, uint commandsOffset = 0, uint drawCountOffset = 0);

    public IExecutionContext EnableBackFaceCulling();
    public IExecutionContext EnableFrontFaceCulling();
    public IExecutionContext DisableFaceCulling();

    public IExecutionContext BeginRendering(in Extent2D extent,
        IEnumerable<IDeviceImage> attachments, IDeviceImage? depthAttachment = null,
        IDeviceImage? stencilAttachment = null);

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
        params IDeviceImage[] images);

    public IExecutionContext ClearStencilImages(uint clearValue, ImageLayout layout,
        params IDeviceImage[] images);

    public IExecutionContext ClearDepthImages(float clearValue, ImageLayout layout,
        params IDeviceImage[] images);
}