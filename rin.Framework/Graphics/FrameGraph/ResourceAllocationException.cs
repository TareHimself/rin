namespace rin.Framework.Graphics.FrameGraph;

public class ResourceAllocationException(uint id) : FrameGraphException($"Allocation failed for resource with id {id}")
{
    
}