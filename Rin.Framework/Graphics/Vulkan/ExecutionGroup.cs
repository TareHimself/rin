using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Vulkan.Graph;

namespace Rin.Framework.Graphics.Vulkan;

public class ExecutionGroup
{
    public required List<IPass> Passes { get; init; }
    public bool IsBarrier => Passes.FirstOrDefault() is BarrierPass;
}