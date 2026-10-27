namespace Rin.Core.Graphics;

public interface IDevice
{
    public bool SupportsIndirectRendering { get; }

    /// <summary>
    ///     Whether compute work can be scheduled in parallel with graphics, rather than sharing its timeline,
    ///     because the device exposes a queue family dedicated to compute.
    /// </summary>
    public bool SupportsDedicatedCompute { get; }
}
