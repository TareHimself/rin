using JetBrains.Annotations;
using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Graphics;

namespace rin.Framework.Scene.Components;

public class SceneComponent : Component, ISceneComponent
{
    private readonly object _lock = new();
    private ISceneComponent? _transformParent = null;
    private readonly HashSet<ISceneComponent> _children = [];
    
    [PublicAPI]
    public Vec3<float> Location = new(0.0f);
    [PublicAPI]
    public Rotator Rotation = new (0.0f);
    [PublicAPI]
    public Vec3<float> Scale = new (1.0f);
    [PublicAPI]
    public bool Visible { get; set; } = true;
    [PublicAPI]
    public ISceneComponent? TransformParent => _transformParent;

    [PublicAPI]
    public bool TryHandleDetachment(ISceneComponent target)
    {
        lock (_lock)
        {
            _children.Remove(target);
        }
        return true;
    }

    [PublicAPI]
    public bool TryHandleAttachment(ISceneComponent target)
    {
        lock (_lock)
        {
            _children.Add(target);
        }
        return true;
    }

    public bool AttachTo(ISceneComponent component)
    {
        if (component.TryHandleAttachment(this))
        {
            _transformParent = component;
            return true;
        }

        return false;
    }
    
    public bool Detach()
    {
        if (_transformParent is { } parent)
        {
            if (parent.TryHandleDetachment(this))
            {
                _transformParent = null;
                return true;
            }
            return false;
        }

        return true;
    }

    [PublicAPI]
    public void SetRelativeLocation(float? x = null,float? y = null,float? z = null)
    {
        var location = GetRelativeLocation();
        Location = new Vec3<float>(x ?? location.X, y ?? location.Y, z ?? location.Z);
    }
    [PublicAPI]
    public void SetRelativeRotation(float? pitch = null,float? yaw = null,float? roll = null)
    {
        var rotation = GetRelativeRotation();
        Rotation = new Rotator(pitch ?? rotation.Pitch, yaw ?? rotation.Yaw, roll ?? rotation.Roll);
    }
    [PublicAPI]
    public void SetRelativeScale(float? x = null,float? y = null,float? z = null)
    {
        var scale = GetRelativeScale();
        Scale = new Vec3<float>(x ?? scale.X, y ?? scale.Y, z ?? scale.Z);
    }
    
    [PublicAPI]
    public void AddRelativeLocation( float? x = null, float? y = null,
        float? z = null)
    {
        SetRelativeLocation(Location.Mutate((l) =>
        {
            if (x is { } dx)
            {
                l.X += dx;
            }
            
            if (y is { } dy)
            {
                l.Y += dy;
            }
            
            if (z is { } dz)
            {
                l.Z += dz;
            }

            return l;
        }));
    }
    [PublicAPI]
    public void AddRelativeRotation(float? pitch = null, float? yaw = null, float? roll = null)
    {
        SetRelativeRotation(Rotation.Delta(pitch, yaw, roll));
    }
    [PublicAPI]
    public void AddRelativeScale(float? x = null, float? y = null, float? z = null)
    {
        SetRelativeScale(Scale.Mutate((l) =>
        {
            if (x is { } dx)
            {
                l.X += dx;
            }
            
            if (y is { } dy)
            {
                l.Y += dy;
            }
            
            if (z is { } dz)
            {
                l.Z += dz;
            }

            return l;
        }));
    }


    [PublicAPI]
    public ISceneComponent[] GetAttachedComponents()
    {
        lock (_lock)
        {
            return _children.ToArray();
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
    
    public Rotator GetRelativeRotation()
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
    
    public void SetRelativeRotation(Rotator rotation)
    {
        Rotation = rotation.Clone();
    }
    
    public void SetRelativeScale(Vec3<float> scale)
    {
        Scale = scale.Clone();
    }
    
    [PublicAPI]
    public Transform GetSceneTransform()
    {
        if (TransformParent == null) return GetRelativeTransform();

        Mat4 parentTransform = TransformParent.GetSceneTransform();
        return parentTransform * GetRelativeTransform();
     }

    public void SetSceneTransform(Transform worldTransform)
    {
        if (TransformParent != null)
        {
            Mat4 targetWorldMatrix = worldTransform;
            Mat4 thisWorldMatrix = GetSceneTransform();

            var thisToTarget = thisWorldMatrix.Inverse() * targetWorldMatrix;
            Mat4 thisToParent = GetRelativeTransform();

            SetRelativeTransform(thisToTarget * thisToParent.Inverse());
            return;
        }
        
        SetRelativeTransform(worldTransform);
    }

    
    
    public virtual void Collect(DrawCommands drawCommands, Mat4 parentTransform)
    {
        var relativeTransform = (Mat4)GetRelativeTransform();
        var myTransform = parentTransform * relativeTransform;
        if(Visible) CollectSelf(drawCommands, myTransform);
        foreach (var attachedComponent in GetAttachedComponents())
        {
            attachedComponent.Collect(drawCommands, myTransform);
        }
    }
    
    protected virtual void CollectSelf(DrawCommands drawCommands, Mat4 transform)
    {
        
    }
}