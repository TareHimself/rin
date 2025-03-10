using System.Numerics;
using Rin.Engine.Core.Math;

namespace Rin.Engine.Views.Utilities;

public interface IHitTestable
{
    public Vector2 GetRelativeSize();
    public Mat3 GetRelativeTransform();
    public void OnHit();
}