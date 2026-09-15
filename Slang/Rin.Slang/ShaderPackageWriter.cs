using System.Formats.Tar;
using System.Text.Json;

namespace Rin.Slang;

public static class ShaderPackageWriter
{
    public static void Write(CompiledShader shader, Stream output)
    {
        var stages = new ShaderManifestStage[shader.Stages.Length];
        for (var i = 0; i < shader.Stages.Length; i++)
        {
            var stage = shader.Stages[i];
            stages[i] = new ShaderManifestStage
            {
                Stage = stage.Stage,
                SpirvId = $"spirv/{stage.Stage}.spv",
                Reflection = stage.Reflection
            };
        }

        var manifest = new ShaderManifest
        {
            Kind = shader.Kind,
            ThreadGroupSize = shader.ThreadGroupSize,
            Stages = stages
        };

        using var tar = new TarWriter(output, leaveOpen: true);

        WriteEntry(tar, "manifest.json",
            JsonSerializer.SerializeToUtf8Bytes(manifest, ShaderManifestJsonContext.Default.ShaderManifest));

        foreach (var stage in shader.Stages) WriteEntry(tar, $"spirv/{stage.Stage}.spv", stage.Spirv);
    }

    public static void WriteToFile(CompiledShader shader, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        using var stream = File.Create(path);
        Write(shader, stream);
    }

    private static void WriteEntry(TarWriter tar, string name, byte[] data)
    {
        var entry = new PaxTarEntry(TarEntryType.RegularFile, name)
        {
            DataStream = new MemoryStream(data)
        };
        tar.WriteEntry(entry);
    }
}
