using System.Numerics;
using Rin.Engine.Core.Math;
using Rin.Engine.Scene.Graphics;

namespace Rin.Engine.Scene.Components;

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
    public Vector3 GetRelativeLocation();
    public Rotator GetRelativeRotation();
    public Vector3 GetRelativeScale();
    public void SetRelativeTransform(Transform transform);
    public void SetRelativeLocation(Vector3 location);
    public void SetRelativeRotation(Rotator rotation);
    public void SetRelativeScale(Vector3 scale);
    public Transform GetSceneTransform();
    public void SetSceneTransform(Transform worldTransform);
    public void Collect(DrawCommands drawCommands, Mat4 parentTransform);

}