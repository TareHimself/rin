using System.Numerics;
using Rin.Core.Shared;

namespace Rin.World.Graphics.Default;

public class PoseInfo
{
    public int Id;
    public PooledMemory<Matrix4x4> Pose;
}