namespace rin.Editor.Scene.Physics;

public interface IPhysicsCapsule : IPhysicsBody
{
    public float Radius { get; set; }
    public float HalfHeight { get; set; }
}