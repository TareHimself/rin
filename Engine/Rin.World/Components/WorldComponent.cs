using System.Diagnostics;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Shared.Math;
using Rin.World.Graphics;
using Rin.World.Math;

namespace Rin.World.Components;

public class WorldComponent : Component, IWorldComponent
{
    private readonly HashSet<IWorldComponent> _children = [];

    private Matrix4x4 _localMatrix = Matrix4x4.Identity;
    private bool _localMatrixValid;

    private Transform _cachedWorldTransform;
    private bool _worldTransformDirty = true;

    /// <summary>
    ///     Bumped on every real recompute, regardless of who triggered it — safe for change detection.
    /// </summary>
    [PublicAPI]
    public uint TransformVersion { get; private set; }

    [PublicAPI]
    public Vector3 Location
    {
        get;
        set
        {
            field = value;
            _localMatrixValid = false;
            MarkWorldTransformDirty();
        }
    } = Vector3.Zero;

    [PublicAPI]
    public Quaternion Rotation
    {
        get;
        set
        {
            field = value;
            _localMatrixValid = false;
            MarkWorldTransformDirty();
        }
    } = Quaternion.Identity;

    [PublicAPI]
    public Vector3 Scale
    {
        get;
        set
        {
            field = value;
            _localMatrixValid = false;
            MarkWorldTransformDirty();
        }
    } = Vector3.One;

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
            MarkWorldTransformDirty();
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
                MarkWorldTransformDirty();
                return true;
            }

            return false;
        }

        return true;
    }

    private void MarkWorldTransformDirty()
    {
        if (_worldTransformDirty) return;
        _worldTransformDirty = true;
        foreach (var child in GetAttachedComponents())
            if (child is WorldComponent worldChild)
                worldChild.MarkWorldTransformDirty();
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

                    Matrix4x4.Decompose(thisToTarget * thisToParent.Inverse(),
                        out var decomposedScale, out var decomposedRotation, out var decomposedTranslation);
                    Scale = decomposedScale;
                    Rotation = decomposedRotation;
                    Location = decomposedTranslation;
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
                if (!_worldTransformDirty) return _cachedWorldTransform;

                _cachedWorldTransform = TransformParent == null
                    ? new Transform
                    {
                        Position = Location,
                        Orientation = Rotation,
                        Scale = Scale
                    }
                    : GetTransform().InParentSpace(TransformParent.GetTransform(space));
                _worldTransformDirty = false;
                TransformVersion++;
                return _cachedWorldTransform;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(space), space, null);
        }
    }


    /// <summary>
    ///     This component's local transform as a matrix, cached until <see cref="Location" /> /
    ///     <see cref="Rotation" /> / <see cref="Scale" /> next change.
    /// </summary>
    [PublicAPI]
    public Matrix4x4 GetLocalMatrix()
    {
        if (_localMatrixValid) return _localMatrix;
        _localMatrix = GetTransform().ToMatrix();
        _localMatrixValid = true;
        return _localMatrix;
    }

    public virtual void Collect(CommandList commandList, Matrix4x4 parentTransform)
    {
        var myTransform = GetLocalMatrix() * parentTransform;
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