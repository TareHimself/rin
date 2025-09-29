using Rin.Framework.Graphics.Graph;

namespace Rin.Framework.Views.Graphics;

public interface ICollectedSurfaceData : ICollectedData
{
    public SurfaceContext SurfaceContext { get; }
}