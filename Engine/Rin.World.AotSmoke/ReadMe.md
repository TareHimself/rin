# Rin.World.AotSmoke

A console app with `PublishAot` that checks a `World` runs under NativeAOT, where its own components get ids on first use without any registration (see [Rin.World](../Rin.World/ReadMe.md)). It spawns entities with components and tags, runs a query, plays back the command buffer, propagates a hierarchy, and saves and restores a snapshot. It exits 0 on success and 1 with the failed check on stderr.

Publish and run it from PowerShell. The NativeAOT linker finds the C++ toolchain through `vswhere`, which is not on the PATH of Git Bash:

```
$env:PATH = "C:\Program Files (x86)\Microsoft Visual Studio\Installer;$env:PATH"
dotnet publish Engine/Rin.World.AotSmoke/Rin.World.AotSmoke.csproj -c Release -r win-x64 -p:RinShadeSkipCompile=true
Engine/Rin.World.AotSmoke/bin/Release/net10.0/win-x64/publish/Rin.World.AotSmoke.exe
```

```
Rin.World AOT smoke test passed.
```

A JIT run (`dotnet run --project Engine/Rin.World.AotSmoke/Rin.World.AotSmoke.csproj -p:RinShadeSkipCompile=true`) prints the same without the native toolchain. The publish reports no trim or AOT warnings. It is not run in CI.

References: Rin.World.
