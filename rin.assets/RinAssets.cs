using Rin.FileSystem;

namespace Rin.Assets;

public static class RinAssets
{
    public static ResourcesFileSystem FileSystem { get; } = new ResourcesFileSystem(typeof(RinAssets).Assembly);
}