using Examples.Editor.Model;
using Rin.World;
using Rin.World.Graphics;
using Rin.World.Transforms;

namespace Examples.Tests.Editor.Model;

public class FieldRowsTests
{
    private static List<FieldRow> RowsOf<T>() where T : unmanaged =>
        FieldRows.Of(ComponentSchema.Engine[ComponentSchema.Engine.IdOf<T>()]);

    [Test]
    public void VectorsBecomeOneRowWithALanePerAxisAndRotationsGetTheirOwnKind()
    {
        var rows = RowsOf<LocalTransform>();

        Assert.That(rows.Select(r => (r.Label, r.Kind)), Is.EqualTo(new[]
        {
            ("Position", FieldRowKind.Number),
            ("Rotation", FieldRowKind.Rotation),
            ("Scale", FieldRowKind.Number)
        }));
        Assert.That(rows[0].Lanes.Select(l => (l.Label, l.Offset)), Is.EqualTo(new[] { ("X", 0), ("Y", 4), ("Z", 8) }));
    }

    [Test]
    public void ANestedStructIsAGroupFollowedByItsFieldsOneLevelDeeperAtOffsetsFromTheComponent()
    {
        var info = ComponentModule.Of(typeof(Sensor).Assembly)!.Components.ToArray().Single(c => c.Type == typeof(Sensor));
        var rows = FieldRows.Of(info);
        var range = rows.Single(r => r.Label == "Range");
        var reach = rows.Single(r => r.Label == "Reach");

        Assert.That(range.Kind, Is.EqualTo(FieldRowKind.Group));
        Assert.That(rows.Skip(rows.IndexOf(range) + 1).TakeWhile(r => r.Depth == 1).Select(r => (r.Label, r.Kind)),
            Is.EqualTo(new[] { ("Mode", FieldRowKind.Enum), ("Reach", FieldRowKind.Number), ("IsOn", FieldRowKind.Bool) }));
        Assert.That(reach.Lanes[0].Offset, Is.EqualTo(range.Offset + range.Type.Fields.Single(f => f.Name == "Reach").Offset));
    }

    [Test]
    public void AStructOfAFewNumbersBecomesOneRowWithALanePerField()
    {
        var color = RowsOf<PointLight>().Single(r => r.Label == "Color");

        Assert.That(color.Kind, Is.EqualTo(FieldRowKind.Number));
        Assert.That(color.Lanes.Select(l => l.Label), Is.EqualTo(new[] { "R", "G", "B", "A" }));
    }

    [Test]
    public void EveryRowOfADerivedComponentIsReadOnly()
    {
        var rows = RowsOf<WorldTransform>();

        Assert.That(rows, Is.Not.Empty);
        Assert.That(rows.All(r => r.IsReadOnly), Is.True);
        Assert.That(RowsOf<LocalTransform>().Any(r => r.IsReadOnly), Is.False);
    }
}
