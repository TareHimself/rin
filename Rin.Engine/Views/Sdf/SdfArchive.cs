using System.Text;
using System.Text.Json.Nodes;
using Rin.Engine.Core;
using Rin.Engine.Core.Archives;
using Rin.Engine.Core.Extensions;

namespace Rin.Engine.Views.Sdf;

public class SdfArchive(Stream data) : IReadArchive, ISdfContainer
{
    private readonly ZipArchive _archive = new(data);
    private readonly Dictionary<string, SdfVector> _vectors = [];
    private bool _vectorsLoaded;

    public void Dispose()
    {
        _archive.Dispose();
    }

    public IEnumerable<string> Keys => _archive.Keys;

    public int Count => _archive.Count;

    public Stream CreateReadStream(string key)
    {
        return _archive.CreateReadStream(key);
    }


    public SdfVector? GetVector(string id)
    {
        return GetVectorsDict().GetValueOrDefault(id);
    }

    public SdfResult? GetResult(int id)
    {
        var result = new SdfResult();
        using var memoryStream = new MemoryStream();
        GetAtlasStream(id).CopyTo(memoryStream);
        result.BinaryDeserialize(memoryStream);
        return result;
    }

    IEnumerable<SdfVector> ISdfContainer.GetVectors()
    {
        return GetVectorsDict().Values;
    }

    private Dictionary<string, SdfVector> GetVectorsDict()
    {
        if (_vectorsLoaded) return _vectors;

        var data = JsonNode.Parse(_archive.CreateReadStream("vectors.json"))?.AsObject() ??
                   throw new InvalidOperationException();
        var arr = data["vectors"]?.AsArray() ?? throw new InvalidOperationException();

        foreach (var jsonNode in arr)
        {
            var vector = new SdfVector();
            vector.JsonDeserialize(jsonNode?.AsObject() ?? throw new InvalidOperationException());

            _vectors.Add(vector.Id, vector);
        }

        _vectorsLoaded = true;

        return _vectors;
    }

    public Stream GetAtlasStream(int id)
    {
        return _archive.CreateReadStream(id.ToString());
    }

    public class Writer
    {
        private readonly Dictionary<int, SdfResult> _results = [];
        private readonly Dictionary<string, SdfVector> _vectors = [];

        public Writer()
        {
        }

        public Writer(Dictionary<string, SdfVector> vectors, IEnumerable<SdfResult> results)
        {
            _vectors = vectors;
            foreach (var (key, value) in _results) AddAtlas(key, value);
        }

        public void AddAtlas(int id, SdfResult data)
        {
            _results.Add(id, data);
        }

        public void AddVector(SdfVector vector)
        {
            _vectors.Add(vector.Id, vector);
        }

        public void SaveTo(string path)
        {
            using var archive = new ZipArchive();
            foreach (var (id, result) in _results)
            {
                var stream = new MemoryStream();
                result.BinarySerialize(stream);
                archive.Write($"{id}", stream);
            }

            var obj = new JsonObject();
            var vectorsJsonArray = new JsonArray();
            foreach (var (id, vector) in _vectors) vectorsJsonArray.Add(vector.ToJsonObject());
            obj["vectors"] = vectorsJsonArray;
            using var textStream = new MemoryStream(Encoding.UTF8.GetBytes(obj.ToString()));
            archive.Write("info.json", textStream);
            using var writeStream = SEngine.Get().Sources.Write(path);
            archive.SaveTo(writeStream);
        }
    }
}