using System.Numerics;
using rin.Framework.Core.Math;

namespace rin.Editor.Scene.Physics.Bepu;

public static class Extensions
{
    public static Vector3 ToVector3(this in Vector3 vector) => new Vector3(vector.X, vector.Y, vector.Z);
    public static Quaternion ToQuaternion(this in Quat quat) => new Quaternion(quat.X, quat.Y, quat.Z, quat.W);
}