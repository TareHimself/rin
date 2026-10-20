# Rin.Slang

The compiled shader package format (`.crsh`): the compiled result, its manifest and reflection data, and the writer and reader for it. No native dependency. Packed as `TareHimself.Rin.Slang` (AOT compatible).

```
dotnet build Slang/Rin.Slang/Rin.Slang.csproj
dotnet test Slang/Rin.Slang.Tests/Rin.Slang.Tests.csproj
```

References: none. Used by `Rin.Slang.Compiler` to write packages and by `Engine/Rin.Graphics.Vulkan` to read them (project reference when `DepMode` is `local_file`, package reference when `online`). Tested by [Rin.Slang.Tests](../Rin.Slang.Tests/ReadMe.md).
