using rin.Framework.Core.Math;
using rin.Framework.Scene.Entities;

namespace rin.Framework.Scene.Components;

public class TransformComponent(Entity owner) : Component(owner)
{
    public TransformComponent? AttachedParent;
    public Vec3<float> Location = new(0.0f);
    public Quat Rotation = Quat.Identity;
    public Vec3<float> Scale = new(1.0f);
}