using System.Numerics;

namespace Rin.Engine.Math;

/// <summary>
/// Provides functions and constants for math in Rin
/// </summary>
public static class MathR
{
    public static Vector3 Up => Vector3.UnitY;
    public static Vector3 Right => Vector3.UnitX;   
    public static Vector3 Forward => Vector3.UnitZ;

    public static Matrix4x4 PerspectiveProjection(float fieldOfView, float width, float height, float near, float far)
    {
        var tan = 1 / float.Tan(float.DegreesToRadians(fieldOfView / 2));
        var aspect = width / height;
        var y = tan;
        var x = y / aspect;
        var z = far / (far - near);
        var mat = Matrix4x4.Identity;
        
        mat.M11 = x;
        mat.M22 = -y;
        mat.M33 = z;
        mat.M34 = 1f;
        mat.M43 = (far * near) / (far - near);
        mat.M44 = 0f;

        return mat;   
        // float y = 1f / float.Tan(fieldOfView * 0.5f);
        // float x = y / aspectRatio;
        // float z = float.IsPositiveInfinity(farPlaneDistance) ? 1f : farPlaneDistance / (farPlaneDistance - nearPlaneDistance);
        // Matrix4x4.Impl ofViewLeftHanded;
        // ofViewLeftHanded.X = Vector4.Create(x, 0.0f, 0.0f, 0.0f);
        // ofViewLeftHanded.Y = Vector4.Create(0.0f, y, 0.0f, 0.0f);
        // ofViewLeftHanded.Z = Vector4.Create(0.0f, 0.0f, z, 1f);
        // ofViewLeftHanded.W = Vector4.Create(0.0f, 0.0f, -z * nearPlaneDistance, 0.0f);
        // return ofViewLeftHanded;
        
        //return Matrix4x4.CreatePerspectiveFieldOfView(fieldOfView, width / height, near, far);
        // var fovRad = fieldOfView;
        // var aspect = width / height;
        // var tan = float.Tan(fovRad / 2.0f);
        // var mat = Matrix4x4.Identity;
        //
        // mat.M11 = 1 / (aspect * tan);
        // mat.M22 = 1f / tan;
        // mat.M33 = far / (far - near);
        // mat.M34 = -(far * near) / (far - near);
        // mat.M43 = 1f;
        // mat.M44 = 0f;
        //
        // return mat;
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