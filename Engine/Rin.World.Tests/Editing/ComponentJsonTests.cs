using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Rin.Core.Views;
using Rin.World.Editing;
using Rin.World.Graphics;
using Rin.World.Physics;
using Rin.World.Transforms;

namespace Rin.World.Tests.Editing;

public class ComponentJsonTests
{
    private static readonly ComponentSchema Schema =
        ComponentSchema.Compose(ComponentModule.Engine, ComponentModule.Of(typeof(Follow).Assembly)!);

    private static ComponentInfo InfoOf<T>() where T : unmanaged => Schema[Schema.IdOf<T>()];

    private static T Read<T>(JsonObject values, Func<string, Entity?>? findEntity = null) where T : unmanaged
    {
        var info = InfoOf<T>();
        var bytes = info.DefaultValue.ToArray();
        ComponentJson.ReadFields(info, values, bytes, findEntity ?? (_ => null));
        return MemoryMarshal.Read<T>(bytes);
    }

    [Test]
    public void OnlySavedUnderivedComponentsWithAnEditableFieldAreEditable()
    {
        Assert.That(ComponentJson.IsEditable(InfoOf<PointLight>()), Is.True);
        Assert.That(ComponentJson.IsEditable(InfoOf<WorldTransform>()), Is.False, "derived");
        Assert.That(ComponentJson.IsEditable(InfoOf<StaticMesh>()), Is.False, "only process-local fields");
        Assert.That(ComponentJson.IsEditable(InfoOf<RenderProxy>()), Is.False, "not saved");
    }

    [Test]
    public void TheSchemaListsEditableFieldsWithTheirDefaults()
    {
        using var stream = new MemoryStream();
        ComponentJson.WriteSchema(Schema, stream);

        var components = JsonNode.Parse(stream.ToArray())!["components"]!.AsArray();
        var light = components.Single(c => c!["name"]!.GetValue<string>() == typeof(PointLight).FullName)!;
        var socket = components.Single(c => c!["name"]!.GetValue<string>() == typeof(AttachSocket).FullName)!;

        Assert.That(light["fields"]!.AsArray().Select(f => f!["name"]!.GetValue<string>()), Is.EqualTo(new[] { "Color", "Lumens", "Radius" }));
        Assert.That(light["defaults"]!["Color"]!.ToJsonString(), Is.EqualTo("[1,1,1,1]"));
        Assert.That(light["defaults"]!["Lumens"]!.GetValue<float>(), Is.EqualTo(800f));
        Assert.That(socket["fields"]!.AsArray().Select(f => f!["name"]!.GetValue<string>()), Is.EqualTo(new[] { "BoneIndex" }));
        Assert.That(components.Any(c => c!["name"]!.GetValue<string>() == typeof(StaticMesh).FullName), Is.False);
    }

    [Test]
    public void ReadingFieldsKeepsTheDefaultsOfFieldsItDoesNotName()
    {
        var light = Read<PointLight>(new JsonObject { ["Lumens"] = 100 });

        Assert.That(light.Lumens, Is.EqualTo(100f));
        Assert.That(light.Color, Is.EqualTo(Color.White));
    }

    [Test]
    public void AStructOfNumbersReadsFromAShorterArrayInFieldOrder()
    {
        var light = Read<PointLight>(new JsonObject { ["Color"] = new JsonArray(0.5f, 0.25f, 0f) });

        Assert.That(light.Color, Is.EqualTo(new Color(0.5f, 0.25f, 0f, 1f)));
    }

    [Test]
    public void EnumsReadByNameAndNestedStructsByField()
    {
        var body = Read<RigidBody>(new JsonObject
        {
            ["State"] = "controlled",
            ["Shape"] = new JsonObject { ["Kind"] = "Sphere", ["Radius"] = 2 }
        });

        Assert.That(body.State, Is.EqualTo(PhysicsState.Controlled));
        Assert.That(body.Shape, Is.EqualTo(ColliderShape.Sphere(2f)));
    }

    [Test]
    public void ProcessLocalAndDerivedFieldsAreNeverRead()
    {
        var socket = Read<AttachSocket>(new JsonObject
        {
            ["BoneIndex"] = 3,
            ["SocketMatrix"] = new JsonArray(Enumerable.Range(0, 16).Select(_ => (JsonNode?)2).ToArray())
        });

        Assert.That(socket.BoneIndex, Is.EqualTo(3));
        Assert.That(socket.SocketMatrix.M11, Is.Not.EqualTo(2f));
    }

    [Test]
    public void EntityFieldsResolveByName()
    {
        var target = new World().Spawn();
        var info = InfoOf<Follow>();
        var bytes = info.DefaultValue.ToArray();

        ComponentJson.ReadFields(info, new JsonObject { ["Target"] = "Leader" }, bytes, name => name == "Leader" ? target : null);

        Assert.That(MemoryMarshal.Read<Follow>(bytes).Target, Is.EqualTo(target));
        Assert.Throws<InvalidDataException>(() =>
            ComponentJson.ReadFields(info, new JsonObject { ["Target"] = "Nobody" }, bytes, _ => null));
    }

    [Test]
    public void AValueOfTheWrongShapeThrows()
    {
        Assert.Throws<InvalidDataException>(() => Read<PointLight>(new JsonObject { ["Lumens"] = "bright" }));
        Assert.Throws<InvalidDataException>(() => Read<LocalTransform>(new JsonObject { ["Position"] = new JsonArray(1, 2) }));
    }
}
