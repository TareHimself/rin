using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rin.Shade.Transpiler;

/// <summary>
/// One diagnostic from the Slang compiler's text output: <c>path(line): error 30027: message</c>.
/// </summary>
public sealed record SlangDiagnostic(string Path, int Line, int Column, string Severity, string Code, string Message)
{
    private static readonly Regex Pattern = new(
        @"^(?<path>.+?)\((?<line>\d+)(?:,(?<column>\d+))?\): (?<severity>error|warning|note|fatal error) (?<code>\d+): (?<message>.*)$",
        RegexOptions.Compiled);

    /// <summary>
    /// Every diagnostic line in Slang's output. Source excerpts and caret lines are not diagnostics and are skipped.
    /// </summary>
    public static List<SlangDiagnostic> Parse(string output)
    {
        var diagnostics = new List<SlangDiagnostic>();
        foreach (var rawLine in output.Split('\n'))
        {
            var match = Pattern.Match(rawLine.TrimEnd('\r'));
            if (!match.Success) continue;

            var column = match.Groups["column"].Success ? int.Parse(match.Groups["column"].Value) : 0;
            diagnostics.Add(new SlangDiagnostic(match.Groups["path"].Value, int.Parse(match.Groups["line"].Value),
                column, match.Groups["severity"].Value, match.Groups["code"].Value, match.Groups["message"].Value));
        }

        return diagnostics;
    }
}
