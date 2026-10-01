# Rin.World

The scene layer on top of Rin.Core: a `World` that owns actors and components, systems that update them, and the render and physics integration.

## Where it fits

- References: Rin.Core, `Shade/Rin.Shade`, `Shade/Rin.Shade.SourceGenerator`, Rin.SourceGenerators (as analyzer) and the `BepuPhysics` package.
- Referenced by: Rin.GLTF and Rin.World.Tests.
- It has no reference to a graphics backend, so it renders through the Rin.Core interfaces.

## Start here

- `World.cs`: the world, implements `IUpdatable`. `WorldContent.cs` and `WorldExtensions.cs` sit beside it.
- `Actors/Actor.cs` and `Components/` (`Component.cs`, `StaticMeshComponent.cs`, `SkinnedMeshComponent.cs`, `CameraComponent.cs`, physics components such as `BoxPhysicsComponent.cs`, `Lights/`).
- `Systems/ISystem.cs`, `Systems/LightSystem.cs`.
- `Graphics/IRenderSystem.cs`, `Graphics/Default/`, `Graphics/RenderProxyHandle.cs`: how the world is turned into render commands.
- `Physics/`, `Mesh/`, `Math/`, `Views/`.

## Build and test

```
dotnet build Engine/Rin.World/Rin.World.csproj
dotnet test Engine/Rin.World.Tests/Rin.World.Tests.csproj -p:RinShadeSkipCompile=true
```

## Notes

- Shaders under `Shaders/Rin/World/` are compiled into `Content\Shaders\Rin\World\` by the Rin.Shade targets. This needs the real Slang native package. CI builds this project in Release in a separate job (`Compile all shaders`) for that purpose, and the test job skips it with `-p:RinShadeSkipCompile=true`.
