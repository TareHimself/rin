using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Graph;

public class GraphConfigBuffer
{
    public required ulong Size { get; set; }
    public required BufferCreateFlags Usage { get; set; }
}
