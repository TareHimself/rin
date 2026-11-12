using Rin.Core.Shared.Buffers;

namespace Rin.Core.Graphics;

public interface IHostImage : IDisposable
{
    public Extent2D Extent { get; }
    public uint Channels { get; }

    public HostImageFormat Format { get; }

    /// <summary>
    ///     Creates a sampled texture and queues this image as its contents.
    /// </summary>
    public ResourceHandle CreateTexture(bool mips = false, IGraphicsModule? graphicsModule = null);
    
    /// <summary>
    /// Saves the image as png
    /// </summary>
    public void SaveRaw(Stream stream);
    
    /// <summary>
    /// Saves the image as png
    /// </summary>
    public void SavePng(Stream stream);

    public IHostImage Mutate(Action<IMutationContext> mutator);

    public IHostImage Mutate(Func<IMutationContext, IMutationContext> mutator)
    {
        return Mutate(m => { mutator(m); });
    }
}