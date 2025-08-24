namespace Rin.Framework.Graphics.Textures;

public class BindlessImage : IBindlessImage
{
    public BindlessImage()
    {
        DebugName = "";
    }

    public BindlessImage(in ImageHandle handle, IDeviceImage image, bool mipMapped,
        string debugName)
    {
        Handle = handle;
        Image = image;
        MipMapped = mipMapped;
        DebugName = debugName;
    }

    public BindlessImage(in ImageHandle handle, bool mipMapped, string debugName)
    {
        Handle = handle;
        MipMapped = mipMapped;
        DebugName = debugName;
    }

    public bool Uploading { get; set; } = false;

    public IDeviceImage? Image { get; set; }
    public ImageHandle Handle { get; set; }
    public bool MipMapped { get; set; }
    public string DebugName { get; set; }

    public bool Uploaded { get; set; } = false;
}