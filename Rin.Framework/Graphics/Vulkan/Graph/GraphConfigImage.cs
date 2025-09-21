namespace Rin.Framework.Graphics.Vulkan.Graph;

public class GraphConfigImage
{
    public required Extent2D Extent { get; set; }
    public required ImageUsage Usage { get; set; }
    public required ImageFormat Format { get; set; }
    
    public required ImageType Type { get; set; }
    
    public uint Count { get; set; }
}