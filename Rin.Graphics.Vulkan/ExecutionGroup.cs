using Rin.Core.Graphics.Graph;
using Rin.Graphics.Vulkan.Graph;
using Rin.Graphics.Vulkan.Graph;

namespace Rin.Graphics.Vulkan;

public class ExecutionGroup
{
    public required List<IPass> Passes { get; init; }
    public bool IsBarrier => Passes.FirstOrDefault() is BarrierPass;
}