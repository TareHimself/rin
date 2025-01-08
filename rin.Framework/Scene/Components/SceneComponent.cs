using JetBrains.Annotations;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Entities;
using rin.Framework.Scene.Graphics;

namespace rin.Framework.Scene.Components;

public class SceneComponent : Component
{
    private readonly object _lock = new();
    private SceneComponent? _parent = null;
    private readonly HashSet<SceneComponent> _children = [];
    public Vec3<float> Location = new(0.0f);
    public Quat Rotation = Quat.Identity;
    public Vec3<float> Scale = new(1.0f);
    

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
        return new Transform()
        {
            Location = Location.Clone(),
            Rotation = Rotation.Clone(),
            Scale = Scale.Clone()
        };
    }
    
    public Vec3<float> GetRelativeLocation()
    {
        return Location.Clone();
    }
    
    public Quat GetRelativeRotation()
    {
        return Rotation.Clone();
    }
    
    public Vec3<float> GetRelativeScale()
    {
        return Scale.Clone();
    }
    
    public void SetRelativeTransform(Transform transform)
    {
        Location = transform.Location.Clone();
        Rotation = transform.Rotation.Clone();
        Scale = transform.Scale.Clone();
    }
    
    public void SetRelativeLocation(Vec3<float> location)
    {
        Location = location.Clone();
    }
    
    public void SetRelativeRotation(Quat rotation)
    {
        Rotation = rotation.Clone();
    }
    
    public void SetRelativeScale(Vec3<float> scale)
    {
        Scale = scale.Clone();
    }
    
    public void SetRelativeLocation(float? x = null,float? y = null,float? z = null)
    {
        Location.X = x ?? Location.X;
        Location.Y = y ?? Location.Y;
        Location.Z = z ?? Location.Z;
    }
    
    public void SetRelativeRotation(float? x = null,float? y = null,float? z = null,float? w = null)
    {
        Rotation.X = x ?? Rotation.X;
        Rotation.Y = y ?? Rotation.Y;
        Rotation.Z = z ?? Rotation.Z;
        Rotation.W = w ?? Rotation.W;
    }
    
    public void SetRelativeScale(float? x = null,float? y = null,float? z = null)
    {
        Scale.X = x ?? Scale.X;
        Scale.Y = y ?? Scale.Y;
        Scale.Z = z ?? Scale.Z;
    }

    [PublicAPI]
    public Transform GetWorldTransform()
    {
        if (Parent == null) return GetRelativeTransform();

        Mat4 parentTransform = Parent.GetWorldTransform();
        return parentTransform * GetRelativeTransform();
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
    
    public virtual void Collect(DrawCommands drawCommands, Mat4 parentTransform)
    {
        var myTransform = parentTransform * GetRelativeTransform();
        
        foreach (var attachedComponent in GetAttachedComponents())
        {
            attachedComponent.Collect(drawCommands, myTransform);
        }
    }
}