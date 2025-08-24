namespace Rin.Framework.Graphics.FrameGraph;

public class GraphConfigImage
{
    public required Extent3D Extent { get; set; }
    public required ImageUsage Usage { get; set; }
    public required ImageFormat Format { get; set; }
}