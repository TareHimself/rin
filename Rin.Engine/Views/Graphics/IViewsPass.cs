using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.Views.Graphics;

public interface IViewsPass : IPass
{
    public static abstract IViewsPass Create(PassCreateInfo info);
}