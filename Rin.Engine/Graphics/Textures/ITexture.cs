namespace Rin.Engine.Graphics.Textures;

public interface ITexture
{
    public IDeviceImage? Image { get; }

    public ImageHandle Handle { get; }
    public ImageFilter Filter { get; }
    public ImageTiling Tiling { get; }
    public bool MipMapped { get; }
    public string DebugName { get; }
    public bool Uploaded { get; }
}