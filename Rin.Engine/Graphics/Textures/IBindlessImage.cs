namespace Rin.Engine.Graphics.Textures;

public interface IBindlessImage
{
    public IDeviceImage? Image { get; }

    public ImageHandle Handle { get; }
    
    public bool MipMapped { get; }
    public string DebugName { get; }
    public bool Uploaded { get; }
}