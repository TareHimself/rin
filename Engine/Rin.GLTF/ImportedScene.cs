using Rin.World;

namespace Rin.GLTF;

/// <summary>
///     What <see cref="GltfSceneImporter" /> spawned, and what it skipped as warnings.
/// </summary>
public sealed record ImportedScene(IReadOnlyList<Entity> Entities, Entity? StartCamera, IReadOnlyList<string> Warnings);
