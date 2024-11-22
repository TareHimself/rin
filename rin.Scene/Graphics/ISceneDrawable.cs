using rin.Core.Math;

namespace rin.Scene.Graphics;

public interface ISceneDrawable
{
    void Collect(SceneFrame frame, Matrix4 parentSpace);
}