namespace Rin.Framework.Graphics.Graph;

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