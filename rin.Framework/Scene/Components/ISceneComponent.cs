using JetBrains.Annotations;
using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Graphics;

namespace rin.Framework.Scene.Components;

public interface ISceneComponent : IComponent
{
    public ISceneComponent? TransformParent { get; }
    public bool TryHandleDetachment(ISceneComponent target);
    public bool TryHandleAttachment(ISceneComponent target);
    public bool AttachTo(ISceneComponent component);
    public bool Detach();
    public void SetRelativeLocation(float? x = null, float? y = null, float? z = null);
    public void SetRelativeRotation(float? pitch = null, float? yaw = null, float? roll = null);
    public void SetRelativeScale(float? x = null, float? y = null, float? z = null);
    public void AddRelativeLocation(float? x = null, float? y = null,
        float? z = null);
    public void AddRelativeRotation(float? pitch = null, float? yaw = null, float? roll = null);
    public void AddRelativeScale(float? x = null, float? y = null, float? z = null);
    public ISceneComponent[] GetAttachedComponents();
    public Transform GetRelativeTransform();
    public Vec3<float> GetRelativeLocation();
    public Rotator GetRelativeRotation();
    public Vec3<float> GetRelativeScale();
    public void SetRelativeTransform(Transform transform);
    public void SetRelativeLocation(Vec3<float> location);
    public void SetRelativeRotation(Rotator rotation);
    public void SetRelativeScale(Vec3<float> scale);
    public Transform GetSceneTransform();
    public void SetSceneTransform(Transform worldTransform);
    public void Collect(DrawCommands drawCommands, Mat4 parentTransform);
}