namespace Rin.Core.Graphics.Graph;

public enum GraphBufferUsage
{
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