# ViewsTest

Test bench for views, animation, images and audio effects (parametric EQ, stress-test delay, bloom). The build is a `WinExe` on Windows, so no console window appears.

```
dotnet run --project Examples/ViewsTest/ViewsTest.csproj
dotnet run --project Examples/ViewsTest/ViewsTest.csproj -- --stencil
```

`--stencil` shows `StencilScene` instead of the default animation scene.

Keys (from `ViewsTestApplication.cs`):
- Up opens a child window. Alt+Enter toggles fullscreen.
- Default scene: `=`, `-`, `0` add test items to the list.
- K toggles the parametric EQ (then M or N pick the vocal or bass preset), J toggles the stress-test effect (then H and Y lower and raise feedback), L toggles the bloom effect.

References: Rin.Shade (plus its source generator), Rin.SourceGenerators, Examples.Common.
