using System;
using System.Collections.Generic;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Where the Slang being written sits, so a type can be named relative to it (VideoItem, not
/// Rin::Core::Views::Graphics::Shaders::VideoItem). The scopes whose members are visible unqualified are
/// listed innermost first - the namespaces enclosing the current one, or the `using namespace` lines at
/// global scope. A short name is only used when no more inner visible scope declares something with the
/// same first segment, since that one would be found first instead.
/// </summary>
internal static class NameScope
{
    [ThreadStatic] private static List<string>? _visible;
    [ThreadStatic] private static HashSet<string>? _declared;

    public static IDisposable InNamespace(string path, HashSet<string> declared)
    {
        var visible = new List<string>();
        for (var current = path; current.Length > 0; current = Parent(current))
            visible.Add(current);
        visible.Add("");
        return Enter(visible, declared);
    }

    public static IDisposable WithUsings(IReadOnlyList<string> usings, HashSet<string> declared)
    {
        var visible = new List<string>(usings) { "" };
        return Enter(visible, declared);
    }

    /// <summary>
    /// Names written while this is open are always fully qualified. Slang resolves the type of an
    /// `extension` header before it applies `using namespace`, so that one place can't be shortened.
    /// </summary>
    public static IDisposable Qualified()
    {
        var previous = (_visible, _declared);
        _visible = null;
        return new Restore(previous);
    }

    public static string Name(string namespacePath, string name)
    {
        var full = namespacePath.Length == 0 ? name : $"{namespacePath}::{name}";
        if (_visible is null || _declared is null) return full;

        for (var i = 0; i < _visible.Count; i++)
        {
            var scope = _visible[i];
            string candidate;
            if (scope.Length == 0) return full;
            if (namespacePath == scope) candidate = name;
            else if (namespacePath.StartsWith(scope + "::", StringComparison.Ordinal))
                candidate = $"{namespacePath[(scope.Length + 2)..]}::{name}";
            else continue;

            if (!IsShadowed(candidate, i)) return candidate;
        }

        return full;
    }

    private static bool IsShadowed(string candidate, int visibleIndex)
    {
        var separator = candidate.IndexOf("::", StringComparison.Ordinal);
        var firstSegment = separator < 0 ? candidate : candidate[..separator];

        for (var i = 0; i < visibleIndex; i++)
        {
            var scope = _visible![i];
            if (_declared!.Contains(scope.Length == 0 ? firstSegment : $"{scope}::{firstSegment}")) return true;
        }

        return false;
    }

    private static string Parent(string path)
    {
        var separator = path.LastIndexOf("::", StringComparison.Ordinal);
        return separator < 0 ? "" : path[..separator];
    }

    private static IDisposable Enter(List<string> visible, HashSet<string> declared)
    {
        var previous = (_visible, _declared);
        _visible = visible;
        _declared = declared;
        return new Restore(previous);
    }

    private sealed class Restore((List<string>? Visible, HashSet<string>? Declared) previous) : IDisposable
    {
        public void Dispose()
        {
            _visible = previous.Visible;
            _declared = previous.Declared;
        }
    }
}
