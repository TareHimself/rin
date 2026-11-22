using System.Reflection;
using System.Runtime.Loader;

namespace Rin.EditorSchema;

/// <summary>
///     A game's assembly and everything it references, loaded from the game's own output folder. The tool references no
///     Rin assembly itself, so the schema comes from exactly the Rin.World the game was built against.
/// </summary>
internal sealed class GameAssemblies : AssemblyLoadContext
{
    private const string ComponentJsonType = "Rin.World.Editing.ComponentJson";

    private readonly AssemblyDependencyResolver _resolver;
    private readonly Assembly _game;

    public GameAssemblies(string assemblyPath) : base(nameof(GameAssemblies))
    {
        _resolver = new AssemblyDependencyResolver(assemblyPath);
        _game = LoadFromAssemblyPath(assemblyPath);
    }

    /// <summary>
    ///     Loads every assembly the game references from its output folder, so each one's component module is found.
    /// </summary>
    public List<Assembly> LoadAll()
    {
        var loaded = new List<Assembly> { _game };
        var seen = new HashSet<string>([_game.FullName!]);
        for (var i = 0; i < loaded.Count; i++)
        foreach (var reference in loaded[i].GetReferencedAssemblies())
            if (seen.Add(reference.FullName) && TryLoad(reference) is { } assembly)
                loaded.Add(assembly);
        return loaded;
    }

    public int WriteSchema(List<Assembly> assemblies, string outputPath)
    {
        var writer = assemblies.Select(a => a.GetType(ComponentJsonType)).FirstOrDefault(t => t is not null)
                     ?? throw new InvalidOperationException("The game does not reference Rin.World.");
        var method = writer.GetMethod("WriteSchema", [typeof(IEnumerable<Assembly>), typeof(Stream)])!;
        using var stream = File.Create(outputPath);
        return (int)method.Invoke(null, [assemblies, stream])!;
    }

    protected override Assembly? Load(AssemblyName assemblyName) =>
        _resolver.ResolveAssemblyToPath(assemblyName) is { } path ? LoadFromAssemblyPath(path) : null;

    // Framework assemblies resolve to the shared runtime, which declares no components.
    private Assembly? TryLoad(AssemblyName reference) =>
        _resolver.ResolveAssemblyToPath(reference) is null ? null : LoadFromAssemblyName(reference);
}
