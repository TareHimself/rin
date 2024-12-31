using JetBrains.Annotations;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Graphics;
using rin.Framework.World.Components;

namespace rin.Framework.Scene.Components;

public class SceneComponent : Component
{
    private readonly object _lock = new();
    private Transform _relativeTransform = new();
    private SceneComponent? _parent = null;
    private readonly HashSet<SceneComponent> _children = [];
    

    [PublicAPI]
    public SceneComponent? Parent => _parent;

    [PublicAPI]
    public IEnumerable<SceneComponent> GetAttachedComponents()
    {
        lock (_lock)
        {
            return _children.ToArray();
        }
    }
    
    [PublicAPI]
    protected virtual void OnComponentAttached(SceneComponent component)
    {
        lock (_lock)
        {
            _children.Add(component);
        }
    }
    
    protected virtual void OnComponentDetached(SceneComponent component)
    {
        lock (_lock)
        {
            _children.Remove(component);
        }
    }
    
    public void AttachTo(SceneComponent component)
    {
        _parent = component;
        component.OnComponentAttached(this);
    }
    
    public void Detach()
    {
        if (_parent is { } parent)
        {
            parent.OnComponentDetached(this);
            _parent = null;
        }
    }
    
    public Transform GetRelativeTransform()
    {
        return _relativeTransform.Clone();
    }
    
    public Vector3<float> GetRelativeLocation()
    {
        return _relativeTransform.Location.Clone();
    }
    
    public Quaternion GetRelativeRotation()
    {
        return _relativeTransform.Rotation.Clone();
    }
    
    public Vector3<float> GetRelativeScale()
    {
        return _relativeTransform.Scale.Clone();
    }
    
    public void SetRelativeTransform(Transform transform)
    {
        _relativeTransform = transform.Clone();
    }
    
    public void SetRelativeLocation(Vector3<float> location)
    {
        _relativeTransform.Location = location.Clone();
    }
    
    public void SetRelativeRotation(Quaternion rotation)
    {
        _relativeTransform.Rotation = rotation.Clone();
    }
    
    public void SetRelativeScale(Vector3<float> scale)
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

    [PublicAPI]
    public Transform GetWorldTransform()
    {
        if (Parent == null) return GetRelativeTransform();

        Matrix4 parentTransform = Parent.GetWorldTransform();
        return parentTransform * _relativeTransform;
    }

    public void SetWorldTransform(Transform worldTransform)
    {
        if (Parent != null)
        {
            Matrix4 targetWorldMatrix = worldTransform;
            Matrix4 thisWorldMatrix = GetWorldTransform();

            var thisToTarget = thisWorldMatrix.Inverse() * targetWorldMatrix;
            Matrix4 thisToParent = GetRelativeTransform();

            SetRelativeTransform(thisToTarget * thisToParent.Inverse());
            return;
        }
        
        SetRelativeTransform(worldTransform);
    }
    
    public virtual void Collect(DrawCommands drawCommands, Matrix4 parentTransform)
    {
        var myTransform = parentTransform * GetRelativeTransform();
        
        foreach (var attachedComponent in GetAttachedComponents())
        {
            attachedComponent.Collect(drawCommands, myTransform);
        }
    }
}