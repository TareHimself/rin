using System.Runtime.InteropServices;

namespace Rin.Slang.Compiler;

/// <summary>
///     Marks a vertex stage's position output <c>Invariant</c>, so every pipeline that draws the same mesh computes the
///     same depth (the depth prepass and the color pass must agree exactly). Slang has no syntax for it, so the
///     decoration is added to the compiled SPIR-V.
/// </summary>
internal static class SpirvInvariantPosition
{
    // Constants from the SPIR-V specification: https://registry.khronos.org/SPIR-V/specs/unified1/SPIRV.html
    private const int HeaderWords = 5;
    private const uint OpDecorate = 71;
    private const uint DecorationBuiltIn = 11;
    private const uint DecorationInvariant = 18;
    private const uint BuiltInPosition = 0;

    /// <summary>
    ///     The module with <c>OpDecorate %position Invariant</c> inserted after its <c>BuiltIn Position</c> decoration, or
    ///     the module unchanged when it has none.
    /// </summary>
    public static byte[] Apply(byte[] spirv)
    {
        var words = MemoryMarshal.Cast<byte, uint>(spirv);
        var i = HeaderWords;
        while (i < words.Length)
        {
            var wordCount = (int)(words[i] >> 16);
            if (wordCount == 0) break;

            if (IsPositionDecoration(words.Slice(i, wordCount)))
            {
                uint[] invariant = [(3u << 16) | OpDecorate, words[i + 1], DecorationInvariant];
                var insertAt = (i + wordCount) * sizeof(uint);
                return [..spirv.AsSpan(0, insertAt), ..MemoryMarshal.AsBytes(invariant.AsSpan()), ..spirv.AsSpan(insertAt)];
            }

            i += wordCount;
        }

        return spirv;
    }

    // OpDecorate <target> BuiltIn Position
    private static bool IsPositionDecoration(ReadOnlySpan<uint> instruction) =>
        instruction.Length == 4 &&
        (instruction[0] & 0xFFFF) == OpDecorate &&
        instruction[2] == DecorationBuiltIn &&
        instruction[3] == BuiltInPosition;
}
