namespace Rin.Core.Graphics;

public enum ResourceType : sbyte
{
    /// <summary>
    ///     2D Texture
    /// </summary>
    Texture,

    /// <summary>
    ///     Cube map
    /// </summary>
    Cubemap,

    /// <summary>
    ///     Array of 2D textures
    /// </summary>
    TextureArray,

    /// <summary>
    ///     GPU buffer
    /// </summary>
    Buffer
}

public readonly record struct ResourceHandle
{
    /// <summary>
    ///     lower 7 bits are <see cref="ResourceType" />, bit 7 is <see cref="IsBindless" />, higher 24 bits store the Id
    /// </summary>
    private readonly uint _data;

    private const uint TypeMask = 0x7F;
    private const uint BindlessBit = 0x80;
    private const int IdShift = 8;
    private const uint IdMask = 0xFFFFFF;

    public ResourceHandle(ResourceType type, uint id, bool isBindless = false)
    {
        _data = ((id & IdMask) << IdShift) | ((uint)type & TypeMask) | (isBindless ? BindlessBit : 0);
    }

    public ResourceHandle(uint data)
    {
        _data = data;
    }

    public ResourceType Type => (ResourceType)(_data & TypeMask);
    public bool IsBindless => (_data & BindlessBit) != 0;
    public uint Id => (_data >> IdShift) & IdMask;

    public static ResourceHandle InvalidTexture => new(ResourceType.Texture, 0);
    public static ResourceHandle InvalidCubemap => new(ResourceType.Cubemap, 0);
    public static ResourceHandle InvalidTextureArray => new(ResourceType.TextureArray, 0);
    public static ResourceHandle InvalidBuffer => new(ResourceType.Buffer, 0);

    public bool IsValid()
    {
        return IGraphicsModule.Get().IsValidResourceHandle(this);
    }

    public static explicit operator uint(ResourceHandle handle)
    {
        return handle._data;
    }

    public static explicit operator ResourceHandle(uint data)
    {
        return new ResourceHandle(data);
    }
}
