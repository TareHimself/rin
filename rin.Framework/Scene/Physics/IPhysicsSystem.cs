using rin.Framework.Core.Math;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Physics;

public interface IPhysicsSystem : IDisposable
{
    public Vec3<float> Gravity { get; set; }
    public IPhysicsBox CreateBox(ISceneComponent owner,Vec3<float> size);
    public IPhysicsSphere CreateSphere(ISceneComponent owner,float radius);
    
    public void Update(double deltaTime);

    public void Start();
}