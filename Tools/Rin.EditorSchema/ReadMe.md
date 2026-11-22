# Rin.EditorSchema

`rin-editor-schema` builds a game project and writes the schema of its editable components, the engine's included: their names, fields and defaults. The [Blender add-on](../Blender/rin/ReadMe.md) builds its Add Component menu from that file.

```
dotnet run --project Tools/Rin.EditorSchema -- Examples/Examples/Examples.csproj editor.schema.json
```

| Argument | Meaning |
| --- | --- |
| `<project.csproj>` | The game project. It is built first, with `dotnet build`. |
| `[output.schema.json]` | Where to write the schema. Default: `editor.schema.json` in the current folder. |

It loads the built assembly and everything it references from the project's own output folder, and calls `ComponentJson.WriteSchema` with their component modules. It references no Rin assembly itself, so the schema always matches the Rin.World the game was built against. No window opens and nothing native loads.

References: none.
