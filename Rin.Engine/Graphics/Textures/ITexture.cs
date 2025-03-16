namespace Rin.Engine.Graphics;

public interface ITexture
{
    public IDeviceImage? Image { get; }

    public int Id { get; }
    public ImageFilter Filter { get; }
    public ImageTiling Tiling { get; }
    public bool MipMapped { get; }
    public string DebugName { get; }
    public bool Uploaded { get; }
}