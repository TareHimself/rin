namespace Rin.Slang;

public sealed class CompiledShader
{
    public required ShaderKind Kind { get; init; }

    public required CompiledStage[] Stages { get; init; }

    public uint[]? ThreadGroupSize { get; init; }
}
