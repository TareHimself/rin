using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Rin.Engine.Archives;
using Rin.Engine.Graphics;
using SharpCompress.Writers;

namespace Rin.Engine.Views.Sdf;

public class SdfArchive : IDisposable, ISdfContainer
{
    
    private readonly SqliteArchive _archive;
    private readonly ConcurrentDictionary<string, SdfImage> _images = [];
    private readonly ConcurrentDictionary<string, SdfVector> _vectors = [];
    public SdfArchive(string filename)
    {
        _archive = new SqliteArchive(filename);
        LoadFromArchive();
    }

    public SdfArchive()
    {
        _archive = new SqliteArchive();
    }

    public string[] GetImages()
    {
        return _images.Keys.ToArray();
    }

    public string[] GetVectors()
    {
        return _vectors.Keys.ToArray();
    }

    public IHostImage? LoadImage(string id)
    {
        if (_archive.CreateReadStream($"{id}.png") is { } stream)
        {
            return HostImage.Create(stream);
        }

        return null;
    }

    public SdfImage? GetImage(string id)
    {
        return _images.GetValueOrDefault(id);
    }

    public SdfVector? GetVector(string id)
    {
        return _vectors.GetValueOrDefault(id);
    }

    public bool HasImage(string id)
    {
        return _images.ContainsKey(id);
    }

    public bool HasVector(string id)
    {
        return _vectors.ContainsKey(id);
    }

    public string AddImage(IHostImage image)
    {
        var id = Guid.NewGuid().ToString();
        var sdfImage = new SdfImage
        {
            Id = id,
            Extent = image.Extent
        };
        _images.AddOrUpdate(id, sdfImage, (_, _) => sdfImage);
        var stream = new MemoryStream();
        image.Save(stream);
        WriteImage(id, stream);
        WriteInfo();
        return id;
    }
    
    public string[] AddImages(IEnumerable<HostImage> images)
    {

        var ids = images.Select(image =>
        {
            var id = Guid.NewGuid().ToString();
            var sdfImage = new SdfImage
            {
                Id = id,
                Extent = image.Extent
            };
            _images.AddOrUpdate(id, sdfImage, (_, _) => sdfImage);
            var stream = new MemoryStream();
            image.Save(stream);
            WriteImage(id, stream);
            return id;
        }).ToArray();
        
        WriteInfo();
        
        return ids;
    }

    public void AddVector(SdfVector vector)
    {
        _vectors.AddOrUpdate(vector.Id, vector,(_, _) => vector);
        WriteInfo();
    }
    
    public void AddVectors(IEnumerable<SdfVector> vectors)
    {
        foreach (var vector in vectors)
        {
            _vectors.AddOrUpdate(vector.Id, vector,(_, _) => vector);
        }
        WriteInfo();
    }
    
    public void Dispose()
    {
        WriteInfo();
        _archive.Dispose();
    }
    
    private void LoadFromArchive()
    {
        // var data = new MemoryStream();
        // _archive.CreateReadStream("info.json").CopyTo(data);
        // var text = Encoding.UTF8.GetString(data.ToArray());
        if (_archive.Keys.Contains("info.json") && _archive.CreateReadStream("info.json") is { } stream && JsonSerializer.Deserialize<Info>(stream) is { } info)
        {
            foreach (var image in info.Images)
            {
                _images.AddOrUpdate(image.Id, image, (_, _) => image);
            }

            foreach (var vector in info.Vectors)
            {
                _vectors.AddOrUpdate(vector.Id, vector, (_, _) => vector);
            }
        }
    }

    private void WriteImage(string id, Stream image)
    {
        _archive.Write($"{id}.png",image);
    }
    
    private void WriteInfo()
    {
        var infoObject = new Info
        {
            Images = _images.Select(c => c.Value).ToArray(),
            Vectors = _vectors.Select(c => c.Value).ToArray(),
        };
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, infoObject);
        _archive.Write("info.json", stream);
    }
    
    class Info
    {
        public SdfImage[] Images { get; set; } = [];
        public SdfVector[] Vectors { get; set; } = [];
    }
}