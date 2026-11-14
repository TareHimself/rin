using Rin.Core.Graphics.Graph;
using Rin.Graphics.Vulkan.Graph;

namespace Rin.Graphics.Vulkan;

public class ExecutionGroup
{
    public List<IPass> Passes { get; } = [];
    public bool IsBarrier => Passes.Count > 0 && Passes[0] is BarrierPass;
}