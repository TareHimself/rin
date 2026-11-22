using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Rin.World;
using Rin.World.Editing;
using Rin.World.Graphics;
using Rin.World.Graphics.ImageBasedLighting;
using Rin.World.Physics;
using Rin.World.Transforms;
using SharpGLTF.Schema2;
using Camera = Rin.World.Graphics.Camera;
using GltfCamera = SharpGLTF.Schema2.Camera;

namespace Rin.GLTF;

/// <summary>
///     Spawns a glTF scene into a world: one entity per node, with its static meshes and cameras. Materials come from
///     the caller. Node extra <see cref="ComponentsKey" /> adds components by full type name, with their fields as
///     <see cref="ComponentJson" /> reads them, so the world's schema must hold the game's components. Collider
///     nodes under a node become its <see cref="RigidBody" />'s shape, an <see cref="EnvironmentLight" />'s <c>Map</c>
///     names a .hdr file in the level's folder, and scene extra <see cref="StartCameraKey" /> names the camera node to
///     start from. A level folder holds its glTF as <see cref="LevelFileName" />.
/// </summary>
public sealed class GltfSceneImporter
{
    public const string StartCameraKey = "rin_start_camera";
    public const string ComponentsKey = "rin_components";
    public const string LevelFileName = "level.glb";
    private const string MapField = "Map";

    private static readonly Quaternion HalfTurn = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);

    private readonly RenderFeature _render;
    private readonly Func<Material?, IMeshMaterial?> _resolveMaterial;
    private readonly Dictionary<int, StaticMesh> _meshes = [];
    private readonly Dictionary<int, IMeshMaterial?> _materials = [];
    private readonly List<Entity> _entities = [];
    private readonly List<(string? Name, Entity Entity)> _cameras = [];
    private readonly Dictionary<string, Entity> _entitiesByNodeName = [];
    private readonly Dictionary<Entity, string> _names = [];
    private readonly List<(Node Node, JsonObject Components, Entity Entity)> _pendingComponents = [];
    private readonly List<string> _warnings = [];
    private readonly PhysicsShapes? _shapes;
    private readonly GltfColliders _colliders;
    private readonly GltfEnvironments _environments;
    private readonly Dictionary<Node, Entity> _bodies = [];

    private GltfSceneImporter(RenderFeature render, Func<Material?, IMeshMaterial?> resolveMaterial, PhysicsShapes? shapes,
        string? levelDirectory)
    {
        _render = render;
        _resolveMaterial = resolveMaterial;
        _shapes = shapes;
        _colliders = new GltfColliders(_warnings);
        _environments = new GltfEnvironments(render.Environments, levelDirectory, _warnings);
    }

    private Rin.World.World World => _render.World;

    /// <param name="path">A level folder, or a .glb or .gltf file.</param>
    /// <param name="shapes">Where compound colliders are registered, usually <see cref="PhysicsFeature.Shapes" />.</param>
    public static ImportedScene Load(string path, RenderFeature render, Func<Material?, IMeshMaterial?> resolveMaterial,
        PhysicsShapes? shapes = null)
    {
        var file = Directory.Exists(path) ? Path.Combine(path, LevelFileName) : path;
        return Load(ModelRoot.Load(file), render, resolveMaterial, shapes, Path.GetDirectoryName(Path.GetFullPath(file)));
    }

    /// <param name="shapes">Where compound colliders are registered, usually <see cref="PhysicsFeature.Shapes" />.</param>
    /// <param name="levelDirectory">Where files the level names, such as environment maps, are found.</param>
    public static ImportedScene Load(ModelRoot model, RenderFeature render,
        Func<Material?, IMeshMaterial?> resolveMaterial, PhysicsShapes? shapes = null, string? levelDirectory = null)
    {
        var importer = new GltfSceneImporter(render, resolveMaterial, shapes, levelDirectory);
        if (model.DefaultScene is not { } scene) return new ImportedScene([], null, ["The file has no scene."], new Dictionary<Entity, string>());

        foreach (var node in scene.VisualChildren) importer.SpawnNode(node, null);
        importer.AddPendingComponents();
        importer.AddColliders();
        importer.WarnAboutCollidersWithoutBodies();
        var startCamera = importer.FindStartCamera(ReadString(scene.Extras, StartCameraKey));
        return new ImportedScene(importer._entities, startCamera, importer._warnings, importer._names);
    }

    private void SpawnNode(Node node, Entity? parent)
    {
        var entity = World.Spawn();
        _entities.Add(entity);
        if (parent is { } parentEntity) World.SetParent(entity, parentEntity);
        World.SetLocalTransform(entity, LocalTransformOf(node));
        if (node.Name is { } name)
        {
            _entitiesByNodeName.TryAdd(name, entity);
            _names[entity] = name;
        }
        if ((node.Extras as JsonObject)?[ComponentsKey] is JsonObject components)
            _pendingComponents.Add((node, components, entity));

        if (node.Mesh is { } mesh) AddMesh(entity, node, mesh);
        if (node.Camera is { } camera) AddCamera(ViewerEntity(entity, node), node, camera);
        if (node.PunctualLight is not null)
            _warnings.Add($"Node '{node.Name}' has a glTF light, which is not imported. Give it a light component instead.");

        foreach (var child in node.VisualChildren)
        {
            if (GltfColliders.IsColliderNode(child)) _colliders.Collect(node, child);
            else SpawnNode(child, entity);
        }
    }

    private static LocalTransform LocalTransformOf(Node node)
    {
        Matrix4x4.Decompose(GltfAxes.ToRin(node.LocalMatrix), out var scale, out var rotation, out var position);
        return new LocalTransform { Position = position, Rotation = rotation, Scale = scale };
    }

    // glTF cameras look down -Z, so they get a half turn to look down Rin's forward. It goes on the node's own entity
    // unless that would also turn its mesh or children.
    private Entity ViewerEntity(Entity entity, Node node)
    {
        if (node.Mesh is null && !node.VisualChildren.Any())
        {
            ref var transform = ref World.Write<LocalTransform>(entity);
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

    // Runs once every node exists, so Entity fields can name nodes anywhere in the scene.
    private void AddPendingComponents()
    {
        foreach (var (node, components, entity) in _pendingComponents)
        foreach (var (name, fields) in components)
            AddComponent(node, entity, name, fields);
    }

    private void AddComponent(Node node, Entity entity, string name, JsonNode? fields)
    {
        if (!World.Schema.TryGetId(name, out var id) || !ComponentJson.IsEditable(World.Schema[id]))
        {
            _warnings.Add($"Node '{node.Name}' has unknown component '{name}'.");
            return;
        }

        if (fields is not JsonObject values)
        {
            _warnings.Add($"Component '{name}' on node '{node.Name}' is not an object.");
            return;
        }

        var info = World.Schema[id];
        var component = info.DefaultValue.ToArray();
        try
        {
            ComponentJson.ReadFields(info, values, component, FindEntity);
        }
        catch (InvalidDataException e)
        {
            _warnings.Add($"Component '{name}' on node '{node.Name}' was skipped: {e.Message}");
            return;
        }

        if (!TryResolveProcessLocalFields(node, info, values, component)) return;
        World.Add(entity, id, component);
        if (info.Type == typeof(RigidBody)) _bodies[node] = entity;
    }

    // Collider nodes replace whatever collider the node's own components gave it, like the add-on's bounding box.
    private void AddColliders()
    {
        foreach (var (node, entity) in _bodies)
            if (_colliders.ShapeOf(node, _shapes) is { } shape)
                Colliders.Set(World, entity, shape);
    }

    // Fields that only mean something in this process are not in the saved fields, so they are found here: an
    // environment light's map from the file its Map names.
    private bool TryResolveProcessLocalFields(Node node, ComponentInfo info, JsonObject values, byte[] component)
    {
        if (info.Type != typeof(EnvironmentLight)) return true;
        if (_environments.Load(node.Name ?? "", ReadString(values, MapField)) is not { } map) return false;
        MemoryMarshal.AsRef<EnvironmentLight>(component.AsSpan()).Map = map;
        return true;
    }

    private void WarnAboutCollidersWithoutBodies()
    {
        foreach (var owner in _colliders.Owners)
            if (!_bodies.ContainsKey(owner))
                _warnings.Add($"Node '{owner.Name}' has colliders but no RigidBody, so they are ignored.");
    }

    private Entity? FindEntity(string nodeName) =>
        _entitiesByNodeName.TryGetValue(nodeName, out var entity) ? entity : null;

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

    private static string? ReadString(JsonNode? extras, string key) =>
        (extras as JsonObject)?[key] is JsonValue value && value.TryGetValue(out string? text) ? text : null;
}
