using System.Security.Cryptography;
using System.Text;

namespace Rin.Slang;

/// <summary>
///     Content hash over a shader's resolved dependency set (source + transitive #includes), so a
///     caller - the MSBuild pipeline, an editor's live-reload check, anything - can decide whether a
///     .crsh is still fresh by comparing against <see cref="ShaderManifest.SourceHash" /> without
///     invoking the Slang compiler. Takes a portable id per dependency (rather than resolving one
///     itself) so this stays root-agnostic - it's the caller who knows what "portable" means for its
///     environment (e.g. a path relative to a repo root).
/// </summary>
public static class ShaderSourceHash
{
    public static string Compute(IEnumerable<(string PortableId, string AbsolutePath)> dependencies)
    {
        using var sha256 = SHA256.Create();
        using var combined = new MemoryStream();

        foreach (var (portableId, absolutePath) in dependencies.OrderBy(d => d.PortableId, StringComparer.Ordinal))
        {
            combined.Write(Encoding.UTF8.GetBytes(portableId));
            combined.Write(File.ReadAllBytes(absolutePath));
        }

        return Convert.ToHexString(sha256.ComputeHash(combined.ToArray()));
    }
}
