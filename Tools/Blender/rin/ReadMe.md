# rin

A Blender add-on for authoring Rin levels. Build the scene in Blender, export it with **File > Export > Rin Level (folder)**, and load the folder with `GltfSceneImporter` (see [Rin.GLTF](../../../Engine/Rin.GLTF/ReadMe.md)). It runs in Blender's own Python, so it is not part of the uv tooling.

| Adds | What it does |
| --- | --- |
| Object properties > Rin Components, and the 3D Viewport sidebar's Rin tab | **Add Component** menu, and a collapsible section per added component, for every editable component in the schema set in the add-on preferences, except the ones Blender already authors (`LocalTransform`, `Camera`, `AttachSocket`). The export writes them as the `rin_components` extra |
| Rin Preview collection | A locked Blender light for every **Point Light** and **Directional Light** component, so the viewport shows Rin's lights. The add-on keeps it in sync, hides it with its owner, and leaves it out of the export |
| Rin Components > Add Component > Rigid Body | Gives the object a Rin `RigidBody` section: State (Static, Controlled, Simulated), Mass, Is Trigger and Collision Channel, plus its colliders. **Add Collider** adds a Box, Sphere or Capsule, sized to the mesh, under the object's `<name> Colliders` empty; each row can select or delete its collider. Hiding the object hides its colliders too. Without colliders a mesh gets a box from its bounds. On import one centred collider stays a simple shape, anything else becomes a compound. Works on empties too, for trigger zones |
| Environment Light component | A `.hdr` panorama that lights everything (**Unbounded**, like a sky) or only views inside the empty's box, which you size by scaling the empty. An unbounded one is previewed as the scene's World (your own World comes back when it goes); box lights have no Blender preview |
| File > Export > Rin Level (folder) | Writes `level.glb` into the folder you pick and copies the files it uses (environment maps into `environments/`), so the folder moves as one. Cameras and custom properties are on, and the active camera is the level's start camera. Blender's own lights are not exported, and the export warns about them |
| Linked duplicates | Turns off Blender's Duplicate Data > Mesh preference, so Shift+D shares the mesh and the level uploads it once |

It is a Blender extension (MIT licensed, see `blender_manifest.toml`). Load it straight from the repo, so code changes need only **F3 > Reload Scripts** or a restart:

1. **Edit > Preferences > Get Extensions > Repositories > + > Add Local Repository**, tick **Custom Directory** and pick `Tools/Blender`.
2. Enable **Rin** in the add-on list.

To share it instead, build a zip and use **Install from Disk**:

```
blender-launcher -c extension build --source-dir Tools/Blender/rin --output-dir .
```

Write the schema with [rin-editor-schema](../../Rin.EditorSchema/ReadMe.md) (`dotnet run --project Tools/Rin.EditorSchema -- Examples/Examples/Examples.csproj editor.schema.json`), then set **Component Schema** in the add-on preferences to that file. Rerun it and press the reload button after changing a component. `Vector3` and `Quaternion` fields are entered in Rin's axes (+Y up, +Z forward), not Blender's.

Put lights on empties: add a light component, and the preview light follows the empty. A directional light shines along the empty's -Y, so rotate the empty to aim it. Preview power uses 683 lm/W, and a point light's preview reaches as far as Rin's light does (Radius on **Auto** derives it from the lumens). Materials are not exported for Rin's use: Rin picks them by glTF material name, and Blender's shaders are only a preview.

`Engine/Rin.GLTF.Tests/Assets/make_blender_level.py` builds a test level through the add-on from the command line:

```
blender-launcher -b --factory-startup --python Engine/Rin.GLTF.Tests/Assets/make_blender_level.py
```
