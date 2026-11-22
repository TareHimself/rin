using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.GLTF;

/// <summary>
///     Loads the Radiance (.hdr) files a level's environment lights name, relative to the level's folder, once each.
/// </summary>
internal sealed class GltfEnvironments(EnvironmentMaps maps, string? levelDirectory, List<string> warnings)
{
    private readonly Dictionary<string, EnvironmentMapHandle> _loaded = new(StringComparer.OrdinalIgnoreCase);

    public EnvironmentMapHandle? Load(string nodeName, string? relativePath)
    {
        if (relativePath is null)
        {
            warnings.Add($"The environment light on node '{nodeName}' names no map.");
            return null;
        }

        if (levelDirectory is null)
        {
            warnings.Add($"Map '{relativePath}' on node '{nodeName}' cannot be found: the level was not loaded from a file.");
            return null;
        }

        var path = Path.GetFullPath(Path.Combine(levelDirectory, relativePath));
        if (_loaded.TryGetValue(path, out var handle)) return handle;
        if (!File.Exists(path))
        {
            warnings.Add($"Map '{relativePath}' on node '{nodeName}' is missing from the level folder.");
            return null;
        }

        try
        {
            handle = maps.Create(EnvironmentMap.FromRadiance(File.ReadAllBytes(path)));
        }
        catch (InvalidDataException e)
        {
            warnings.Add($"Map '{relativePath}' on node '{nodeName}' was skipped: {e.Message}");
            return null;
        }

        _loaded[path] = handle;
        return handle;
    }
}
