namespace Rin.Engine.Graphics.Textures;

public record struct TextureHandle
{
    /// <summary>
    /// lower 8 bits are <see cref="ImageType"/>, higher 24 bits store the Id
    /// </summary>
    private int _data;
    
    public ImageType Type => (ImageType)(_data & 0xFF);
    public int Id => (_data >> 8) & 0xFFFFFF;
    
    public bool IsValid() => Id >= 0 && SGraphicsModule.Get().GetTextureFactory().IsHandleValid(this);
    
    public TextureHandle(ImageType type, int id)
    {
        _data = ((id & 0xFFFFFF) << 8) | ((int)type & 0xFF);
    }
    
    public static TextureHandle InvalidImage => new(ImageType.Image,-1);
    public static TextureHandle InvalidCube => new (ImageType.Cube,-1);
    public static TextureHandle InvalidVolume => new (ImageType.Volume,-1);
}