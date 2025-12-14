namespace Rin.Framework.Graphics.Graph;

public class ResourceAllocationException(uint id) : FrameGraphException($"Allocation failed for resource with id {id}")
{
}