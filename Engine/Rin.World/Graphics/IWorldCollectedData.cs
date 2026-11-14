using Rin.Core.Graphics.Graph;

namespace Rin.World.Graphics;

public interface IWorldCollectedData : ICollectedData, IDisposable
{
    public uint GetOutputImageId();
}
