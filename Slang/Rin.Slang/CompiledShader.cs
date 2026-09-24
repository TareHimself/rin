namespace Rin.Slang;

public sealed class CompiledShader
{
    public required ShaderKind Kind { get; init; }

    public required CompiledStage[] Stages { get; init; }

    public uint[]? ThreadGroupSize { get; init; }

    /// <summary>
    ///     The source file and every file it transitively #includes - what actually determined this
    ///     compile's output - as (portable id, absolute path) pairs: the id is what gets hashed/embedded
    ///     (see <see cref="ShaderSourceHash" />), the absolute path is what's actually read from disk.
    ///     Only meaningful right after a real <see cref="ShaderCompiler" /> compile - a package read
    ///     back from disk has no source files to point at (they may not even exist on this machine),
    ///     and its freshness is already captured in <see cref="ShaderManifest.SourceHash" /> instead.
    /// </summary>
    public (string PortableId, string AbsolutePath)[] Dependencies { get; init; } = [];
}
