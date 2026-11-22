using System.Numerics;
using System.Text.Json.Nodes;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.Graphics.Null;
using Rin.World;
using Rin.World.Editing;
using Rin.World.Graphics;
using Rin.World.Graphics.ImageBasedLighting;
using Rin.World.Graphics.Mesh;
using Rin.World.Physics;
using Rin.World.Transforms;
using SharpGLTF.Schema2;
using Camera = Rin.World.Graphics.Camera;

namespace Rin.GLTF.Tests;

public class GltfSceneImporterTests
{
    private const float Tolerance = 1e-4f;
    private const string DoorName = "Rin.GLTF.Tests.TestDoor";

    private static string CubePath => Path.Combine(AppContext.BaseDirectory, "Assets", "cube.glb");
    private static string FoxPath => Path.Combine(AppContext.BaseDirectory, "Assets", "fox.glb");

    // Exported by the Rin Blender add-on, see Assets/make_blender_level.py.
    private static string BlenderLevelPath => Path.Combine(AppContext.BaseDirectory, "Assets", "blender_level");

    // The schema the add-on built blender_level.glb's components from.
    private static readonly ComponentSchema Schema =
        ComponentSchema.Compose(ComponentModule.Engine, ComponentModule.Of(typeof(TestDoor).Assembly)!);

    private static string EditorSchemaPath => Path.Combine(AppContext.BaseDirectory, "Assets", "editor.schema.json");

    private const string RigidBodyName = "Rin.World.Physics.RigidBody";
    private const string BoxColliderName = "Rin.World.Physics.BoxCollider";
    private const string EnvironmentLightName = "Rin.World.Graphics.ImageBasedLighting.EnvironmentLight";

    private Rin.World.World _world = null!;
    private RenderFeature _render = null!;
    private ModelRoot _model = null!;
    private Scene _scene = null!;
    private int _resolvedMaterials;
    private PhysicsShapes _shapes = null!;

    [OneTimeSetUp]
    public void RegisterNullGraphicsModule()
    {
        Global.Provider.AddSingle<IGraphicsModule>(new NullGraphicsModule());
        Global.Provider.AddSingle<IMeshFactory>(new MeshFactory());
    }

    [SetUp]
    public void CreateWorldAndEmptyScene()
    {
        _world = new Rin.World.World(Schema);
        _render = new RenderFeature(_world, new NullRenderSystem(), IMeshFactory.Get());
        _model = ModelRoot.Load(CubePath);
        _scene = _model.UseScene("Test");
        _model.DefaultScene = _scene;
        _resolvedMaterials = 0;
        _shapes = new PhysicsShapes();
    }

    [TearDown]
    public void DisposeRenderFeature() => _render.RenderSystem.Dispose();

    [Test]
    public void EachNodeBecomesAnEntityUnderItsParentMirroredIntoRinSpace()
    {
        var parent = _scene.CreateNode("Parent");
        parent.LocalMatrix = Matrix4x4.CreateTranslation(1f, 2f, 3f);
        parent.CreateNode("Child");

        var imported = Import();

        Assert.That(imported.Entities, Has.Count.EqualTo(2));
        var (parentEntity, childEntity) = (imported.Entities[0], imported.Entities[1]);
        Assert.That(_world.GetParent(childEntity), Is.EqualTo(parentEntity));
        Assert.That(_world.Get<LocalTransform>(parentEntity).Position, Is.EqualTo(new Vector3(-1f, 2f, 3f)));
    }

    [Test]
    public void NodesSharingAMeshShareItsHandleAndMaterials()
    {
        var mesh = _model.LogicalMeshes[0];
        _scene.CreateNode("A").Mesh = mesh;
        _scene.CreateNode("B").Mesh = mesh;

        var imported = Import();

        var a = _world.Get<StaticMesh>(imported.Entities[0]);
        var b = _world.Get<StaticMesh>(imported.Entities[1]);
        Assert.That((b.Mesh, b.Materials), Is.EqualTo((a.Mesh, a.Materials)));
        Assert.That(_resolvedMaterials, Is.EqualTo(mesh.Primitives.Select(p => p.Material).Distinct().Count()));
    }

    [Test]
    public void AGltfLightIsSkippedWithAWarning()
    {
        var light = _model.CreatePunctualLight(PunctualLightType.Point);
        _scene.CreateNode("Lamp").PunctualLight = light;

        var imported = Import();

        Assert.That(_world.Has<PointLight>(imported.Entities[0]), Is.False);
        Assert.That(imported.Warnings, Has.One.Contains("Lamp"));
    }

    [Test]
    public void ACameraLooksTheSameWayInRinAsInTheAuthoringTool()
    {
        // Turned to look down glTF's -X, which mirrors to Rin's +X.
        var node = AddCamera("Camera");
        node.LocalTransform = new SharpGLTF.Transforms.AffineTransform(null,
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f), null);

        var imported = Import();
        _world.Update(0f);

        var camera = imported.StartCamera!.Value;
        var forward = Vector3.TransformNormal(MathR.Forward, _world.Get<WorldTransform>(camera).Matrix);
        Assert.That(Vector3.Distance(forward, Vector3.UnitX), Is.LessThan(Tolerance));
        Assert.That(_world.Get<Camera>(camera).FieldOfView, Is.EqualTo(60f).Within(Tolerance));
    }

    [Test]
    public void ACameraNodeWithChildrenKeepsItsTurnOffTheChildren()
    {
        AddCamera("Camera").CreateNode("Child");

        var imported = Import();

        var camera = imported.StartCamera!.Value;
        Assert.That(_world.GetParent(camera), Is.EqualTo(imported.Entities[0]));
        Assert.That(_world.Get<LocalTransform>(imported.Entities[0]).Rotation, Is.EqualTo(Quaternion.Identity));
    }

    [Test]
    public void TheStartCameraIsTheOneTheSceneNamesOrElseTheFirst()
    {
        AddCamera("First");
        AddCamera("Second");

        var first = Import().StartCamera;
        _scene.Extras = new JsonObject { [GltfSceneImporter.StartCameraKey] = "Second" };
        CreateWorldForReimport();
        var named = Import();

        Assert.That(first, Is.Not.Null);
        Assert.That(named.StartCamera, Is.EqualTo(named.Entities[1]));
    }

    [Test]
    public void ASkinnedNodeIsSkippedWithAWarning()
    {
        var imported = GltfSceneImporter.Load(FoxPath, _render, _ => null);

        Assert.That(imported.Warnings, Has.Some.Contains("skinned"));
        Assert.That(imported.Entities.Any(_world.Has<StaticMesh>), Is.False);
    }

    [Test]
    public void ABlenderLevelKeepsItsBrightnessAndWhatTheStartCameraSawOnItsRight()
    {
        var imported = LoadBlenderLevel();
        _world.Update(0f);

        var lamp = imported.Entities.Single(_world.Has<PointLight>);
        var sun = imported.Entities.Single(_world.Has<DirectionalLight>);
        Assert.That((_world.Get<PointLight>(lamp).Lumens, _world.Get<PointLight>(lamp).Radius), Is.EqualTo((800f, 0f)));
        Assert.That(_world.Get<DirectionalLight>(sun).Lux, Is.EqualTo(100000f));
        var sunForward = Vector3.TransformNormal(MathR.Forward, _world.Get<WorldTransform>(sun).Matrix);
        Assert.That(Vector3.Distance(sunForward, -Vector3.UnitY), Is.LessThan(Tolerance), "the sun shines straight down");
        Assert.That(ModelRoot.Load(Path.Combine(BlenderLevelPath, GltfSceneImporter.LevelFileName)).LogicalNodes.Select(n => n.Name),
            Has.None.EndsWith("preview"), "the add-on's preview lights stay in Blender");
        Assert.That(imported.Warnings, Is.Empty);

        var camera = _world.Get<WorldTransform>(imported.StartCamera!.Value).Matrix;
        var cubes = imported.Entities.Where(_world.Has<StaticMesh>).ToArray();
        var toCube = _world.Get<WorldTransform>(cubes[0]).Matrix.Translation - camera.Translation;
        Assert.That(camera.Translation.Z, Is.EqualTo(10f).Within(Tolerance), "Overview, not the first camera");
        Assert.That(Vector3.Dot(toCube, Vector3.TransformNormal(MathR.Forward, camera)), Is.GreaterThan(0f));
        Assert.That(Vector3.Dot(toCube, Vector3.TransformNormal(MathR.Right, camera)), Is.GreaterThan(0f),
            "the cube sat right of the camera in Blender");
        Assert.That(_world.Get<StaticMesh>(cubes[1]).Mesh, Is.EqualTo(_world.Get<StaticMesh>(cubes[0]).Mesh),
            "linked duplicates share one mesh");
    }

    [Test]
    public void ABlenderLevelKeepsTheComponentsAddedInTheAddOn()
    {
        var imported = LoadBlenderLevel();

        var cubes = imported.Entities.Where(_world.Has<StaticMesh>).ToArray();
        var door = _world.Get<TestDoor>(cubes[0]);
        Assert.That((door.OpenSeconds, door.IsLocked, door.Key), Is.EqualTo((2.5f, true, cubes[1])));
        Assert.That(imported.Warnings, Is.Empty);
    }

    [Test]
    public void ABlenderLevelTurnsRigidBodySettingsIntoRigidBodies()
    {
        var imported = LoadBlenderLevel();

        var meshes = imported.Entities.Where(_world.Has<StaticMesh>).ToArray();
        var (cube, twin, tower) = (_world.Get<RigidBody>(meshes[0]), _world.Get<RigidBody>(meshes[1]),
            _world.Get<RigidBody>(meshes[2]));
        Assert.That((ShapeOf(meshes[0]), cube.State, cube.Mass), Is.EqualTo((ColliderShape.Box(new Vector3(2f)), PhysicsState.Simulated, 2f)));
        Assert.That((ShapeOf(meshes[1]), twin.State, twin.IsTrigger, twin.CollisionChannel),
            Is.EqualTo((ColliderShape.Sphere(1f), PhysicsState.Controlled, true, 3)));
        var capsule = _world.Get<CapsuleCollider>(meshes[2]);
        Assert.That(tower.State, Is.EqualTo(PhysicsState.Static));
        Assert.That(capsule.Radius, Is.EqualTo(0.5f).Within(Tolerance));
        Assert.That(capsule.HalfHeight, Is.EqualTo(1f).Within(Tolerance), "a 3 m capsule minus its two caps");
    }

    [Test]
    public void ABlenderLevelFoldsItsCollidersIntoTheirRigidBodies()
    {
        var imported = LoadBlenderLevel();

        var meshes = imported.Entities.Where(_world.Has<StaticMesh>).ToArray();
        Assert.That(meshes, Has.Length.EqualTo(5), "collider nodes spawn no entities");
        Assert.That(Vector3.Distance(_world.Get<BoxCollider>(meshes[3]).Size, new Vector3(1f, 0.5f, 2f)), Is.LessThan(Tolerance),
            "one centred box stays a box collider, with Blender's Z as Rin's Y");

        var parts = _shapes.PartsOf(_world.Get<CompoundCollider>(meshes[4]).Parts).ToArray();
        Assert.That(parts, Has.Length.EqualTo(5));
        Assert.That(parts.Any(p => Near(p.Position, new Vector3(0f, 0.9f, 0f)) && Near(p.Shape.Size, new Vector3(1.2f, 0.1f, 0.8f))),
            Is.True, "the top");
        Assert.That(parts.Any(p => Near(p.Position, new Vector3(0.5f, 0.45f, 0.3f))), Is.True,
            "Blender's (-0.5, -0.3) leg mirrors to Rin's +X");
    }

    [Test]
    public void CollidersUnderANodeBecomeOneCompoundRigidBody()
    {
        AddBodyWithColliders(new Vector3(2f, 0f, 0f), new Vector3(-2f, 0f, 0f));

        var imported = Import();

        Assert.That(imported.Entities, Has.Count.EqualTo(1));
        var parts = _shapes.PartsOf(_world.Get<CompoundCollider>(imported.Entities[0]).Parts).ToArray();
        Assert.That(parts.Select(p => p.Position.X).Order(), Is.EqualTo(new[] { -2f, 2f }));
    }

    [Test]
    public void ASingleCentredColliderBecomesAnInlineShape()
    {
        AddBodyWithColliders(Vector3.Zero);

        var imported = Import();

        Assert.That(ShapeOf(imported.Entities[0]), Is.EqualTo(ColliderShape.Box(Vector3.One)));
    }

    [Test]
    public void CollidersWithoutARigidBodyAreIgnoredWithAWarning()
    {
        var body = AddBodyWithColliders(Vector3.Zero);
        body.Extras = null;

        var imported = Import();

        Assert.That(_world.Has<RigidBody>(imported.Entities[0]), Is.False);
        Assert.That(imported.Warnings, Has.One.Contains("no RigidBody"));
    }

    [Test]
    public void ACompoundNeedsSomewhereToRegisterItsParts()
    {
        AddBodyWithColliders(new Vector3(2f, 0f, 0f), new Vector3(-2f, 0f, 0f));

        var imported = GltfSceneImporter.Load(_model, _render, _ => null);

        Assert.That(ShapeOf(imported.Entities[0]), Is.EqualTo(ColliderShape.Box(new Vector3(4f))),
            "keeps the exported box");
        Assert.That(imported.Warnings, Has.One.Contains("PhysicsShapes"));
    }

    [Test]
    public void ABlenderLevelLoadsItsEnvironmentLightsFromItsFolder()
    {
        var imported = LoadBlenderLevel();

        var lights = imported.Entities.Where(_world.Has<EnvironmentLight>).Select(e => _world.Get<EnvironmentLight>(e)).ToArray();
        var sky = lights.Single(l => l.Extent == Vector3.Zero);
        var room = lights.Single(l => l.Extent != Vector3.Zero);
        Assert.That((sky.Intensity, room.Extent, room.BlendDistance), Is.EqualTo((1.2f, new Vector3(3f), 0.5f)));
        Assert.That(_render.Environments.Get(sky.Map), Is.Not.Null);
        Assert.That(room.Map, Is.EqualTo(sky.Map), "one file is loaded once");
        Assert.That(imported.Warnings, Is.Empty);
    }

    [Test]
    public void AnEnvironmentLightWhoseMapIsMissingIsSkippedWithAWarning()
    {
        _scene.CreateNode("Sky").Extras = new JsonObject
        {
            [GltfSceneImporter.ComponentsKey] = new JsonObject
            {
                [EnvironmentLightName] = new JsonObject { ["Map"] = "environments/missing.hdr" }
            }
        };

        var imported = GltfSceneImporter.Load(_model, _render, _ => null, _shapes, AppContext.BaseDirectory);

        Assert.That(_world.Has<EnvironmentLight>(imported.Entities[0]), Is.False);
        Assert.That(imported.Warnings, Has.One.Contains("missing.hdr"));
    }

    [Test]
    public void TheSchemaTheBlenderLevelWasBuiltFromMatchesTheComposedSchema()
    {
        using var written = new MemoryStream();
        ComponentJson.WriteSchema(Schema, written);

        var expected = JsonNode.Parse(File.ReadAllText(EditorSchemaPath));
        if (JsonNode.DeepEquals(JsonNode.Parse(written.ToArray()), expected)) return;

        var actualPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "editor.actual.schema.json");
        File.WriteAllBytes(actualPath, written.ToArray());
        Assert.Fail($"Copy {actualPath} over Assets/editor.schema.json and rerun make_blender_level.py.");
    }

    [Test]
    public void ComponentsInNodeExtrasAreAddedWithEntityFieldsResolvedByNodeName()
    {
        var door = _scene.CreateNode("Door");
        door.Extras = new JsonObject
        {
            [GltfSceneImporter.ComponentsKey] = new JsonObject
            {
                [DoorName] = new JsonObject { ["IsLocked"] = true, ["Key"] = "Key" }
            }
        };
        _scene.CreateNode("Key");

        var imported = Import();

        var doorEntity = imported.Entities[0];
        var component = _world.Get<TestDoor>(doorEntity);
        Assert.That((component.OpenSeconds, component.IsLocked, component.Key), Is.EqualTo((1f, true, imported.Entities[1])));
        Assert.That(imported.Warnings, Is.Empty);
    }

    [Test]
    public void UnknownAndMalformedComponentsAreSkippedWithAWarning()
    {
        _scene.CreateNode("Door").Extras = new JsonObject
        {
            [GltfSceneImporter.ComponentsKey] = new JsonObject
            {
                ["Game.Missing"] = new JsonObject(),
                [DoorName] = new JsonObject { ["Key"] = "Nowhere" }
            }
        };

        var imported = Import();

        Assert.That(_world.Has<TestDoor>(imported.Entities[0]), Is.False);
        Assert.That(imported.Warnings, Has.Count.EqualTo(2));
        Assert.That(imported.Warnings, Has.Some.Contains("Game.Missing"));
        Assert.That(imported.Warnings, Has.Some.Contains("Nowhere"));
    }

    private ImportedScene Import() => GltfSceneImporter.Load(_model, _render, _ =>
    {
        _resolvedMaterials++;
        return null;
    }, _shapes);

    private ImportedScene LoadBlenderLevel() => GltfSceneImporter.Load(BlenderLevelPath, _render, _ => null, _shapes);

    private ColliderShape ShapeOf(Entity entity) =>
        Colliders.TryGetShape(_world, entity, out var shape) ? shape : throw new AssertionException("no collider");

    private Node AddBodyWithColliders(params Vector3[] colliderPositions)
    {
        var body = _scene.CreateNode("Body");
        body.Extras = new JsonObject
        {
            [GltfSceneImporter.ComponentsKey] = new JsonObject
            {
                [RigidBodyName] = new JsonObject(),
                [BoxColliderName] = new JsonObject { ["Size"] = new JsonArray(4, 4, 4) }
            }
        };
        var group = body.CreateNode("Colliders");
        group.Extras = new JsonObject { ["rin_colliders"] = true };
        foreach (var position in colliderPositions)
        {
            var collider = group.CreateNode("Collider");
            collider.LocalMatrix = Matrix4x4.CreateTranslation(-position.X, position.Y, position.Z);
            collider.Extras = new JsonObject
            {
                ["rin_collider"] = new JsonObject { ["Kind"] = "Box", ["Size"] = new JsonArray(1, 1, 1) }
            };
        }

        return body;
    }

    private static bool Near(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < Tolerance;

    private void CreateWorldForReimport()
    {
        _render.RenderSystem.Dispose();
        _world = new Rin.World.World(Schema);
        _render = new RenderFeature(_world, new NullRenderSystem(), IMeshFactory.Get());
    }


    private Node AddCamera(string name)
    {
        var camera = _model.CreateCamera(name);
        camera.SetPerspectiveMode(null, float.DegreesToRadians(60f), 0.1f, 500f);
        var node = _scene.CreateNode(name);
        node.Camera = camera;
        return node;
    }
}
