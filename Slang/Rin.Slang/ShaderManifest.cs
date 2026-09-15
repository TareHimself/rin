namespace Rin.Slang;

public sealed class ShaderManifest
{
    public required ShaderKind Kind { get; init; }

    public uint[]? ThreadGroupSize { get; init; }

    public required ShaderManifestStage[] Stages { get; init; }
}

public sealed class ShaderManifestStage
{
    public required string Stage { get; init; }

    public required string SpirvId { get; init; }

    public required SlangReflectionData Reflection { get; init; }
}
