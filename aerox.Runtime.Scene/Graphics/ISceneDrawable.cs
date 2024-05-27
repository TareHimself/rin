using aerox.Runtime.Math;

namespace aerox.Runtime.Scene.Graphics;

public interface ISceneDrawable
{
    void Draw(SceneFrame frame, Matrix4 parentSpace);
}