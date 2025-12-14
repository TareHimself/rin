using Rin.Framework.Graphics.Shaders;

namespace Rin.Framework.Graphics;

public interface IGraphicsBindContext : IBindContext<IGraphicsBindContext>
{
    public IGraphicsShader Shader { get; }

    public IGraphicsBindContext Draw(uint vertices, uint instances = 1, uint firstVertex = 0,
        uint firstInstance = 0);

    public IGraphicsBindContext DrawIndexed(uint indexCount,
        uint instanceCount = 1,
        uint firstIndex = 0,
        uint firstVertex = 0,
        uint firstInstance = 0);

    public IGraphicsBindContext DrawIndexedIndirect(in DeviceBufferView commands, uint drawCount, uint stride,
        uint commandsOffset = 0);

    public IGraphicsBindContext DrawIndexedIndirectCount(in DeviceBufferView commands, in DeviceBufferView drawCount,
        uint maxDrawCount, uint stride, uint commandsOffset = 0, uint drawCountOffset = 0);
}