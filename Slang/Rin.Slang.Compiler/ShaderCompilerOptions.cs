namespace Rin.Slang.Compiler;

public sealed class ShaderCompilerOptions
{
    private readonly Dictionary<string, string> _defines = [];
    private readonly Dictionary<string, string> _pathAliases = [];
    private readonly List<string> _searchPaths = [];

    public IReadOnlyList<string> SearchPaths => _searchPaths;

    public IReadOnlyDictionary<string, string> Defines => _defines;

    public IReadOnlyDictionary<string, string> PathAliases => _pathAliases;

    public ShaderCompilerOptions AddSearchPath(string path)
    {
        _searchPaths.Add(path);
        return this;
    }

    public ShaderCompilerOptions AddDefine(string name, string value)
    {
        _defines[name] = value;
        return this;
    }

    /// <summary>
    ///     Registers a virtual root so `#include "alias/rest/of/path.slang"` resolves against
    ///     <paramref name="path" /> instead of needing a search path whose real subfolder is named
    ///     <paramref name="alias" />. Checked before search paths, so an alias always wins.
    /// </summary>
    public ShaderCompilerOptions AddPathAlias(string alias, string path)
    {
        _pathAliases[alias] = path;
        return this;
    }
}
