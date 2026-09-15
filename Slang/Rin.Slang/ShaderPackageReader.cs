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
}
