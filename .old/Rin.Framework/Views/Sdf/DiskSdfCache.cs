using Rin.Framework.Graphics;

namespace Rin.Framework.Views.Sdf;

public class DiskSdfCache : ISdfCache
{
    private readonly SdfArchive _archive;

    public DiskSdfCache(string cacheFilePath)
    {
        var dir = Path.GetDirectoryName(cacheFilePath) ?? string.Empty;
        Directory.CreateDirectory(dir);
        _archive = new SdfArchive(cacheFilePath);
    }

    public string[] GetImages()
    {
        return _archive.GetImages();
    }

    public string[] GetVectors()
    {
        return _archive.GetVectors();
    }

    public IHostImage? LoadImage(string id)
    {
        return _archive.LoadImage(id);
    }

    public SdfImage? GetImage(string id)
    {
        return _archive.GetImage(id);
    }

    public SdfVector? GetVector(string id)
    {
        return _archive.GetVector(id);
    }

    public bool HasImage(string id)
    {
        return _archive.HasImage(id);
    }

    public bool HasVector(string id)
    {
        return _archive.HasVector(id);
    }

    public string AddImage(IHostImage image)
    {
        return _archive.AddImage(image);
    }

    public void AddVector(SdfVector vector)
    {
        _archive.AddVector(vector);
    }

    public string[] AddImages(IEnumerable<HostImage> images)
    {
        return _archive.AddImages(images);
    }

    public void AddVectors(IEnumerable<SdfVector> vectors)
    {
        _archive.AddVectors(vectors);
    }

    ~DiskSdfCache()
    {
        _archive.Dispose();
        Console.WriteLine("Disk Sdf Cache Cleaned");
    }
}