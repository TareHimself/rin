# HeadlessTest

Runs the engine with `NullGraphicsModule` and `NullAudioModule` (no window, no GPU). Creates a `World` with Bepu physics, drops a sphere from height 50 at an accelerated time scale, prints its height every 0.25 s and exits after 3 s.

```
dotnet run --project Examples/HeadlessTest/HeadlessTest.csproj
```

References: Rin.Core, Rin.World, Rin.Graphics.Null, Rin.Audio.Null.
