using Rin.Core.Graphics.Graph;

namespace Rin.Core.Views.Graphics;

public interface ICollectedSurfaceData : ICollectedData
{
    public SurfaceContext SurfaceContext { get; }
}