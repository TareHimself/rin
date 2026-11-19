# rin_blender

A Blender add-on for authoring Rin levels. Build the scene in Blender, export it with **File > Export > Rin Level (.glb)**, and load it with `GltfSceneImporter` (see [Rin.GLTF](../../Engine/Rin.GLTF/ReadMe.md)). It runs in Blender's own Python, so it is not part of the uv tooling.

| Adds | What it does |
| --- | --- |
| Light properties > Rin | **Lumens** for point and spot lights, **Lux** for suns. They edit Blender's power directly, so the Blender preview and the export always agree |
| File > Export > Rin Level (.glb) | Runs the glTF exporter with lights, cameras, custom properties and physical light units on, and names the active camera as the level's start camera |
| Linked duplicates | Turns off Blender's Duplicate Data > Mesh preference, so Shift+D shares the mesh and the level uploads it once |

It is a Blender extension (MIT licensed, see `blender_manifest.toml`). Build the zip, then pick it with **Install from Disk** in Blender's Add-ons preferences:

```
blender-launcher -c extension build --source-dir Tools/rin_blender --output-dir .
```

Lumens and lux map to Blender's watts at 683 lm/W, the factor the glTF exporter uses in its Standard lighting mode. Materials are not exported for Rin's use: Rin picks them by glTF material name, and Blender's shaders are only a preview.

`Engine/Rin.GLTF.Tests/Assets/make_blender_level.py` builds a test level through the add-on from the command line:

```
blender-launcher -b --factory-startup --python Engine/Rin.GLTF.Tests/Assets/make_blender_level.py
```
