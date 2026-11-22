# Rin.GLTF

Imports glTF and glb files into Rin: `GltfMeshImporter` loads static and skinned meshes, `GltfAnimationImporter` loads animation clips, and `GltfSceneImporter` spawns a whole scene into a world, all through `SharpGLTF.Core`. glTF is right-handed and Rin left-handed, so the importers mirror X: a model facing glTF's +Z faces Rin's forward, and left and right match what the authoring tool showed.

`GltfSceneImporter.Load(path, renderFeature, resolveMaterial)` takes a level folder (its `level.glb`) or a glTF file and spawns one entity per node with its static mesh (one upload per glTF mesh) and perspective cameras. The caller turns each glTF material into an `IMeshMaterial`. Lights come from `PointLight` and `DirectionalLight` components in `rin_components`; `KHR_lights_punctual` lights, skinned meshes and orthographic cameras are skipped and reported in `Warnings`. These extras add to the glTF values:

| Extra | On | Meaning |
| --- | --- | --- |
| `rin_components` | any node | Object of [components](../Rin.World/ReadMe.md#editor-components) to add, keyed by full type name, each holding its editable fields. The world's schema must hold them, so compose it with the game's component module. `Entity` fields name a node. Unknown or malformed components are skipped with a warning |
| `rin_collider` | collider node | A box, sphere or capsule in the node's own frame. Collider nodes, and groups of them marked `rin_colliders`, spawn no entities: they become the collider component of the nearest node above them with a `RigidBody`, replacing any collider its own components gave it: a box, sphere or capsule collider when there is one centred collider, otherwise a `CompoundCollider` registered with the `PhysicsShapes` passed to `Load` |
| `rin_components` `Map` | an `EnvironmentLight` | A `.hdr` path relative to the level's folder, loaded once per file into `RenderFeature.Environments`. A missing or unreadable map skips the light with a warning |
| `rin_start_camera` | scene | Name of the camera node returned as `StartCamera`, instead of the first camera |

```
dotnet build Engine/Rin.GLTF/Rin.GLTF.csproj
dotnet test Engine/Rin.GLTF.Tests/Rin.GLTF.Tests.csproj -p:RinShadeSkipCompile=true
```

The tests need the native fake published first, see [Rin.GLTF.Tests](../Rin.GLTF.Tests/ReadMe.md).

References: Rin.World (and Rin.Core through it), SharpGLTF.Core. Used by Rin.GLTF.Tests and Examples.
