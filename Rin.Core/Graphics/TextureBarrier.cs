namespace Rin.Core.Graphics;

public struct TextureBarrier(ResourceHandle texture, ImageLayout from, ImageLayout to)
{
    public ResourceHandle Texture = texture;
    public ImageLayout From = from;
    public ImageLayout To = to;
}