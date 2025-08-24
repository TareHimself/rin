using Rin.Framework.Graphics.FrameGraph;

namespace Rin.Framework.Graphics;

public class ExecutionGroup
{
    public required List<IPass> Passes { get; init; }
    public bool IsBarrier => Passes.FirstOrDefault() is BarrierPass;
}