using rin.Framework.Core.Math;
using rin.Runtime.Core.Math;
using rin.Scene.Graphics;

namespace rin.Scene.Components;

public class TransformComponent : Component
{
    private Transform _relativeTransform = new();

    private TransformComponent? _parent = null;
    private readonly HashSet<TransformComponent> _children = [];

    public void AttachTo(TransformComponent component)
    {
        _parent = component;
        component._children.Add(this);
    }
    
    public Transform GetRelativeTransform()
    {
        return _relativeTransform.Clone();
    }
    
    public Vec3<float> GetRelativeLocation()
    {
        return _relativeTransform.Location.Clone();
    }
    
    public Quat GetRelativeRotation()
    {
        return _relativeTransform.Rotation.Clone();
    }
    
    public Vec3<float> GetRelativeScale()
    {
        return _relativeTransform.Scale.Clone();
    }
    
    public void SetRelativeTransform(Transform transform)
    {
        _relativeTransform = transform.Clone();
    }
    
    public void SetRelativeLocation(Vec3<float> location)
    {
        _relativeTransform.Location = location.Clone();
    }
    
    public void SetRelativeRotation(Quat rotation)
    {
        _relativeTransform.Rotation = rotation.Clone();
    }
    
    public void SetRelativeScale(Vec3<float> scale)
    {
        _relativeTransform.Scale = scale.Clone();
    }
    
    public void SetRelativeLocation(float? x = null,float? y = null,float? z = null)
    {
        _relativeTransform.Location.X = x ?? _relativeTransform.Location.X;
        _relativeTransform.Location.Y = y ?? _relativeTransform.Location.Y;
        _relativeTransform.Location.Z = z ?? _relativeTransform.Location.Z;
    }
    
    public void SetRelativeRotation(float? x = null,float? y = null,float? z = null,float? w = null)
    {
        _relativeTransform.Rotation.X = x ?? _relativeTransform.Rotation.X;
        _relativeTransform.Rotation.Y = y ?? _relativeTransform.Rotation.Y;
        _relativeTransform.Rotation.Z = z ?? _relativeTransform.Rotation.Z;
        _relativeTransform.Rotation.W = w ?? _relativeTransform.Rotation.W;
    }
    
    public void SetRelativeScale(float? x = null,float? y = null,float? z = null)
    {
        _relativeTransform.Scale.X = x ?? _relativeTransform.Scale.X;
        _relativeTransform.Scale.Y = y ?? _relativeTransform.Scale.Y;
        _relativeTransform.Scale.Z = z ?? _relativeTransform.Scale.Z;
    }

    public Transform GetWorldTransform()
    {
        if (Parent == null) return GetRelativeTransform();

        Mat4 parentTransform = Parent.GetWorldTransform();
        return parentTransform * _relativeTransform;
    }

    public void SetWorldTransform(Transform worldTransform)
    {
        if (Parent != null)
        {
            Mat4 targetWorldMatrix = worldTransform;
            Mat4 thisWorldMatrix = GetWorldTransform();

            var thisToTarget = thisWorldMatrix.Inverse() * targetWorldMatrix;
            Mat4 thisToParent = GetRelativeTransform();

            SetRelativeTransform(thisToTarget * thisToParent.Inverse());
            return;
        }
        
        SetRelativeTransform(worldTransform);
    }
    
    protected virtual void CollectSelf(SceneFrame frame, Mat4 parentTransform,Mat4 myTransform)
    {
    }

    protected virtual void CollectChildren(SceneFrame frame, Mat4 parentTransform,Mat4 myTransform)
    {
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Collect(frame,myTransform);
        }
    }

    public virtual void Collect(SceneFrame frame, Mat4 parentSpace)
    {
        var myTransform = parentSpace * GetRelativeTransform();
        CollectSelf(frame,parentSpace,myTransform);
        CollectChildren(frame,parentSpace,myTransform);
    }
}