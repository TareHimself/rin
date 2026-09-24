namespace Rin.Core.Graphics;

[Flags]
public enum ResourceUsageFlags
{
    Host,
    Transfer,
    Vertex,
    Fragment,
    Compute,
    Indirect,
}