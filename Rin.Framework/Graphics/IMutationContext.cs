namespace Rin.Framework.Graphics;

public interface IMutationContext
{
    public IMutationContext DrawImage(IHostImage image, in Offset2D offset);

    public IMutationContext AddImage(IHostImage image, in Offset2D offset);

    public IMutationContext DrawRect(in Offset2D offset, in Extent2D extent, params double[] values);

    public IMutationContext Fill(params double[] values);
}