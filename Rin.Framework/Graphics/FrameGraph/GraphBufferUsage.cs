namespace Rin.Framework.Graphics.FrameGraph;

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