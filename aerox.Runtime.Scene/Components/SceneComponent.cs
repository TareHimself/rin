using aerox.Runtime.Math;

namespace aerox.Runtime.Scene.Components;

public class SceneComponent : Component
{
    private Transform _relativeTransform = new();

    public Transform RelativeTransform
    {
        get => _relativeTransform;
        set => _relativeTransform = value.Clone();
    }

    public SceneComponent? Parent { get; private set; }

    public void AttachTo(SceneComponent component)
    {
        Parent = component;
    }

    public Transform GetWorldTransform()
    {
        if (Parent == null) return RelativeTransform;

        Matrix4 parentTransform = Parent.GetWorldTransform();
        return parentTransform * RelativeTransform;
    }

    public void SetWorldTransform(Transform worldTransform)
    {
        if (Parent != null)
        {
            Matrix4 targetWorldMatrix = worldTransform;
            Matrix4 thisWorldMatrix = GetWorldTransform();

            var thisToTarget = thisWorldMatrix.Inverse() * targetWorldMatrix;
            Matrix4 thisToParent = RelativeTransform;

            RelativeTransform = thisToTarget * thisToParent.Inverse();
            return;
        }

        RelativeTransform = worldTransform;
    }
}