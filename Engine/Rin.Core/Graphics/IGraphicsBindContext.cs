using Rin.Core.Graphics.Shaders;

namespace Rin.Core.Graphics;

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

    /// <summary>
    /// Draws <paramref name="drawCount" /> non-indexed draws whose <see cref="DrawIndirectCommand" /> records sit
    /// <paramref name="stride" /> bytes apart in <paramref name="commands" />. Needs <see cref="IDevice.SupportsIndirectRendering" />.
    /// </summary>
    public IGraphicsBindContext DrawIndirect(in DeviceBufferView commands, uint drawCount, uint stride,
        uint commandsOffset = 0);

    /// <summary>
    /// Like <see cref="DrawIndirect" />, with the draw count read from <paramref name="drawCount" /> on the GPU and clamped to <paramref name="maxDrawCount" />.
    /// </summary>
    public IGraphicsBindContext DrawIndirectCount(in DeviceBufferView commands, in DeviceBufferView drawCount,
        uint maxDrawCount, uint stride, uint commandsOffset = 0, uint drawCountOffset = 0);

    public IGraphicsBindContext DrawIndexedIndirect(in DeviceBufferView commands, uint drawCount, uint stride,
        uint commandsOffset = 0);

    public IGraphicsBindContext DrawIndexedIndirectCount(in DeviceBufferView commands, in DeviceBufferView drawCount,
        uint maxDrawCount, uint stride, uint commandsOffset = 0, uint drawCountOffset = 0);
}
