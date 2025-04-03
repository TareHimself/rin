using System.Numerics;

namespace Rin.Engine.Math;

public static class RMath
{
    public static Vector3 Up => Vector3.UnitY;
    public static Vector3 Right => Vector3.UnitX;
    public static Vector3 Forward => Vector3.UnitZ;

    public static Matrix4x4 PerspectiveProjection(float fieldOfView, float width, float height, float near, float far)
    {
        return Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(fieldOfView, width / height, near, far);
    }
    
    public static Matrix4x4 OrthographicProjection(float left,float right,float top,float bottom,float near,float far)
    {
        return Matrix4x4.CreateOrthographicOffCenter(left, right, top, bottom, near, far);
    }
    
    public static Matrix4x4 ViewportProjection(float width,float height,float near,float far)
    {
        return OrthographicProjection(0.0f, width, 0.0f, height, near, far);
    }

    public static Matrix4x4 LookAt(Vector3 begin, Vector3 end, Vector3 up)
    {
        return Matrix4x4.CreateLookAtLeftHanded(begin, end, up);
    }

    public static Matrix4x4 LookTo(Vector3 location, Vector3 direction, Vector3 up)
    {
        return Matrix4x4.CreateLookToLeftHanded(location, direction, up);
    }

    public static Matrix4x4 LookTo(Vector3 location, Quaternion rotation, Vector3 up)
    {
        return Matrix4x4.CreateLookToLeftHanded(location, Vector3.Transform(Forward, rotation), up);
    }
}