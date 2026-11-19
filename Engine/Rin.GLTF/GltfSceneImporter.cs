using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Rin.Core.Views;
using Rin.World;
using Rin.World.Graphics;
using Rin.World.Transforms;
using SharpGLTF.Schema2;
using Camera = Rin.World.Graphics.Camera;
using GltfCamera = SharpGLTF.Schema2.Camera;

namespace Rin.GLTF;

/// <summary>
///     Spawns a glTF scene into a world: one entity per node, with its static meshes, punctual lights and cameras.
///     Materials come from the caller. Node extras <see cref="LumensKey" /> and <see cref="LuxKey" /> override a light's
///     brightness, and scene extra <see cref="StartCameraKey" /> names the camera node to start from.
/// </summary>
public sealed class GltfSceneImporter
{
    public const string LumensKey = "rin_lumens";
    public const string LuxKey = "rin_lux";
    public const string StartCameraKey = "rin_start_camera";

    // A point light without a glTF range reaches as far as it still gives this much light.
    private const float RadiusCutoffLux = 0.01f;

    private static readonly Quaternion HalfTurn = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);

    private readonly RenderFeature _render;
    private readonly Func<Material?, IMeshMaterial?> _resolveMaterial;
    private readonly Dictionary<int, StaticMesh> _meshes = [];
    private readonly Dictionary<int, IMeshMaterial?> _materials = [];
    private readonly List<Entity> _entities = [];
    private readonly List<(string? Name, Entity Entity)> _cameras = [];
    private readonly List<string> _warnings = [];

    private GltfSceneImporter(RenderFeature render, Func<Material?, IMeshMaterial?> resolveMaterial)
    {
        _render = render;
        _resolveMaterial = resolveMaterial;
    }

    private Rin.World.World World => _render.World;

    public static ImportedScene Load(string path, RenderFeature render, Func<Material?, IMeshMaterial?> resolveMaterial) =>
        Load(ModelRoot.Load(path), render, resolveMaterial);

    public static ImportedScene Load(ModelRoot model, RenderFeature render,
        Func<Material?, IMeshMaterial?> resolveMaterial)
    {
        var importer = new GltfSceneImporter(render, resolveMaterial);
        if (model.DefaultScene is not { } scene) return new ImportedScene([], null, ["The file has no scene."]);

        foreach (var node in scene.VisualChildren) importer.SpawnNode(node, null);
        var startCamera = importer.FindStartCamera(ReadString(scene.Extras, StartCameraKey));
        return new ImportedScene(importer._entities, startCamera, importer._warnings);
    }

    private void SpawnNode(Node node, Entity? parent)
    {
        var entity = World.Spawn();
        _entities.Add(entity);
        if (parent is { } parentEntity) World.SetParent(entity, parentEntity);
        World.SetLocalTransform(entity, LocalTransformOf(node));

        if (node.Mesh is { } mesh) AddMesh(entity, node, mesh);
        if (node.PunctualLight is not null || node.Camera is not null)
        {
            var viewer = ViewerEntity(entity, node);
            if (node.PunctualLight is { } light) AddLight(viewer, node, light);
            if (node.Camera is { } camera) AddCamera(viewer, node, camera);
        }

        foreach (var child in node.VisualChildren) SpawnNode(child, entity);
    }

    private static LocalTransform LocalTransformOf(Node node)
    {
        Matrix4x4.Decompose(GltfAxes.ToRin(node.LocalMatrix), out var scale, out var rotation, out var position);
        return new LocalTransform { Position = position, Rotation = rotation, Scale = scale };
    }

    // glTF cameras and lights look down -Z, so they get a half turn to look down Rin's forward. It goes on the node's
    // own entity unless that would also turn its mesh or children.
    private Entity ViewerEntity(Entity entity, Node node)
    {
        if (node.Mesh is null && !node.VisualChildren.Any())
        {
            ref var transform = ref World.Get<LocalTransform>(entity);
            transform.Rotation *= HalfTurn;
            return entity;
        }

        var viewer = World.Spawn();
        _entities.Add(viewer);
        World.SetParent(viewer, entity);
        World.SetLocalTransform(viewer, new LocalTransform { Rotation = HalfTurn });
        return viewer;
    }

    private void AddMesh(Entity entity, Node node, Mesh mesh)
    {
        if (node.Skin is not null)
        {
            _warnings.Add($"Node '{node.Name}' is skinned, and skinned meshes are not imported yet.");
            return;
        }

        if (!_meshes.TryGetValue(mesh.LogicalIndex, out var staticMesh))
        {
            staticMesh = new StaticMesh
            {
                Mesh = GltfMeshImporter.CreateStaticMesh(mesh),
                Materials = _render.Materials.Add(ResolveMaterials(mesh))
            };
            _meshes[mesh.LogicalIndex] = staticMesh;
        }

        World.Add(entity, staticMesh);
    }

    private IMeshMaterial?[] ResolveMaterials(Mesh mesh)
    {
        var materials = new IMeshMaterial?[mesh.Primitives.Count];
        for (var i = 0; i < materials.Length; i++) materials[i] = ResolveMaterial(mesh.Primitives[i].Material);
        return materials;
    }

    private IMeshMaterial? ResolveMaterial(Material? material)
    {
        var key = material?.LogicalIndex ?? -1;
        if (!_materials.TryGetValue(key, out var resolved))
        {
            resolved = _resolveMaterial(material);
            _materials[key] = resolved;
        }

        return resolved;
    }

    private void AddLight(Entity entity, Node node, PunctualLight light)
    {
        var color = Color.FromRgb(light.Color.X, light.Color.Y, light.Color.Z);
        switch (light.LightType)
        {
            case PunctualLightType.Directional:
                World.Add(entity, new DirectionalLight { Color = color, Lux = ReadFloat(node.Extras, LuxKey) ?? light.Intensity });
                return;
            case PunctualLightType.Spot:
                _warnings.Add($"Spot light '{node.Name}' is imported as a point light.");
                break;
        }

        var lumens = ReadFloat(node.Extras, LumensKey) ?? light.Intensity * 4f * MathF.PI;
        World.Add(entity, new PointLight { Color = color, Lumens = lumens, Radius = RadiusOf(light, lumens) });
    }

    private static float RadiusOf(PunctualLight light, float lumens)
    {
        if (light.Range > 0f && float.IsFinite(light.Range)) return light.Range;
        var candela = lumens / (4f * MathF.PI);
        return MathF.Sqrt(candela / RadiusCutoffLux);
    }

    private void AddCamera(Entity entity, Node node, GltfCamera camera)
    {
        if (camera.Settings is not CameraPerspective perspective)
        {
            _warnings.Add($"Camera '{node.Name}' is orthographic, which is not supported.");
            return;
        }

        var settings = new Camera
        {
            FieldOfView = float.RadiansToDegrees(perspective.VerticalFOV),
            NearClipPlane = perspective.ZNear
        };
        if (perspective.ZFar > 0f && float.IsFinite(perspective.ZFar)) settings.FarClipPlane = perspective.ZFar;

        World.Add(entity, settings);
        _cameras.Add((node.Name, entity));
    }

    private Entity? FindStartCamera(string? name)
    {
        if (_cameras.Count == 0) return null;
        if (name is null) return _cameras[0].Entity;

        foreach (var camera in _cameras)
            if (camera.Name == name)
                return camera.Entity;

        _warnings.Add($"Start camera '{name}' was not found, so the first camera is used.");
        return _cameras[0].Entity;
    }

    // Parsed from text so a value built in code as an int reads the same as one loaded from a file.
    private static float? ReadFloat(JsonNode? extras, string key) =>
        (extras as JsonObject)?[key] is JsonValue { } value && value.GetValueKind() == JsonValueKind.Number &&
        float.TryParse(value.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;

    private static string? ReadString(JsonNode? extras, string key) =>
        (extras as JsonObject)?[key] is JsonValue value && value.TryGetValue(out string? text) ? text : null;
}
