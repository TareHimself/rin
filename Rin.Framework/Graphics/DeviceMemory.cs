namespace Rin.Framework.Graphics;

/// <summary>
///     GPU Memory
/// </summary>
public abstract class DeviceMemory(Allocator allocator, IntPtr allocation) : IDisposable
{
    public readonly IntPtr Allocation = allocation;
    protected readonly Allocator Allocator = allocator;
    public abstract void Dispose();
}