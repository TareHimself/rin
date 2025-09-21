namespace Rin.Framework.Graphics.Images;

public readonly record struct ImageHandle
{
    /// <summary>
    ///     lower 8 bits are <see cref="ImageType" />, higher 24 bits store the Id
    /// </summary>
    private readonly int _data;

    public ImageHandle(ImageType type, int id)
    {
        _data = ((id & 0xFFFFFF) << 8) | ((int)type & 0xFF);
    }

    public ImageHandle(int data)
    {
        _data = data;
    }

    public ImageType Type => (ImageType)(_data & 0xFF);
    public int Id => (_data >> 8) & 0xFFFFFF;

    public static ImageHandle InvalidTexture => new(ImageType.Texture, 0);
    public static ImageHandle InvalidCubemap => new(ImageType.Cubemap, 0);
    public static ImageHandle InvalidTextureArray => new(ImageType.TextureArray, 0);

    public bool IsValid()
    {
        return IGraphicsModule.Get().IsValidImageHandle(this);
    }

    public static explicit operator int(ImageHandle handle)
    {
        return handle._data;
    }

    public static explicit operator ImageHandle(int data)
    {
        return new ImageHandle(data);
    }
}