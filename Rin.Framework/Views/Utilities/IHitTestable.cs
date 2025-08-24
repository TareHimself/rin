using System.Numerics;

namespace Rin.Framework.Views.Utilities;

public interface IHitTestable
{
    public Vector2 GetRelativeSize();
    public Matrix4x4 GetLocalTransform();
    public void OnHit();
}