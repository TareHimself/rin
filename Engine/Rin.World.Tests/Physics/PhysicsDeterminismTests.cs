using System.Numerics;
using Rin.World.Physics;
using Rin.World.Transforms;
using Rin.World.Physics.Bepu;

namespace Rin.World.Tests.Physics;

public class PhysicsDeterminismTests
{
    private const int BoxCount = 24;
    private const int Ticks = 240;

    [Test]
    public void TwoIdenticalWorldsSteppedTheSameTicksEndInIdenticalPoses()
    {
        var first = RunScene();
        var second = RunScene();

        Assert.That(second, Is.EqualTo(first));
        Assert.That(first[0].Position.Y, Is.LessThan(20f), "the boxes should have fallen");
    }

    private static (Vector3 Position, Quaternion Rotation)[] RunScene()
    {
        var world = new World();
        var physics = new BepuPhysicsSystem();
        try
        {
            _ = new PhysicsFeature(world, physics);
            var floor = world.Spawn();
            world.SetPosition(floor, Vector3.Zero);
            world.Add(floor, new RigidBody { Shape = ColliderShape.Box(new Vector3(40, 1, 40)), State = PhysicsState.Static });

            var boxes = new Entity[BoxCount];
            for (var i = 0; i < BoxCount; i++)
            {
                boxes[i] = world.Spawn();
                world.SetPosition(boxes[i], new Vector3(i % 3 * 0.7f, 20f + i * 1.5f, i % 4 * 0.6f));
                world.SetRotation(boxes[i], Quaternion.CreateFromAxisAngle(Vector3.UnitX, i * 0.3f));
                world.Add(boxes[i], new RigidBody { Shape = ColliderShape.Box(Vector3.One), State = PhysicsState.Simulated });
            }

            for (var tick = 0; tick < Ticks; tick++) world.Update(world.TickInterval);

            var poses = new (Vector3, Quaternion)[BoxCount];
            for (var i = 0; i < BoxCount; i++)
            {
                var local = world.Get<LocalTransform>(boxes[i]);
                poses[i] = (local.Position, local.Rotation);
            }

            return poses;
        }
        finally
        {
            physics.Destroy();
        }
    }
}
