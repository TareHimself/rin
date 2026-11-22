using System.Numerics;
using System.Runtime.InteropServices;
using Rin.World.Transforms;

namespace Rin.World.Tests;

public class WorldByteAccessTests
{
    private World _world = null!;
    private Entity _entity;

    [SetUp]
    public void SetUp()
    {
        _world = new World();
        _entity = _world.Spawn();
    }

    private ComponentId IdOf<T>() where T : unmanaged => _world.Schema.IdOf<T>();

    private static byte[] BytesOf<T>(T value) where T : unmanaged => MemoryMarshal.AsBytes([value]).ToArray();

    [Test]
    public void ReadGivesTheComponentsBytes()
    {
        _world.Add(_entity, new Health(7));

        Assert.That(MemoryMarshal.Read<int>(_world.Read(_entity, IdOf<Health>())), Is.EqualTo(7));
    }

    [Test]
    public void WritingBytesChangesTheComponentAndMarksItChanged()
    {
        _world.Add(_entity, new Health(7));
        var before = _world.ChangeTick;
        _world.Update(_world.TickInterval);

        MemoryMarshal.Write(_world.Write(_entity, IdOf<Health>()), 9);

        Assert.That(_world.Get<Health>(_entity).Value, Is.EqualTo(9));
        Assert.That(_world.HasChangedSince<Health>(_entity, before), Is.True);
    }

    [Test]
    public void AddingByIdMovesTheEntityAndFiresAddHooks()
    {
        var added = new List<int>();
        _world.OnAdd((Entity _, in Health health) => added.Add(health.Value));

        _world.Add(_entity, IdOf<Health>(), BytesOf(new Health(3)));

        Assert.That(_world.Get<Health>(_entity).Value, Is.EqualTo(3));
        Assert.That(added, Is.EqualTo(new[] { 3 }));
    }

    [Test]
    public void AddingByIdOverwritesAComponentAlreadyThereWithoutHooks()
    {
        var hooks = 0;
        _world.Add(_entity, new Health(3));
        _world.OnAdd((Entity _, in Health _) => hooks++);

        _world.Add(_entity, IdOf<Health>(), BytesOf(new Health(5)));

        Assert.That(_world.Get<Health>(_entity).Value, Is.EqualTo(5));
        Assert.That(hooks, Is.Zero);
    }

    [Test]
    public void AddDefaultRunsTheStructsFieldInitializers()
    {
        _world.AddDefault(_entity, IdOf<LocalTransform>());

        Assert.That(_world.Get<LocalTransform>(_entity).Scale, Is.EqualTo(Vector3.One));
        Assert.That(_world.Get<LocalTransform>(_entity).Rotation, Is.EqualTo(Quaternion.Identity));
    }

    [Test]
    public void AValueOfTheWrongSizeIsRejected()
    {
        Assert.Throws<ArgumentException>(() => _world.Add(_entity, IdOf<Health>(), new byte[2]));
        Assert.That(_world.Has<Health>(_entity), Is.False);
    }

    [Test]
    public void RemovingByIdFiresRemoveHooks()
    {
        var removed = 0;
        _world.Add(_entity, new Health(3));
        _world.OnRemove((Entity _, in Health _) => removed++);

        Assert.That(_world.Remove(_entity, IdOf<Health>()), Is.True);
        Assert.That(_world.Remove(_entity, IdOf<Health>()), Is.False);
        Assert.That(_world.Has<Health>(_entity), Is.False);
        Assert.That(removed, Is.EqualTo(1));
    }

    [Test]
    public void ComponentsOfListsComponentsAndTagsInIdOrder()
    {
        _world.Add(_entity, new Stunned());
        _world.Add(_entity, new Health(1));

        Assert.That(_world.ComponentsOf(_entity).ToArray(), Is.EqualTo(new[] { IdOf<Health>(), IdOf<Stunned>() }.Order()));
        Assert.That(_world.Has(_entity, IdOf<Stunned>()), Is.True);
        Assert.That(_world.Read(_entity, IdOf<Stunned>()).IsEmpty, Is.True);
    }

    [Test]
    public void ReadingAComponentTheEntityLacksThrows()
    {
        Assert.Throws<InvalidOperationException>(() => _world.Read(_entity, IdOf<Health>()));
    }

    [Test]
    public void AFieldReadsAndWritesItsSliceOfTheComponent()
    {
        var id = IdOf<LocalTransform>();
        _world.Add(_entity, new LocalTransform { Position = new Vector3(1, 2, 3) });
        Assert.That(_world.Schema[id].TryGetField(nameof(LocalTransform.Position), out var position), Is.True);

        position.Write(_world.Write(_entity, id), new Vector3(4, 5, 6));

        Assert.That(position.Read<Vector3>(_world.Read(_entity, id)), Is.EqualTo(new Vector3(4, 5, 6)));
        Assert.That(_world.Get<LocalTransform>(_entity).Position, Is.EqualTo(new Vector3(4, 5, 6)));
        Assert.Throws<ArgumentException>(() => position.Read<float>(_world.Read(_entity, id)));
    }

    [Test]
    public void ComputedTransformsAreMarkedDerived()
    {
        var schema = ComponentSchema.Engine;
        var socket = schema[schema.IdOf<AttachSocket>()];

        Assert.That(schema[schema.IdOf<WorldTransform>()].IsDerived, Is.True);
        Assert.That(schema[schema.IdOf<LocalTransform>()].IsDerived, Is.False);
        Assert.That(socket.TryGetField(nameof(AttachSocket.SocketMatrix), out var matrix) && matrix.IsDerived, Is.True);
        Assert.That(socket.TryGetField(nameof(AttachSocket.BoneIndex), out var bone) && bone.IsDerived, Is.False);
    }
}
