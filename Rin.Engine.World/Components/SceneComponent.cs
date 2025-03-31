using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Core.Math;
using Rin.Engine.World.Graphics;
using Rin.Engine.World.Math;

namespace Rin.Engine.World.Components;

public class SceneComponent : Component, ISceneComponent
{
    private readonly object _lock = new();
    private ISceneComponent? _transformParent = null;
    private readonly HashSet<ISceneComponent> _children = [];
    
    [PublicAPI]
    public Vector3 Location = new(0.0f);
    [PublicAPI]
    public Quaternion Rotation = Quaternion.Identity;
    [PublicAPI]
    public Vector3 Scale = new (1.0f);
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

    public void SetLocation(in Vector3 location, Space space = Space.Local)
    {
        switch (space)
        {
            case Space.Local:
                Location = location;
                break;
            case Space.World:
            {
                SetTransform(GetTransform(space) with { Location = location},space);
            }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(space), space, null);
        }
    }

    public void Translate(in Vector3 translation, Space space = Space.Local)
    {
        switch (space)
        {
            case Space.Local:
                Location += translation;
                break;
            case Space.World:
            {
                var worldTransform = GetTransform(space);
                worldTransform.Location += translation;
                SetTransform(worldTransform,space);
            }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(space), space, null);
        }
    }

    public void SetRotation(in Quaternion rotation, Space space = Space.Local)
    {
        switch (space)
        {
            case Space.Local:
                Rotation = rotation;
                break;
            case Space.World:
            {
                SetTransform(GetTransform(space) with { Rotation = rotation},space);
            }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(space), space, null);
        }
    }

    public void Rotate(in Vector3 axis, float delta, Space space = Space.Local)
    {
         switch (space)
        {
            case Space.Local:
                Rotation = Rotation.AddLocal(axis, delta);
                break;
            case Space.World:
            {
                var worldTransform = GetTransform(space);
                worldTransform.Rotation = Rotation.Add(axis, delta);
                SetTransform(worldTransform,space);
            }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(space), space, null);
        }
    }
    
    public void SetScale(in Vector3 scale, Space space = Space.Local)
    {
        switch (space)
        {
            case Space.Local:
                Scale = scale;
                break;
            case Space.World:
            {
                SetTransform(GetTransform(space) with { Scale = scale},space);
            }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(space), space, null);
        }
    }

    public void SetTransform(in Transform transform, Space space = Space.Local)
    {
        switch (space)
        {
            case Space.Local:
                Location = transform.Location;
                Rotation = transform.Rotation;
                Scale = transform.Scale;
                break;
            case Space.World:
            {
                if (TransformParent != null)
                {
                    var targetWorldMatrix = transform.ToMatrix();
                    var thisWorldMatrix = GetTransform(space).ToMatrix();

                    var thisToTarget = thisWorldMatrix.Inverse() * targetWorldMatrix;
                    var thisToParent = GetTransform().ToMatrix();
                    
                    Matrix4x4.Decompose(thisToTarget * thisToParent.Inverse(),out Location,out Rotation,out Scale);
                    return;
                }

                Location = transform.Location;
                Rotation = transform.Rotation;
                Scale = transform.Scale;
            }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(space), space, null);
        }
    }

    public Vector3 GetLocation(Space space = Space.Local)
    {
        return space switch
        {
            Space.Local => Location,
            Space.World => GetTransform(space).Location,
            _ => throw new ArgumentOutOfRangeException(nameof(space), space, null)
        };
    }

    public Quaternion GetRotation(Space space = Space.Local)
    {
        return space switch
        {
            Space.Local => Rotation,
            Space.World => GetTransform(space).Rotation,
            _ => throw new ArgumentOutOfRangeException(nameof(space), space, null)
        };
    }

    public Vector3 GetScale(Space space = Space.Local)
    {
        return space switch
        {
            Space.Local => Scale,
            Space.World => GetTransform(space).Scale,
            _ => throw new ArgumentOutOfRangeException(nameof(space), space, null)
        };
    }

    public Transform GetTransform(Space space = Space.Local)
    {
        switch (space)
        {
            case Space.Local:
                return new Transform()
                {
                    Location = Location,
                    Rotation = Rotation,
                    Scale = Scale
                };
            case Space.World:
            {
                if (TransformParent == null) return new Transform()
                {
                    Location = Location,
                    Rotation = Rotation,
                    Scale = Scale
                };

                var parentTransform = TransformParent.GetTransform(space);
                
                return GetTransform().InParentSpace(parentTransform);
            }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(space), space, null);
        }
    }


    [PublicAPI]
    public ISceneComponent[] GetAttachedComponents()
    {
        lock (_lock)
        {
            return _children.ToArray();
        }
    }
    
    
    public virtual void Collect(DrawCommands drawCommands, Matrix4x4 parentTransform)
    {
        var relativeTransform = GetTransform().ToMatrix();
        var myTransform = relativeTransform * parentTransform;
        if(Visible) CollectSelf(drawCommands, myTransform);
        foreach (var attachedComponent in GetAttachedComponents())
        {
            attachedComponent.Collect(drawCommands, myTransform);
        }
    }
    
    protected virtual void CollectSelf(DrawCommands drawCommands, Matrix4x4 transform)
    {
        
    }
}