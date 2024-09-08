using aerox.Runtime.Math;

namespace aerox.Runtime.Scene.Graphics;

public interface ISceneDrawable
{
    void Collect(SceneFrame frame, Matrix4 parentSpace);
}