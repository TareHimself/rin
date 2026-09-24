using System.Formats.Tar;
using System.Text.Json;

namespace Rin.Slang;

public static class ShaderPackageReader
{
    public static CompiledShader Read(Stream input)
    {
        using var tar = new TarReader(input, leaveOpen: true);

        ShaderManifest? manifest = null;
        var spirvByPath = new Dictionary<string, byte[]>();

        TarEntry? entry;
        while ((entry = tar.GetNextEntry()) != null)
        {
            if (entry.DataStream is null) continue;
            using var buffer = new MemoryStream();
            entry.DataStream.CopyTo(buffer);
            var bytes = buffer.ToArray();

            if (entry.Name == "manifest.json")
                manifest = JsonSerializer.Deserialize(bytes, ShaderManifestJsonContext.Default.ShaderManifest);
            else
                spirvByPath[entry.Name] = bytes;
        }

        if (manifest == null) throw new InvalidDataException("Shader package is missing manifest.json");

        var stages = new CompiledStage[manifest.Stages.Length];
        for (var i = 0; i < manifest.Stages.Length; i++)
        {
            var manifestStage = manifest.Stages[i];
            if (!spirvByPath.TryGetValue(manifestStage.SpirvId, out var spirv))
                throw new InvalidDataException($"Shader package is missing '{manifestStage.SpirvId}'");

            stages[i] = new CompiledStage
            {
                Stage = manifestStage.Stage,
                Spirv = spirv,
                Reflection = manifestStage.Reflection
            };
        }

        return new CompiledShader
        {
            Kind = manifest.Kind,
            ThreadGroupSize = manifest.ThreadGroupSize,
            Stages = stages
        };
    }

    public static CompiledShader ReadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    /// <summary>
    ///     Just the manifest (including <see cref="ShaderManifest.SourceHash" />) - skips decompressing
    ///     SPIR-V/reflection data entirely. For a caller that only wants to know "is this .crsh still
    ///     fresh relative to its source", e.g. an editor deciding whether to invoke a real recompile.
    /// </summary>
    public static ShaderManifest? ReadManifest(Stream input)
    {
        using var tar = new TarReader(input, leaveOpen: true);

        TarEntry? entry;
        while ((entry = tar.GetNextEntry()) != null)
        {
            if (entry.Name != "manifest.json" || entry.DataStream is null) continue;
            return JsonSerializer.Deserialize(entry.DataStream, ShaderManifestJsonContext.Default.ShaderManifest);
        }

        return null;
    }

    public static ShaderManifest? ReadManifestFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return ReadManifest(stream);
    }
}
