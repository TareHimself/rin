namespace Rin.Slang;

public sealed class CompiledStage
{
    public required string Stage { get; init; }

    public required byte[] Spirv { get; init; }

    public required SlangReflectionData Reflection { get; init; }
}
