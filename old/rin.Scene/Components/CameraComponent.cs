using rin.Framework.Core.Math;
 
namespace rin.Scene.Components;

public class CameraComponent : RenderedComponent
{


    public float FieldOfView = 90.0f;

    public float NearClipPlane = 1f;

    public float FarClipPlane = 1000.0f;
    public Mat4 GetViewMatrix()
    {
        var worldTransform = GetWorldTransform();
        Mat4 transformMatrix = new Transform()
        {
            Location = worldTransform.Location,
            Rotation = worldTransform.Rotation
        };

        return transformMatrix.Inverse();
    }


    public Mat4 GetProjection(float width, float height)
    {
        return Glm.Perspective(float.DegreesToRadians(FieldOfView), width / height, NearClipPlane, FarClipPlane).RotateDeg(180.0f,new Vec3<float>(1f,0f,0f));
    }
}