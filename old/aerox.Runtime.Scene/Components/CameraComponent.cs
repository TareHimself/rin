using aerox.Runtime.Math;

namespace aerox.Runtime.Scene.Components;

public class CameraComponent : RenderedComponent
{


    public float FieldOfView = 90.0f;

    public float NearClipPlane = 1f;

    public float FarClipPlane = 1000.0f;
    public Matrix4 GetViewMatrix()
    {
        var worldTransform = GetWorldTransform();
        Matrix4 transformMatrix = new Transform()
        {
            Location = worldTransform.Location,
            Rotation = worldTransform.Rotation
        };

        return transformMatrix.Inverse();
    }


    public Matrix4 GetProjection(float width, float height)
    {
        return Glm.Perspective(float.DegreesToRadians(FieldOfView), width / height, NearClipPlane, FarClipPlane).RotateDeg(180.0f,new Vector3<float>(1f,0f,0f));
    }
}