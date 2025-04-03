namespace Rin.Engine.Graphics.Textures;

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

    public Texture(int id, ImageFilter filter, ImageTiling tiling, bool mipMapped, string debugName)
    {
        Id = id;
        Filter = filter;
        Tiling = tiling;
        MipMapped = mipMapped;
        DebugName = debugName;
    }

    public bool Uploading { get; set; } = false;

    public IDeviceImage? Image { get; set; }
    public int Id { get; set; }
    public ImageFilter Filter { get; set; }
    public ImageTiling Tiling { get; set; }
    public bool MipMapped { get; set; }
    public string DebugName { get; set; }

    public bool Uploaded { get; set; } = false;
}