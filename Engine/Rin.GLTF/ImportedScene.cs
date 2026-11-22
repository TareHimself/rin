using Rin.World;

namespace Rin.GLTF;

/// <summary>
///     What <see cref="GltfSceneImporter" /> spawned, and what it skipped as warnings.
/// </summary>
/// <param name="Names">Each named node's entity and its node name, which the scene keeps rather than the ECS.</param>
public sealed record ImportedScene(
    IReadOnlyList<Entity> Entities,
    Entity? StartCamera,
    IReadOnlyList<string> Warnings,
    IReadOnlyDictionary<Entity, string> Names);
