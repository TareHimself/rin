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
        Assert.Throws<InvalidOperationException>(() => schema.Add(new ComponentInfo("Door", null, 4, true, [])));
    }

    [Test]
    public void TheEngineSchemaIsFrozenAndHoldsEveryRinWorldComponentAndTag()
    {
        var tryGetId = typeof(ComponentSchema).GetMethod(nameof(ComponentSchema.TryGetId))!;
        var missing = new List<string>();
        foreach (var type in typeof(World).Assembly.GetTypes())
        {
            if (!type.IsValueType || !(typeof(IComponentData).IsAssignableFrom(type) || typeof(ITagData).IsAssignableFrom(type))) continue;
            object?[] arguments = [null];
            if (!(bool)tryGetId.MakeGenericMethod(type).Invoke(ComponentSchema.Engine, arguments)!) missing.Add(type.FullName!);
        }

        Assert.That(ComponentSchema.Engine.IsFrozen, Is.True);
        Assert.That(missing, Is.Empty, "add them to ComponentSchema.CreateEngine");
    }

    [Test]
    public void AddRejectsACSharpStructWhateverItsSize()
    {
        var schema = new ComponentSchema();

        Assert.Throws<ArgumentException>(() => schema.Add(ComponentInfo.Of<Health>()));
        Assert.Throws<ArgumentException>(() => schema.Add(new ComponentInfo(nameof(Health), typeof(Health), 1, true, [])));
        Assert.That(schema.Count, Is.Zero);
    }

    [Test]
    public void AStructThatIsBothAComponentAndATagIsRejected()
    {
        var schema = new ComponentSchema();

        Assert.Throws<ArgumentException>(() => ComponentInfo.Of<ComponentAndTag>());
        Assert.Throws<ArgumentException>(() => schema.IdOf<ComponentAndTag>());
        Assert.That(schema.Count, Is.Zero);
    }

    [Test]
    public void ARunTimeComponentGetsTheNextId()
    {
        var schema = new ComponentSchema();
        schema.IdOf<Health>();

        var id = schema.Add(new ComponentInfo("Door", null, 8, true, [new ComponentField("Open", typeof(int), 0)]));

        Assert.That(id.Value, Is.EqualTo(1));
        Assert.That(schema[id].Name, Is.EqualTo("Door"));
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

    private struct ComponentAndTag : IComponentData, ITagData;
}
