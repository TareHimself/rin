using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using JetBrains.Annotations;
using Rin.Engine.Math;

namespace Rin.Engine.World.Physics.Bepu;

public class BepuPhysicsSystem : IPhysicsSystem
{
    private readonly BufferPool _pool = new();

    public readonly HashSet<BepuBody> Bodies = [];
    private Vector3 _gravity = new(0, -9.81f, 0);

    public BepuPhysicsSystem()
    {
        Simulation = Simulation.Create(_pool, new NarrowPhaseCallbacks(this), new PoseIntegratorCallbacks(this),
            new SolveDescription(VelocityIterationCount, SubstepCount));
    }

    public Simulation Simulation { get; set; }

    [PublicAPI] public int VelocityIterationCount { get; private set; } = 8;

    [PublicAPI] public int SubstepCount { get; private set; } = 8;

    [PublicAPI]
    public AngularIntegrationMode AngularIntegrationMode { get; set; } = AngularIntegrationMode.ConserveMomentum;

    [PublicAPI] public bool AllowSubstepsForUnconstrainedBodies { get; set; } = true;

    [PublicAPI] public float LinearDamping { get; set; }

    [PublicAPI] public float AngularDamping { get; set; }

    public Vector3 GetGravity()
    {
        return _gravity;
    }

    public void SetGravity(in Vector3 gravity)
    {
        _gravity = gravity;
    }

    public IPhysicsBox CreateBox(in Vector3 size, in Transform transform, PhysicsState state)
    {
        var body = new BepuBoxBody(state, transform, this, size);
        body.Init();
        Bodies.Add(body);
        return body;
    }

    public IPhysicsSphere CreateSphere(float radius, in Transform transform, PhysicsState state)
    {
        var body = new BepuSphereBody(state, transform, this, radius);
        body.Init();
        Bodies.Add(body);
        return body;
    }

    public IPhysicsCapsule CreateCapsule(float radius, float halfHeight, in Transform transform, PhysicsState state)
    {
        var body = new BepuCapsuleBody(state, transform, this, radius, halfHeight);
        body.Init();
        Bodies.Add(body);
        return body;
    }

    public void Update(float deltaTime)
    {
        Simulation.Timestep(deltaTime);
    }

    public void Destroy()
    {
        foreach (var body in Bodies) body.Dispose();
    }

    public RayCastResult? RayCast(in Vector3 begin, in Vector3 direction, float distance, int channel)
    {
        throw new NotImplementedException();
    }

    private struct NarrowPhaseCallbacks(BepuPhysicsSystem physicsSystem) : INarrowPhaseCallbacks
    {
        public void Initialize(Simulation simulation)
        {
        }

        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b,
            ref float speculativeMargin)
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
            pairMaterial = new PairMaterialProperties
                { FrictionCoefficient = 1, MaximumRecoveryVelocity = 2, SpringSettings = new SpringSettings(35, 5) };
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

    private struct PoseIntegratorCallbacks(BepuPhysicsSystem physicsSystem) : IPoseIntegratorCallbacks
    {
        private Vector3Wide _gravityAcceleration;
        private Vector<float> _linearDampingDt;
        private Vector<float> _angularDampingDt;

        public void Initialize(Simulation simulation)
        {
        }

        public void PrepareForIntegration(float dt)
        {
            _linearDampingDt =
                new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - physicsSystem.LinearDamping, 0, 1), dt));
            _angularDampingDt =
                new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - physicsSystem.AngularDamping, 0, 1), dt));
            _gravityAcceleration = Vector3Wide.Broadcast(physicsSystem.GetGravity());
        }

        public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
            BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt,
            ref BodyVelocityWide velocity)
        {
            var acceleration = _gravityAcceleration;
            var newVelocity = velocity.Linear + acceleration * dt;
            velocity.Linear = newVelocity;

            // Apply damping to velocity
            // velocity.Linear *= _linearDampingDt;
            // velocity.Angular *= _angularDampingDt;
        }

        public AngularIntegrationMode AngularIntegrationMode => physicsSystem.AngularIntegrationMode;
        public bool AllowSubstepsForUnconstrainedBodies => physicsSystem.AllowSubstepsForUnconstrainedBodies;
        public bool IntegrateVelocityForKinematics => false;
    }
}