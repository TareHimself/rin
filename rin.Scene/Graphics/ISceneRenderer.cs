using rin.Core.Math;
using rin.Graphics;

namespace rin.Scene.Graphics;

public interface ISceneRenderer : IDisposable
{
    public abstract void Resize(Vector2<int> size);

    public abstract void RenderTo(DeviceImage renderTarget);
}