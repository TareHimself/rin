using System.Diagnostics;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.World.Graphics;
using Rin.Engine.World.Math;
using Rin.Framework.Shared.Math;

namespace Rin.Engine.World.Components;

public class WorldComponent : Component, IWorldComponent
{
    private readonly HashSet<IWorldComponent> _children = [];

    [PublicAPI] public Vector3 Location = new(0.0f);

    [PublicAPI] public Quaternion Rotation = Quaternion.Identity;

    [PublicAPI] public Vector3 Scale = new(1.0f);

    [PublicAPI] public bool Visible { get; set; } = true;

    [PublicAPI] public IWorldComponent? TransformParent { get; private set; }


    public override void Stop()
    {
        if (TransformParent is not null) Detach();
        base.Stop();
    }

    [PublicAPI]
    public bool TryHandleDetachment(IWorldComponent target)
    {
        _children.Remove(target);
        return true;
    }

    [PublicAPI]
    public bool TryHandleAttachment(IWorldComponent target)
    {
        _children.Add(target);
        return true;
    }

    public bool AttachTo(IWorldComponent component)
    {
        Debug.Assert(this != component, "Cannot attach component to self");
        if (component.TryHandleAttachment(this))
        {
            TransformParent = component;
            return true;
        }

        return false;
    }

    public bool Detach()
    {
        if (TransformParent is { } parent)
        {
            if (parent.TryHandleDetachment(this))
            {
                TransformParent = null;
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
                SetTransform(GetTransform(space) with { Position = location }, space);
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
                worldTransform.Position += translation;
                SetTransform(worldTransform, space);
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
                SetTransform(GetTransform(space) with { Orientation = rotation }, space);
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
                worldTransform.Orientation = Rotation.Add(axis, delta);
                SetTransform(worldTransform, space);
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
                SetTransform(GetTransform(space) with { Scale = scale }, space);
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
                Location = transform.Position;
                Rotation = transform.Orientation;
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

                    Matrix4x4.Decompose(thisToTarget * thisToParent.Inverse(), out Location, out Rotation, out Scale);
                    return;
                }

                Location = transform.Position;
                Rotation = transform.Orientation;
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
            Space.World => GetTransform(space).Position,
            _ => throw new ArgumentOutOfRangeException(nameof(space), space, null)
        };
    }

    public Quaternion GetRotation(Space space = Space.Local)
    {
        return space switch
        {
            Space.Local => Rotation,
            Space.World => GetTransform(space).Orientation,
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
                return new Transform
                {
                    Position = Location,
                    Orientation = Rotation,
                    Scale = Scale
                };
            case Space.World:
            {
                if (TransformParent == null)
                    return new Transform
                    {
                        Position = Location,
                        Orientation = Rotation,
                        Scale = Scale
                    };

                var parentTransform = TransformParent.GetTransform(space);

                return GetTransform().InParentSpace(parentTransform);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(space), space, null);
        }
    }


    public virtual void Collect(CommandList commandList, Matrix4x4 parentTransform)
    {
        var relativeTransform = GetTransform().ToMatrix();
        var myTransform = relativeTransform * parentTransform;
        if (Visible) CollectSelf(commandList, myTransform);
        foreach (var attachedComponent in GetAttachedComponents()) attachedComponent.Collect(commandList, myTransform);
    }


    [PublicAPI]
    public IWorldComponent[] GetAttachedComponents()
    {
        return _children.ToArray();
    }

    protected virtual void CollectSelf(CommandList commandList, Matrix4x4 transform)
    {
    }
}