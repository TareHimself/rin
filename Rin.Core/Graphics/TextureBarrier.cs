using Rin.Core.Graphics.Images;

namespace Rin.Core.Graphics;

public struct TextureBarrier(ITexture texture, ImageLayout from, ImageLayout to)
{
    public ITexture Texture = texture;
    public ImageLayout From = from;
    public ImageLayout To = to;
}