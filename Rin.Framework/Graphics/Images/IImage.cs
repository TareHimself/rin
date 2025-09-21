namespace Rin.Framework.Graphics.Images;

public interface IImage
{
    public Extent2D Extent { get; }
    public bool Mips { get; }
    public ImageFormat Format { get; }
    
    public ImageHandle Handle { get; }
}