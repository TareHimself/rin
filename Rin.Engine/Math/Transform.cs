using System.Diagnostics.Contracts;
using System.Numerics;
using Rin.Engine.Extensions;

namespace Rin.Engine.Math;

public struct Transform()
{
    public Vector3 Location = new(0.0f);
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3 Scale = new(1.0f);


    public static Transform From(Matrix4x4 matrix)
    {
        var result = new Transform();
        Matrix4x4.Decompose(matrix, out result.Scale, out result.Rotation, out result.Location);
        return result;
    }

    [Pure]
    public Matrix4x4 ToMatrix()
    {
        var rotation = Matrix4x4.CreateFromQuaternion(Rotation);
        var scale = Matrix4x4.CreateScale(Scale);
        var translation = Matrix4x4.CreateTranslation(Location);

        return scale * rotation * translation;
    }
    
    public void Deconstruct(out Vector3 location, out Quaternion rotation, out Vector3 scale)
    {
        location = Location;
        rotation = Rotation;
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