using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Maps a line of emitted Slang back to the C# location it was lowered from, so a Slang compiler
/// error can be reported against the shader class instead of the generated text.
/// </summary>
public sealed class SlangSourceMap(IReadOnlyList<Location?> lineOrigins)
{
    public int LineCount => lineOrigins.Count;

    /// <summary>
    /// The C# location for a 1-based Slang line, or null when that line has no C# counterpart
    /// (type declarations, bindings, blank lines).
    /// </summary>
    public Location? Origin(int slangLine)
    {
        var index = slangLine - 1;
        if (index < 0 || index >= lineOrigins.Count) return null;
        var origin = lineOrigins[index];
        return origin is { IsInSource: true } ? origin : null;
    }
}
