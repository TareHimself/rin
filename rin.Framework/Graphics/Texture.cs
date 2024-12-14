namespace rin.Framework.Graphics;

public class Texture : ITexture
{
    public IDeviceImage? Image {get; set;}
    public int Id { get; set; }
    public ImageFilter Filter {get; set;}
    public ImageTiling Tiling {get; set;}
    public bool MipMapped { get; set; } = false;
    public string DebugName { get; set; }
    
    public bool Valid => Image != null;

    public Texture()
    {
        DebugName = "";
    }

    public Texture(int id,IDeviceImage image, ImageFilter filter, ImageTiling tiling, bool mipMapped, string debugName)
    {
        Id = id;
        Image = image;
        Filter = filter;
        Tiling = tiling;
        MipMapped = mipMapped;
        DebugName = debugName;
    }
}