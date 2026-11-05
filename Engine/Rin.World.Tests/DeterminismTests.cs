using System.Numerics;
using Rin.World.Graphics;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;
using Rin.World.Scheduling;
using Rin.World.Scripting;
using Rin.World.Tasks;
using Rin.World.Transforms;

namespace Rin.World.Tests;

/// <summary>
///     Runs a scripted input log through worlds with systems, scripts, tasks, rendering and Bepu physics, and compares
///     snapshot hashes tick by tick.
/// </summary>
public class DeterminismTests
{
    private const int SaveTick = 60;
    private const int CompareTick = 150;

    [Test]
    public void TheSameInputLogGivesTheSameSnapshotEveryTick()
    {
        using var first = new Scenario(physics: true, events: true);
        using var second = new Scenario(physics: true, events: true);

        var firstHashes = first.Run(CompareTick * 2);
        var secondHashes = second.Run(CompareTick * 2);

        Assert.That(secondHashes, Is.EqualTo(firstHashes));
        Assert.That(firstHashes.Distinct().Count(), Is.EqualTo(firstHashes.Count), "every tick should change the state");
        Assert.That(first.World.EntityCount, Is.GreaterThan(20));
        Assert.That(first.ContactsSeen, Is.GreaterThan(0), "the bodies should have landed");
    }

    [Test]
    public void TwoWorldsThatRollBackTheSameWayStayIdentical()
    {
        using var first = new Scenario(physics: true, events: true);
        using var second = new Scenario(physics: true, events: true);

        Assert.That(RunWithRollback(second), Is.EqualTo(RunWithRollback(first)));
    }

    [Test]
    public void WithoutPhysicsARolledBackWorldMatchesOneThatNeverRestored()
    {
        using var rolledBack = new Scenario(physics: false, events: false);
        using var straight = new Scenario(physics: false, events: false);

        var replayed = RunWithRollback(rolledBack);
        var original = straight.Run(CompareTick).GetRange(SaveTick, CompareTick - SaveTick);

        Assert.That(replayed, Is.EqualTo(original));
    }

    private static List<ulong> RunWithRollback(Scenario scenario)
    {
        scenario.Run(SaveTick);
        var saved = new WorldSnapshot();
        scenario.World.SaveSnapshot(saved);
        scenario.Run(CompareTick - SaveTick);

        scenario.World.RestoreSnapshot(saved);
        Assert.That(scenario.World.TickIndex, Is.EqualTo(SaveTick));
        return scenario.Run(CompareTick - SaveTick);
    }

    private sealed class Scenario : IDisposable
    {
        private readonly WorldSnapshot _snapshot = new();
        private readonly BepuPhysicsSystem? _bepu;
        private readonly PhysicsFeature? _physics;

        public Scenario(bool physics, bool events)
        {
            World.Random.Seed(1234);
            var meshes = new FakeMeshFactory();
            var meshId = meshes.Register(new FakeMesh());
            _ = new RenderFeature(World, new FakeRenderSystem(), meshes);
            var scripts = new ScriptFeature(World);
            if (physics)
            {
                _bepu = new BepuPhysicsSystem();
                _physics = new PhysicsFeature(World, _bepu);
                var floor = World.Spawn();
                World.SetPosition(floor, new Vector3(0, -0.5f, 0));
                World.Add(floor, new RigidBody { Shape = ColliderShape.Box(new Vector3(100, 1, 100)), State = PhysicsState.Static });
            }

            World.AddSystem(TickPhase.Input, new InputPlayback(meshId, physics));
            World.AddSystem(TickPhase.Gameplay, new Wander());
            if (_physics is not null) World.AddSystem(TickPhase.Gameplay, new ContactCounter(_physics, this));

            var director = World.Spawn();
            World.Add(director, new DirectorState { NextTick = 3 });
            scripts.Attach(director, new Director(events));
        }

        public World World { get; } = new();

        public int ContactsSeen { get; set; }

        public List<ulong> Run(int ticks)
        {
            var hashes = new List<ulong>(ticks);
            for (var i = 0; i < ticks; i++)
            {
                World.Update(World.TickInterval);
                World.SaveSnapshot(_snapshot);
                hashes.Add(_snapshot.ComputeHash());
            }

            return hashes;
        }

        public void Dispose() => _bepu?.Destroy();
    }

    internal struct Wanderer : ITagData;

    internal struct DirectorState : IComponentData
    {
        public long NextTick;
        public int Fired;
        public int Heard;
    }

    private readonly record struct Ping(int Value);

    // The scripted input log: what a player or the network would feed in, keyed only by tick.
    private sealed class InputPlayback(int meshId, bool physics) : TickSystem
    {
        public override void Tick(in TickContext context)
        {
            var world = context.World;
            if (context.Tick % 4 == 0) SpawnWanderer(world, context.Random, context.Tick);
            if (context.Tick % 9 == 0) DespawnFirstWanderer(world);
        }

        private void SpawnWanderer(World world, SimRandom random, long tick)
        {
            var entity = world.Commands.Spawn();
            var local = new LocalTransform { Position = new Vector3(tick % 7 - 3, 2 + random.NextFloat() * 4, random.NextFloat(-2, 2)) };
            world.Commands.Add(entity, local);
            world.Commands.Add(entity, new WorldTransform());
            world.Commands.Add(entity, new Speed(random.NextFloat(0.5f, 2f)));
            world.Commands.AddTag<Wanderer>(entity);
            world.Commands.Add(entity, new StaticMesh { MeshId = meshId });
            if (physics) world.Commands.Add(entity, new RigidBody { Shape = ColliderShape.Sphere(0.3f), State = PhysicsState.Simulated });
        }

        private static void DespawnFirstWanderer(World world)
        {
            foreach (var chunk in world.Query<Speed>())
                if (chunk.Length > 0)
                {
                    world.Commands.Despawn(chunk.Entities[0]);
                    return;
                }
        }
    }

    private sealed class Wander : TickSystem
    {
        public override void Tick(in TickContext context)
        {
            foreach (var (locals, speeds) in context.World.Query<LocalTransform, Speed>())
                for (var i = 0; i < locals.Length; i++)
                {
                    ref var local = ref locals[i];
                    local.Rotation = Quaternion.Normalize(local.Rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitY, speeds[i].Value * context.DeltaSeconds));
                    local.Position += new Vector3(context.Random.NextFloat(-1, 1) * speeds[i].Value * context.DeltaSeconds, 0, 0);
                }
        }
    }

    private sealed class ContactCounter(PhysicsFeature physics, Scenario scenario) : TickSystem
    {
        public override void Tick(in TickContext context)
        {
            foreach (ref readonly var contact in physics.Contacts)
            {
                scenario.ContactsSeen++;
                if (contact.A.IsValid && context.World.IsAlive(contact.A) && context.World.Has<Speed>(contact.A))
                    context.World.Get<Speed>(contact.A).Value *= 0.9f;
            }
        }
    }

    // Keeps its schedule in DirectorState, so Restore only restarts the tasks.
    private sealed class Director(bool events) : Script
    {
        protected override void Start() => StartTasks();

        protected override void Restore() => StartTasks();

        private void StartTasks()
        {
            Tasks.Run(Spawn);
            if (events) Tasks.Run(Listen);
        }

        private async GameTask Spawn(TaskContext ctx)
        {
            while (true)
            {
                await ctx.Until(() => World.TickIndex >= World.Get<DirectorState>(Entity).NextTick);
                ref var state = ref World.Get<DirectorState>(Entity);
                state.Fired++;
                state.NextTick += World.Random.NextInt(3, 9);

                var child = World.Spawn();
                World.Add(child, new Health(state.Fired));
                World.SetParent(child, Entity);
                if (events) ctx.Raise(Entity, new Ping(state.Fired));
            }
        }

        private async GameTask Listen(TaskContext ctx)
        {
            while (true)
            {
                var ping = await ctx.Event<Ping>(Entity);
                World.Get<DirectorState>(Entity).Heard += ping.Value;
            }
        }
    }
}
