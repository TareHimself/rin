using Rin.Framework.Graphics.Textures;

namespace Rin.Framework.Graphics;

public interface IHostImage : IDisposable
{
    public Extent2D Extent { get; }
    public uint Channels { get; }

    public HostImageFormat Format { get; }

    public (ImageHandle handle, Task task) CreateTexture(ImageFilter filter = ImageFilter.Linear,
        ImageTiling tiling = ImageTiling.Repeat,
        bool mips = false, string? debugName = null);

    /// <summary>
    ///     Saves the image as png
    /// </summary>
    /// <param name="stream"></param>
    public void Save(Stream stream);

    public IHostImage Mutate(Action<IMutationContext> mutator);

    public IHostImage Mutate(Func<IMutationContext, IMutationContext> mutator)
    {
        return Mutate(m => { mutator(m); });
    }

    public Buffer<byte> ToBuffer();
}