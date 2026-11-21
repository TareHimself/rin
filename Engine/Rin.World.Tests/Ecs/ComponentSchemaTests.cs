using System.Runtime.CompilerServices;
using Rin.World.Graphics;
using Rin.World.Transforms;

namespace Rin.World.Tests.Ecs;

public class ComponentSchemaTests
{
    [Test]
    public void IdOfGivesTheSameIdEveryTime()
    {
        var schema = new ComponentSchema();

        var first = schema.IdOf<Health>();
        schema.IdOf<Speed>();

        Assert.That(schema.IdOf<Health>(), Is.EqualTo(first));
        Assert.That(schema.Count, Is.EqualTo(2));
    }

    [Test]
    public void ACloneKeepsEveryIdOfItsSource()
    {
        var source = new ComponentSchema();
        var health = source.IdOf<Health>();
        var speed = source.IdOf<Speed>();

        var clone = source.Clone();

        Assert.That(clone.IdOf<Health>(), Is.EqualTo(health));
        Assert.That(clone.IdOf<Speed>(), Is.EqualTo(speed));
    }

    [Test]
    public void TypesAddedToACloneStayOutOfItsSource()
    {
        var source = new ComponentSchema();
        source.IdOf<Health>();
        var clone = source.Clone();

        clone.IdOf<Speed>();

        Assert.That(source.TryGetId<Speed>(out _), Is.False);
        Assert.That(source.Count, Is.EqualTo(1));
    }

    [Test]
    public void TwoClonesOfTheEngineSchemaGiveEngineComponentsTheSameIds()
    {
        var first = ComponentSchema.Engine.Clone();
        var second = ComponentSchema.Engine.Clone();
        first.IdOf<Health>();

        Assert.That(second.IdOf<LocalTransform>(), Is.EqualTo(first.IdOf<LocalTransform>()));
        Assert.That(second.IdOf<RenderProxy>(), Is.EqualTo(ComponentSchema.Engine.IdOf<RenderProxy>()));
    }

    [Test]
    public void AFrozenSchemaRefusesNewTypes()
    {
        var schema = new ComponentSchema();
        schema.IdOf<Health>();
        schema.Freeze();

        Assert.That(schema.IdOf<Health>().Value, Is.Zero);
        Assert.Throws<InvalidOperationException>(() => schema.IdOf<Speed>());
    }

    [Test]
    public void TheEngineSchemaIsFrozenAndHoldsTheEngineModuleInOrder()
    {
        var schema = ComponentSchema.Engine;
        var module = ComponentModule.Engine.Components;

        Assert.That(schema.IsFrozen, Is.True);
        Assert.That(schema.Count, Is.EqualTo(module.Length));
        for (var i = 0; i < module.Length; i++) Assert.That(schema[new ComponentId(i)], Is.SameAs(module[i]));
    }

    [Test]
    public void TryGetIdFindsAModuleTypeThatWasNeverAskedForByIdOf()
    {
        var schema = ComponentSchema.Engine.Clone();

        Assert.That(schema.TryGetId<Camera>(out var camera), Is.True);
        Assert.That(schema[camera].Type, Is.EqualTo(typeof(Camera)));
    }

    [Test]
    public void ATagWithFieldsIsRejected()
    {
        var schema = new ComponentSchema();

        Assert.Throws<ArgumentException>(() => ComponentInfo.Of<TagWithField>());
        Assert.Throws<ArgumentException>(() => schema.IdOf<TagWithField>());
        Assert.That(schema.Count, Is.Zero);
    }

    [Test]
    public void AnEmptyTagHasNoSizeAlthoughTheStructReportsOne()
    {
        Assert.That(Unsafe.SizeOf<Stunned>(), Is.EqualTo(1));
        Assert.That(ComponentInfo.Of<Stunned>().IsTag, Is.True);
        Assert.That(ComponentInfo.Of<Stunned>().Size, Is.Zero);
    }

    [Test]
    public void TagsAreZeroSizeAndNotSavedComponentsAreMarked()
    {
        var schema = ComponentSchema.Engine.Clone();

        Assert.That(schema[schema.IdOf<Stunned>()].IsTag, Is.True);
        Assert.That(schema[schema.IdOf<Health>()].Size, Is.EqualTo(sizeof(int)));
        Assert.That(schema[schema.IdOf<RenderProxy>()].IsSaved, Is.False);
        Assert.That(schema[schema.IdOf<LocalTransform>()].IsSaved, Is.True);
    }

    [Test]
    public void TwoWorldsSharingASchemaGiveANewTypeOneId()
    {
        var schema = ComponentSchema.Engine.Clone();
        var first = new World(schema);
        var second = new World(schema);

        first.Add(first.Spawn(), new Health(1));
        second.Add(second.Spawn(), new Speed(1f));
        second.Add(second.Spawn(), new Health(2));

        Assert.That(first.Schema, Is.SameAs(second.Schema));
        Assert.That(schema.Count, Is.EqualTo(ComponentSchema.Engine.Count + 2));
        Assert.That(first.Query<Health>().EntityCount, Is.EqualTo(1), "worlds share ids, not entities");
    }

    [Test]
    public void AWorldOnAFrozenSchemaRejectsTypesTheSchemaLacks()
    {
        var schema = ComponentSchema.Engine.Clone();
        schema.IdOf<Health>();
        schema.Freeze();
        var world = new World(schema);
        var entity = world.Spawn();

        world.Add(entity, new Health(1));

        Assert.Throws<InvalidOperationException>(() => world.Add(entity, new Speed(1f)));
        Assert.That(world.Has<Speed>(entity), Is.False);
    }

    private struct TagWithField : ITagData
    {
        public int Value { get; set; }
    }
}
