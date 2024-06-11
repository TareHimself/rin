using aerox.Runtime.Math;

namespace aerox.Runtime.Scene.Components;

public class CameraComponent : RenderedComponent
{


    public float FieldOfView = 90.0f;

    public float NearClipPlane = 0.01f;

    public float FarClipPlane = 10000.0f;
    public Matrix4 GetViewMatrix()
    {
        Matrix4 transformMatrix = GetWorldTransform();
        return transformMatrix;
    }


    public Matrix4 GetProjection(float width, float height)
    {
        return Glm.Perspective(float.DegreesToRadians(FieldOfView), width / height, NearClipPlane, FarClipPlane);
    }
}