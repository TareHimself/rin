using Rin.Core.Graphics;

namespace Rin.Graphics.Null;

public sealed class NullDevice : IDevice
{
    public bool SupportsIndirectRendering { get; init; } = true;
    public bool SupportsDedicatedCompute { get; init; } = true;
}
