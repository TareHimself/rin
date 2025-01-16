using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Physics.Bepu;

public class BepuPhysics : IPhysicsSystem
{
    private BufferPool _pool = new BufferPool();
    private readonly Dictionary<ISceneComponent, IPhysicsBody> _bodies = [];
    public Simulation Simulation { get; set; }
    public int VelocityIterationCount { get; private set; } = 8;
    public int SubstepCount { get; private set; } = 1;

    public AngularIntegrationMode AngularIntegrationMode { get; set; } = AngularIntegrationMode.ConserveMomentum;
    public bool AllowSubstepsForUnconstrainedBodies { get; set; } = true;
    public bool IntegrateVelocityForKinematics { get; set; } = true;

    struct NarrowPhaseCallbacks(BepuPhysics system) : INarrowPhaseCallbacks
    {
        public void Initialize(Simulation simulation)
        {
  
        }

        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
        {
            return true;
        }

        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return true;
        }
        
        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
            out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial = new PairMaterialProperties { FrictionCoefficient = 1, MaximumRecoveryVelocity = 2,SpringSettings = new SpringSettings(30, 1) };
            
           return true;
        }

        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB,
            ref ConvexContactManifold manifold)
        {
           return true;
        }

        public void Dispose()
        {
   
        }
    }

    struct PoseIntegratorCallbacks(BepuPhysics system) : IPoseIntegratorCallbacks
    {
        Vector3Wide _gravityWideDt;
        Vector<float> _linearDampingDt;
        Vector<float> _angularDampingDt;
        
        public void Initialize(Simulation simulation)
        {
        }

        public void PrepareForIntegration(float dt)
        {
            _linearDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - system.LinearDamping, 0, 1), dt));
            _angularDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - system.AngularDamping, 0, 1), dt));
            _gravityWideDt = Vector3Wide.Broadcast(system.Gravity.ToVector3() * dt);
        }

        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
            BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
        {
            velocity.Linear = (velocity.Linear + _gravityWideDt) * _linearDampingDt;
            velocity.Angular *= _angularDampingDt;
        }

        public AngularIntegrationMode AngularIntegrationMode => system.AngularIntegrationMode;
        public bool AllowSubstepsForUnconstrainedBodies => system.AllowSubstepsForUnconstrainedBodies;
        public bool IntegrateVelocityForKinematics => system.IntegrateVelocityForKinematics;
    }
    
    public BepuPhysics()
    {
        Simulation = Simulation.Create(_pool,new NarrowPhaseCallbacks(this),new PoseIntegratorCallbacks(this),new SolveDescription(VelocityIterationCount,SubstepCount));
    }
    public void Dispose()
    {
        foreach (var (_,body) in _bodies)
        {
            body.Dispose();
        }
        Simulation.Dispose();
        _pool.Clear();
    }

    public Vec3<float> Gravity { get; set; } = new Vec3<float>(0.0f, -0.4f, 0.0f);

    public float LinearDamping = 0.03f;
    public float AngularDamping = 0.03f;

    public IPhysicsBox CreateBox(ISceneComponent owner, Vec3<float> size)
    {
        var body = new BepuBox(this, size, owner);
        lock (_bodies)
        {
            _bodies.Add(owner,body);
        }

        return body;
    }

    public IPhysicsSphere CreateSphere(ISceneComponent owner,float radius)
    {
        var body = new BepuSphere(this,radius, owner);
        lock (_bodies)
        {
            _bodies.Add(owner,body);
        }
        
        return body;
    }

    public void Update(double deltaTime)
    {
        lock (Simulation)
        {
            Simulation.Timestep((float)deltaTime);
            KeyValuePair<ISceneComponent,IPhysicsBody>[] bodies = [];
            lock (_bodies)
            {
                bodies = _bodies.Where(kv => kv.Value.IsSimulating).ToArray();
            }
            foreach (var (comp, body ) in bodies)
            {
                var transform = body.GetTransform();
                comp.SetSceneTransform(transform);
            }
        }
    }

    public void Start()
    {
        Simulation.Statics.Add(new StaticDescription(new Vector3(0, -100, 0), Simulation.Shapes.Add(new Box(200, 30, 200))));
    }
}