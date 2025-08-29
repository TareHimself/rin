namespace Rin.Framework.Graphics.Textures;

public interface IBindlessImage
{
    public IImage2D? Image { get; }

    public ImageHandle Handle { get; }

    public bool MipMapped { get; }
    public string DebugName { get; }
    public bool Uploaded { get; }
}