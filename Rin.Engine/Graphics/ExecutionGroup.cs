using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.Graphics;

public class ExecutionGroup
{
    public required List<IPass> Passes { get; init; }
    public bool IsBarrier => Passes.FirstOrDefault() is BarrierPass;
}