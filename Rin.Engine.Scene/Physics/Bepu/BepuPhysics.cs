using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using JetBrains.Annotations;
using Rin.Engine.Scene.Components;

namespace Rin.Engine.Scene.Physics.Bepu;

public class BepuPhysics : IPhysicsSystem
{
    private readonly BufferPool _pool = new BufferPool();
    private readonly Dictionary<IPhysicsComponent, BepuBody> _bodies = [];
    private readonly Dictionary<StaticHandle, BepuBody> _staticBodies = [];
    private readonly Dictionary<BodyHandle, BepuBody> _dynamicBodies = [];
    
    public Simulation Simulation { get; set; }
    
    [PublicAPI]
    public int VelocityIterationCount { get; private set; } = 8;
    
    [PublicAPI]
    public int SubstepCount { get; private set; } = 8;

    [PublicAPI]
    public AngularIntegrationMode AngularIntegrationMode { get; set; } = AngularIntegrationMode.ConserveMomentum;
    [PublicAPI]
    public bool AllowSubstepsForUnconstrainedBodies { get; set; } = true;

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
            pairMaterial = new PairMaterialProperties { FrictionCoefficient = 1, MaximumRecoveryVelocity = 2,SpringSettings = new SpringSettings(1, 1) };
            // var a = system.GetBodyFromCollidable(pair.A);
            // var b = system.GetBodyFromCollidable(pair.B);
            // for (var i = 0; i < manifold.Count; i++)
            // {
            //     if (a.IsSimulating)
            //     {
            //     
            //         var hit = new RayCastResult()
            //         {
            //             Location = manifold.get
            //         }
            //         a.ProcessHit();
            //     }
            // }
            
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
        private Vector3Wide _gravityAcceleration;
        private Vector<float> _linearDampingDt;
        private Vector<float> _angularDampingDt;
        
        public void Initialize(Simulation simulation)
        {
            
        }

        public void PrepareForIntegration(float dt)
        {
            _linearDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - system.LinearDamping, 0, 1), dt));
            _angularDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - system.AngularDamping, 0, 1), dt));
            _gravityAcceleration = Vector3Wide.Broadcast(system.Gravity.ToVector3());
        }

        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
            BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
        {
            //Console.WriteLine("INTEGRATING");
            var acceleration = _gravityAcceleration;
            var newVelocity = velocity.Linear + acceleration * dt;
            //var newPosition = position + velocity.Linear * dt + acceleration * ( dt * dt * 0.5f);
            
            velocity.Linear = newVelocity;
            
            // Apply damping to velocity
            // velocity.Linear *= _linearDampingDt;
            // velocity.Angular *= _angularDampingDt;
        }

        public AngularIntegrationMode AngularIntegrationMode => system.AngularIntegrationMode;
        public bool AllowSubstepsForUnconstrainedBodies => system.AllowSubstepsForUnconstrainedBodies;
        public bool IntegrateVelocityForKinematics => false;
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

    public Vector3 Gravity { get; set; } = new Vector3(0.0f, -9.8f, 0.0f);

    [PublicAPI]
    public float LinearDamping { get; set; } = 0.00f;
    
    [PublicAPI]
    public float AngularDamping { get; set; } = 0.00f;

    public IPhysicsBox CreateBox(IPhysicsComponent owner, Vector3 size)
    {
        var body = new BepuBox(this,owner,size);
        body.Init();
        lock (_bodies)
        {
            _bodies.Add(owner,body);
        }
        return body;
    }

    public IPhysicsSphere CreateSphere(IPhysicsComponent owner,float radius)
    {
        var body = new BepuSphere(this, owner,radius);
        body.Init();
        lock (_bodies)
        {
            _bodies.Add(owner,body);
        }
        
        return body;
    }

    public IPhysicsCapsule CreateCapsule(IPhysicsComponent owner, float radius, float halfHeight)
    {
        var body = new BepuCapsule(this, owner,radius,halfHeight);
        body.Init();
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
            KeyValuePair<IPhysicsComponent,BepuBody>[] bodies;
            lock (_bodies)
            {
                bodies = _bodies.Where(kv => kv.Value is { IsValid: true, IsSimulating: true }).ToArray();
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
        Simulation.Statics.Add(new StaticDescription(new Vector3(0, -30, 0), Simulation.Shapes.Add(new Box(200, 30, 200))));
    }

    public StaticHandle AddStatic(BepuBody body, StaticDescription description)
    {
        var handle = Simulation.Statics.Add(description);
        _staticBodies.Add(handle,body);
        return handle;
    }

    public BodyHandle AddDynamic(BepuBody body, BodyDescription description)
    {
        var handle = Simulation.Bodies.Add(description);
        _dynamicBodies.Add(handle,body);
        return handle;
    }

    public void RemoveStatic(BepuBody body, StaticHandle handle)
    {
        _staticBodies.Remove(handle);
        Simulation.Statics.Remove(handle);
    }

    public void RemoveDynamic(BepuBody body, BodyHandle handle)
    {
        _dynamicBodies.Remove(handle);
        Simulation.Bodies.Remove(handle);
    }

    public BepuBody GetBodyFromCollidable(CollidableReference reference)
    {
        if (Simulation.Statics[reference.StaticHandle].Exists)
        {
            return _staticBodies[reference.StaticHandle];
        }
        
        if (Simulation.Bodies[reference.BodyHandle].Exists)
        {
            return _dynamicBodies[reference.BodyHandle];
        }
        
        throw new KeyNotFoundException($"The body {reference.StaticHandle} does not exist.");
    }

    class RayHitHandler(BepuPhysics physics,int channel) : IRayHitHandler
    {
        public RayCastResult? Result { get; set; }
        public bool AllowTest(CollidableReference collidable)
        {
            return physics.GetBodyFromCollidable(collidable).CollisionChannel == channel;
        }

        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return AllowTest(collidable);
        }

        public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable,
            int childIndex)
        {
            Result = new RayCastResult
            {
                Location = (ray.Origin + (ray.Direction * t)),
                Distance = t,
                Normal = normal,
                Component = physics.GetBodyFromCollidable(collidable).Owner,
            };
        }
    }

    public RayCastResult? RayCast(Vector3 begin, Vector3 direction, float distance, int channel)
    {
        var hitHandler = new RayHitHandler(this,channel);
        Simulation.RayCast(begin.ToVector3(),direction.ToVector3(),distance,ref hitHandler,channel);
        return hitHandler.Result;
    }
}