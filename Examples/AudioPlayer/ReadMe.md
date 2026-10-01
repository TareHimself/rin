# AudioPlayer

Audio player UI (track player, visualizer, file picker views) over an image switcher background, built on the engine's views and Miniaudio modules. Uses the `SpotifyExplode` and `YoutubeExplode` packages. Volume starts at 0.1.

```
dotnet run --project Examples/AudioPlayer/AudioPlayer.csproj
```

Controls (background image switcher, from `AudioPlayerApp.cs`): Left and Right arrows change image, Enter opens a file picker for png/jpg images.

References: Rin.Core, Examples.Common.
