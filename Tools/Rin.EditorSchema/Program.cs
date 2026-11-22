using Rin.EditorSchema;

if (args.Length is < 1 or > 2 || !args[0].EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Usage: rin-editor-schema <project.csproj> [output.schema.json]");
    return 1;
}

var project = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args.Length > 1 ? args[1] : "editor.schema.json");

if (!ProjectBuilder.Build(project)) return 1;
var assemblyPath = ProjectBuilder.TargetPathOf(project);

var game = new GameAssemblies(assemblyPath);
var count = game.WriteSchema(game.LoadAll(), output);
Console.WriteLine($"Wrote {count} component{(count == 1 ? "" : "s")} to '{output}'");
return 0;
