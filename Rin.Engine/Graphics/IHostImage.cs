using Rin.Engine.Graphics.Textures;

namespace Rin.Engine.Graphics;

public interface IHostImage : IDisposable
{
    public Extent2D Extent { get; }
    public uint Channels { get; }

    public ImageFormat Format { get; }

    public (ImageHandle handle, Task task) CreateTexture(ImageFilter filter = ImageFilter.Linear,
        ImageTiling tiling = ImageTiling.Repeat,
        bool mips = false, string? debugName = null);

    /// <summary>
    ///     Saves the image as png
    /// </summary>
    /// <param name="stream"></param>
    public void Save(Stream stream);

    public void Mutate(Action<IMutationContext> mutator);

    public Buffer<byte> ToBuffer();
}