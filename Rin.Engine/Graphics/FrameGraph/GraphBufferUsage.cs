namespace Rin.Engine.Graphics.FrameGraph;

public enum GraphBufferUsage
{
    Undefined,
    HostThenTransfer,
    HostThenGraphics,
    HostThenCompute,
    HostThenIndirect,
    Transfer,
    Graphics,
    Compute,
    Indirect
}