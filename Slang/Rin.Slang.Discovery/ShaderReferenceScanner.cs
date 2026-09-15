using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rin.Slang.Discovery;

/// <summary>
///     Finds every shader content-key referenced anywhere in a set of .cs files, via
///     `[GraphicsShader("...")]`/`[ComputeShader("...")]` attributes or direct
///     `MakeGraphics("...")`/`MakeCompute("...")` calls. Parses source text directly (no compilation,
///     no project context) so it works across project boundaries and regardless of build order -
///     a Roslyn generator only ever sees its own project's compilation, which misses cross-project
///     references (e.g. Sponza referencing a shader that Rin.World owns).
/// </summary>
public static class ShaderReferenceScanner
{
    private static readonly string[] AttributeNames =
        ["GraphicsShader", "GraphicsShaderAttribute", "ComputeShader", "ComputeShaderAttribute"];

    private static readonly string[] MethodNames = ["MakeGraphics", "MakeCompute"];

    public static IReadOnlySet<string> ScanDirectory(string root)
    {
        return Scan(FindCsFiles(root));
    }

    public static IReadOnlySet<string> Scan(IEnumerable<string> csFiles)
    {
        var found = new HashSet<string>();

        foreach (var file in csFiles)
        {
            string text;
            try
            {
                text = File.ReadAllText(file);
            }
            catch (IOException)
            {
                continue;
            }

            var root = CSharpSyntaxTree.ParseText(text, path: file).GetRoot();

            foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
            {
                var name = attribute.Name.ToString();
                var simpleName = name[(name.LastIndexOf('.') + 1)..];
                if (!AttributeNames.Contains(simpleName)) continue;

                if (TryGetFirstStringLiteral(attribute.ArgumentList?.Arguments.Select(a => a.Expression), out var value))
                    found.Add(value);
            }

            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var simpleName = invocation.Expression switch
                {
                    MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
                    IdentifierNameSyntax identifier => identifier.Identifier.Text,
                    _ => null
                };

                if (simpleName == null || !MethodNames.Contains(simpleName)) continue;

                if (TryGetFirstStringLiteral(invocation.ArgumentList.Arguments.Select(a => a.Expression), out var value))
                    found.Add(value);
            }
        }

        return found;
    }

    private static bool TryGetFirstStringLiteral(IEnumerable<ExpressionSyntax>? expressions, out string value)
    {
        var first = expressions?.FirstOrDefault();
        if (first is LiteralExpressionSyntax literal && literal.Kind() == SyntaxKind.StringLiteralExpression)
        {
            value = literal.Token.ValueText;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static IEnumerable<string> FindCsFiles(string root)
    {
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsUnderExcludedDirectory(f));
    }

    private static bool IsUnderExcludedDirectory(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(p => p is "bin" or "obj" or ".git");
    }
}
