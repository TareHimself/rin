using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Rin.World;
using Rin.World.Editing;
using Rin.World.Physics;
using SharpGLTF.Schema2;

namespace Rin.GLTF;

/// <summary>
///     Reads collider nodes into shape parts of the body that owns them. A collider node carries
///     <see cref="ColliderKey" />; a group of them carries <see cref="GroupKey" />. Neither becomes an entity.
/// </summary>
internal sealed class GltfColliders(List<string> warnings)
{
    public const string ColliderKey = "rin_collider";
    public const string GroupKey = "rin_colliders";

    private const float CentredTolerance = 1e-4f;

    private static readonly FieldType ShapeType = ComponentSchema.Engine.FieldTypeOf(typeof(ColliderShape))!;

    private readonly Dictionary<Node, List<ShapePart>> _partsByOwner = [];

    public static bool IsColliderNode(Node node) =>
        node.Extras is JsonObject extras && (extras.ContainsKey(ColliderKey) || extras.ContainsKey(GroupKey));

    public IReadOnlyCollection<Node> Owners => _partsByOwner.Keys;

    public void Collect(Node owner, Node colliderNode) => Collect(owner, colliderNode, Matrix4x4.Identity);

    /// <summary>
    ///     The owner's shape: its only part when that sits at the owner's origin, otherwise a compound registered with
    ///     <paramref name="shapes" />. Null, with a warning, when a compound is needed and there is no registry.
    /// </summary>
    public ColliderShape? ShapeOf(Node owner, PhysicsShapes? shapes)
    {
        if (!_partsByOwner.TryGetValue(owner, out var parts) || parts.Count == 0) return null;
        if (parts.Count == 1 && IsCentred(parts[0])) return parts[0].Shape;
        if (shapes is null)
        {
            warnings.Add($"Node '{owner.Name}' needs a compound collider, but no PhysicsShapes was given to register it.");
            return null;
        }

        return ColliderShape.CompoundOf(shapes.AddCompound(parts.ToArray()));
    }

    // Poses are relative to the owner, whose own scale PhysicsFeature applies when it builds the body.
    private void Collect(Node owner, Node node, Matrix4x4 parentToOwner)
    {
        var nodeToOwner = node.LocalMatrix * parentToOwner;
        if ((node.Extras as JsonObject)?[ColliderKey] is { } shape) AddPart(owner, node, shape, nodeToOwner);
        foreach (var child in node.VisualChildren) Collect(owner, child, nodeToOwner);
    }

    private void AddPart(Node owner, Node node, JsonNode shapeJson, Matrix4x4 nodeToOwner)
    {
        ColliderShape shape;
        try
        {
            shape = ReadShape(shapeJson);
        }
        catch (InvalidDataException e)
        {
            warnings.Add($"Collider '{node.Name}' was skipped: {e.Message}");
            return;
        }

        Matrix4x4.Decompose(GltfAxes.ToRin(nodeToOwner), out var scale, out var rotation, out var position);
        if (!_partsByOwner.TryGetValue(owner, out var parts)) _partsByOwner[owner] = parts = [];
        parts.Add(new ShapePart(Scaled(shape, scale), position, rotation));
    }

    private static ColliderShape ReadShape(JsonNode json)
    {
        if (json is not JsonObject shape || !shape.ContainsKey(nameof(ColliderShape.Kind)))
            throw new InvalidDataException($"Expected a collider with a Kind, got {json.ToJsonString()}.");

        Span<byte> bytes = stackalloc byte[ShapeType.Size];
        bytes.Clear();
        ComponentJson.ReadStruct(ShapeType, shape, bytes, static _ => null);
        var read = MemoryMarshal.Read<ColliderShape>(bytes);
        return read.Kind == ColliderShapeKind.Compound
            ? throw new InvalidDataException("A collider node cannot be a compound; nest collider nodes instead.")
            : read;
    }

    private static ColliderShape Scaled(in ColliderShape shape, Vector3 scale) => shape.Kind switch
    {
        ColliderShapeKind.Box => ColliderShape.Box(shape.Size * scale),
        ColliderShapeKind.Sphere => ColliderShape.Sphere(shape.Radius * float.Max(scale.X, float.Max(scale.Y, scale.Z))),
        _ => ColliderShape.Capsule(shape.Radius * float.Max(scale.X, scale.Z), shape.HalfHeight * scale.Y)
    };

    private static bool IsCentred(in ShapePart part) =>
        part.Position.Length() < CentredTolerance && MathF.Abs(MathF.Abs(part.Rotation.W) - 1f) < CentredTolerance;
}
