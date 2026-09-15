namespace Rin.Core.Graphics.Graph;

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