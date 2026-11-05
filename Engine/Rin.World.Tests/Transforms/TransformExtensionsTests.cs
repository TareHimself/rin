using System.Numerics;
using Rin.World.Transforms;

namespace Rin.World.Tests.Transforms;

public class TransformExtensionsTests
{
    [Test]
    public void ComputeWorldMatrixMatchesPropagationBeforeItRuns()
    {
        var world = new World();
        var parent = world.Spawn();
        var child = world.Spawn();
        world.SetLocalTransform(parent, new LocalTransform
        {
            Position = new Vector3(10, 0, 0),
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2),
            Scale = new Vector3(2)
        });
        world.SetPosition(child, new Vector3(0, 0, 1));
        world.SetParent(child, parent);

        var computed = world.ComputeWorldMatrix(child);
        world.Update(0f);

        Assert.That(computed, Is.EqualTo(world.Get<WorldTransform>(child).Matrix));
    }

    [Test]
    public void ComputeWorldMatrixIsIdentityWithoutALocalTransform()
    {
        var world = new World();

        Assert.That(world.ComputeWorldMatrix(world.Spawn()), Is.EqualTo(Matrix4x4.Identity));
    }
}
