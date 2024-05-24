using aerox.Runtime.Math;

namespace aerox.Runtime.Scene.Components;

public class SceneComponent : Component
{
    private SceneComponent? _parent = null;
    private Transform _relativeTransform = new();

    public Transform RelativeTransform
    {
        get => _relativeTransform;
        set => _relativeTransform = value.Clone();
    }
    
    public SceneComponent? Parent => _parent;

    public void AttachTo(SceneComponent component)
    {
        _parent = component;
    }

    public Transform GetWorldTransform()
    {
        if (Parent == null)
        {
            return RelativeTransform;
        }

        var parentTransform = Parent.GetWorldTransform();
        return RelativeTransform.RelativeTo(parentTransform);
    }

    public void SetWorldTransform(Transform worldTransform)
    {
        if (Parent != null)
        {
            Matrix4 targetWorldMatrix = worldTransform;
            Matrix4 thisWorldMatrix = GetWorldTransform();

            var thisToTarget = thisWorldMatrix.Inverse() * targetWorldMatrix; 
            Matrix4 thisToParent = RelativeTransform;
    
            RelativeTransform = (thisToTarget * thisToParent.Inverse());
            return;
        }

        RelativeTransform = worldTransform;
    }
}