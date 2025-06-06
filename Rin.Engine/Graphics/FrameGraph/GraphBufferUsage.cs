namespace Rin.Engine.Graphics.FrameGraph;

public enum GraphBufferUsage
{
    Undefined,
    Host,
    HostThenTransfer,
    HostThenGraphics,
    HostThenCompute,
    HostThenIndirect,
    Transfer,
    Graphics,
    Compute,
    Indirect
}