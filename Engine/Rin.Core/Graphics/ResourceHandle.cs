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

/// <summary>
///     Compact 32-bit resource reference safe to hand to shaders / GPU-visible data (mirrors
///     <c>ImageHandle</c> in Shaders/Core/images.slang bit-for-bit). Carries only what a shader needs to
///     look a resource up - its type and its slot id - and nothing CPU-only (bindless bookkeeping,
///     generation).
/// </summary>
public readonly record struct DeviceHandle
{
    private readonly uint _data;

    private const uint TypeMask = 0x7F;
    private const int IdShift = 8;
    private const uint IdMask = 0xFFFFFF;

    public DeviceHandle(ResourceType type, uint id)
    {
        _data = ((id & IdMask) << IdShift) | ((uint)type & TypeMask);
    }

    public DeviceHandle(uint data)
    {
        _data = data;
    }

    public ResourceType Type => (ResourceType)(_data & TypeMask);
    public uint Id => (_data >> IdShift) & IdMask;

    public static explicit operator uint(DeviceHandle handle)
    {
        return handle._data;
    }

    public static explicit operator DeviceHandle(uint data)
    {
        return new DeviceHandle(data);
    }
}

/// <summary>
///     CPU-side resource reference. Never sent to the GPU directly - <see cref="Device" /> is the
///     GPU-visible subset of this. <see cref="Generation" /> lets a caller (or a background upload/create
///     task) detect that the slot this handle pointed to was freed - and possibly reused by an unrelated
///     resource - since the handle was obtained.
/// </summary>
public readonly record struct ResourceHandle
{
    public ResourceHandle(ResourceType type, uint id, bool isBindless = false, uint generation = 0)
    {
        Device = new DeviceHandle(type, id);
        IsBindless = isBindless;
        Generation = generation;
    }

    public ResourceHandle(DeviceHandle device, bool isBindless = false, uint generation = 0)
    {
        Device = device;
        IsBindless = isBindless;
        Generation = generation;
    }

    public DeviceHandle Device { get; }
    public bool IsBindless { get; init; }
    public uint Generation { get; init; }

    public ResourceType Type => Device.Type;
    public uint Id => Device.Id;

    public static ResourceHandle InvalidTexture => new(ResourceType.Texture, 0);
    public static ResourceHandle InvalidCubemap => new(ResourceType.Cubemap, 0);
    public static ResourceHandle InvalidTextureArray => new(ResourceType.TextureArray, 0);
    public static ResourceHandle InvalidBuffer => new(ResourceType.Buffer, 0);

    public bool IsValid()
    {
        return Id != 0 && IGraphicsModule.Get().IsValidResourceHandle(this);
    }

    // One-way and intentionally so: this drops Generation/IsBindless, which is fine going into a
    // GPU-visible field (DeviceHandle is exactly that projection) but would be wrong the other way -
    // reconstructing a ResourceHandle from a DeviceHandle can't recover a real Generation, so that
    // conversion doesn't exist.
    public static implicit operator DeviceHandle(ResourceHandle handle)
    {
        return handle.Device;
    }
}
