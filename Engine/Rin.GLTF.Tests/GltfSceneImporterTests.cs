using System.Numerics;
using System.Text.Json.Nodes;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.Graphics.Null;
using Rin.World;
using Rin.World.Graphics;
using Rin.World.Graphics.Mesh;
using Rin.World.Transforms;
using SharpGLTF.Schema2;
using Camera = Rin.World.Graphics.Camera;

namespace Rin.GLTF.Tests;

public class GltfSceneImporterTests
{
    private const float Tolerance = 1e-4f;

    private static string CubePath => Path.Combine(AppContext.BaseDirectory, "Assets", "cube.glb");
    private static string FoxPath => Path.Combine(AppContext.BaseDirectory, "Assets", "fox.glb");

    // Exported by the rin_blender add-on, see Assets/make_blender_level.py.
    private static string BlenderLevelPath => Path.Combine(AppContext.BaseDirectory, "Assets", "blender_level.glb");

    private Rin.World.World _world = null!;
    private RenderFeature _render = null!;
    private ModelRoot _model = null!;
    private Scene _scene = null!;
    private int _resolvedMaterials;

    [OneTimeSetUp]
    public void RegisterNullGraphicsModule()
    {
        Global.Provider.AddSingle<IGraphicsModule>(new NullGraphicsModule());
        Global.Provider.AddSingle<IMeshFactory>(new MeshFactory());
    }

    [SetUp]
    public void CreateWorldAndEmptyScene()
    {
        _world = new Rin.World.World();
        _render = new RenderFeature(_world, new NullRenderSystem(), IMeshFactory.Get());
        _model = ModelRoot.Load(CubePath);
        _scene = _model.UseScene("Test");
        _model.DefaultScene = _scene;
        _resolvedMaterials = 0;
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
    public void APointLightConvertsItsCandelaToLumens()
    {
        AddLight("Lamp", PunctualLightType.Point, intensity: 10f).Range = 5f;

        var light = _world.Get<PointLight>(Import().Entities[0]);

        Assert.That(light.Lumens, Is.EqualTo(10f * 4f * MathF.PI).Within(Tolerance));
        Assert.That(light.Radius, Is.EqualTo(5f));
    }

    [Test]
    public void APointLightWithoutARangeReachesUntilItsLightFallsBelowACutoff()
    {
        AddLight("Lamp", PunctualLightType.Point, intensity: 4f);

        var light = _world.Get<PointLight>(Import().Entities[0]);

        Assert.That(light.Radius, Is.EqualTo(MathF.Sqrt(4f / 0.01f)).Within(Tolerance));
    }

    [Test]
    public void LumensAndLuxInNodeExtrasOverrideTheGltfIntensity()
    {
        AddLight("Lamp", PunctualLightType.Point, intensity: 1f);
        AddLight("Sun", PunctualLightType.Directional, intensity: 1f);
        _scene.VisualChildren.First().Extras = new JsonObject { [GltfSceneImporter.LumensKey] = 800 };
        _scene.VisualChildren.Last().Extras = new JsonObject { [GltfSceneImporter.LuxKey] = 100000 };

        var imported = Import();

        Assert.That(_world.Get<PointLight>(imported.Entities[0]).Lumens, Is.EqualTo(800f));
        Assert.That(_world.Get<DirectionalLight>(imported.Entities[1]).Lux, Is.EqualTo(100000f));
    }

    [Test]
    public void ASpotLightIsImportedAsAPointLightWithAWarning()
    {
        AddLight("Spot", PunctualLightType.Spot, intensity: 1f);

        var imported = Import();

        Assert.That(_world.Has<PointLight>(imported.Entities[0]), Is.True);
        Assert.That(imported.Warnings, Has.One.Contains("Spot"));
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
        var imported = GltfSceneImporter.Load(BlenderLevelPath, _render, _ => null);
        _world.Update(0f);

        var lamp = imported.Entities.Single(_world.Has<PointLight>);
        var sun = imported.Entities.Single(_world.Has<DirectionalLight>);
        Assert.That(_world.Get<PointLight>(lamp).Lumens, Is.EqualTo(800f).Within(0.01f));
        Assert.That(_world.Get<DirectionalLight>(sun).Lux, Is.EqualTo(100000f).Within(1f));

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

    private ImportedScene Import() => GltfSceneImporter.Load(_model, _render, _ =>
    {
        _resolvedMaterials++;
        return null;
    });

    private void CreateWorldForReimport()
    {
        _render.RenderSystem.Dispose();
        _world = new Rin.World.World();
        _render = new RenderFeature(_world, new NullRenderSystem(), IMeshFactory.Get());
    }

    private PunctualLight AddLight(string name, PunctualLightType type, float intensity)
    {
        var light = _model.CreatePunctualLight(type);
        light.Intensity = intensity;
        _scene.CreateNode(name).PunctualLight = light;
        return light;
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
