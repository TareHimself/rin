namespace Rin.Engine.Graphics;

public class Texture : ITexture
{
    public Texture()
    {
        DebugName = "";
    }

    public Texture(int id, IDeviceImage image, ImageFilter filter, ImageTiling tiling, bool mipMapped, string debugName)
    {
        Id = id;
        Image = image;
        Filter = filter;
        Tiling = tiling;
        MipMapped = mipMapped;
        DebugName = debugName;
    }

    public IDeviceImage? Image { get; set; }
    public int Id { get; set; }
    public ImageFilter Filter { get; set; }
    public ImageTiling Tiling { get; set; }
    public bool MipMapped { get; set; }
    public string DebugName { get; set; }

    public bool Valid => Image != null;
}