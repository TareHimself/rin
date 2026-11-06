namespace Rin.Slang;

public sealed class ShaderManifest
{
    public required ShaderKind Kind { get; init; }

    public uint[]? ThreadGroupSize { get; init; }

    public required ShaderManifestStage[] Stages { get; init; }

    /// <summary>See <see cref="ShaderSourceHash" />.</summary>
    public required string SourceHash { get; init; }

    /// <summary>
    ///     The compiler build that produced the package, so a compiler change recompiles it even when the source did not
    ///     change.
    /// </summary>
    public string? CompilerVersion { get; init; }
}

public sealed class ShaderManifestStage
{
    public required string Stage { get; init; }

    public required string SpirvId { get; init; }

    public required SlangReflectionData Reflection { get; init; }
}
