using Rin.Engine.Core;

namespace Rin.Engine.Graphics;

public struct TextureCreateInfo()
{
    public required Buffer<byte> Data;
    public required Extent3D Size;
    public required ImageFormat Format;
    public ImageFilter Filter = ImageFilter.Linear;
    public ImageTiling Tiling = ImageTiling.Repeat;
    public bool Mips = false;
    public string DebugName = "Texture";
}