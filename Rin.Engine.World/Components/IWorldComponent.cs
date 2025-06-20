﻿using System.Numerics;
using Rin.Engine.Math;
using Rin.Engine.World.Graphics;
using Rin.Engine.World.Math;

namespace Rin.Engine.World.Components;

public interface IWorldComponent : IComponent
{
    public IWorldComponent? TransformParent { get; }
    public bool TryHandleDetachment(IWorldComponent target);
    public bool TryHandleAttachment(IWorldComponent target);
    public bool AttachTo(IWorldComponent component);
    public bool Detach();
    public void SetLocation(in Vector3 location, Space space = Space.Local);
    public void Translate(in Vector3 translation, Space space = Space.Local);
    public void SetRotation(in Quaternion rotation, Space space = Space.Local);
    public void Rotate(in Vector3 axis, float delta, Space space = Space.Local);
    public void SetScale(in Vector3 scale, Space space = Space.Local);
    public void SetTransform(in Transform transform, Space space = Space.Local);
    public Vector3 GetLocation(Space space = Space.Local);
    public Quaternion GetRotation(Space space = Space.Local);
    public Vector3 GetScale(Space space = Space.Local);
    public Transform GetTransform(Space space = Space.Local);
    public void Collect(CommandList commandList, Matrix4x4 parentTransform);
}