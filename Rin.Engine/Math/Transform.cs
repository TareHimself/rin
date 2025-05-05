using System.Diagnostics.Contracts;
using System.Numerics;

namespace Rin.Engine.Math;

public struct Transform()
{
    public Vector3 Position = Vector3.Zero;
    public Quaternion Orientation = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;


    public static Transform From(Matrix4x4 matrix)
    {
        var result = new Transform();
        Matrix4x4.Decompose(matrix, out result.Scale, out result.Orientation, out result.Position);
        return result;
    }

    [Pure]
    public Matrix4x4 ToMatrix()
    {
        var rotation = Matrix4x4.CreateFromQuaternion(Orientation);
        var scale = Matrix4x4.CreateScale(Scale);
        var translation = Matrix4x4.CreateTranslation(Position);

        return scale * rotation * translation;
    }

    public void Deconstruct(out Vector3 location, out Quaternion rotation, out Vector3 scale)
    {
        location = Position;
        rotation = Orientation;
        scale = Scale;
    }

    public Transform InParentSpace(Transform parent)
    {
        return From(ToMatrix() * parent.ToMatrix());
    }

    public Transform InParentSpace(Matrix4x4 parent)
    {
        return From(ToMatrix() * parent);
    }
}