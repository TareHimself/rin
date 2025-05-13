namespace Rin.Engine.Graphics.Shaders.Slang;

public class SlangShaderManager : IShaderManager
{
    private readonly BackgroundTaskQueue _compileTasks = new();
    private readonly object _computeLock = new();
    private readonly Dictionary<string, SlangComputeShader> _computeShaders = [];
    private readonly object _graphicsLock = new();
    private readonly Dictionary<string, SlangGraphicsShader> _graphicsShaders = [];
    private readonly SlangSession _session;


    public SlangShaderManager()
    {
        using var builder = new SlangSessionBuilder();
        _session = builder
            .AddSearchPath(Path.GetDirectoryName(SGraphicsModule.ShadersDirectory) ?? "")
            .AddTargetSpirv().Build();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _compileTasks.Dispose();
        foreach (var shader in _graphicsShaders.Values) shader.Dispose();
        foreach (var shader in _computeShaders.Values) shader.Dispose();
        _graphicsShaders.Clear();
        _computeShaders.Clear();
        _session.Dispose();
    }

    public Task Compile(IShader shader)
    {
        return _compileTasks.Enqueue(() => shader.Compile(new SlangCompilationContext(this)));
    }

    public IGraphicsShader MakeGraphics(string path)
    {
        lock (_graphicsLock)
        {
            var absPath = Path.GetFullPath(path);

            {
                if (_graphicsShaders.TryGetValue(absPath, out var shader)) return shader;
            }

            {
                var shader = new SlangGraphicsShader(this, path);
                _graphicsShaders.Add(absPath, shader);
                return shader;
            }
        }
    }

    public IComputeShader MakeCompute(string path)
    {
        lock (_computeLock)
        {
            var absPath = Path.GetFullPath(path);

            {
                if (_computeShaders.TryGetValue(absPath, out var shader)) return shader;
            }

            {
                var shader = new SlangComputeShader(this, path);
                _computeShaders.Add(absPath, shader);
                return shader;
            }
        }
    }


    public static IEnumerable<string> ImportFile(string filePath, HashSet<string> included)
    {
        using var dataStream = SEngine.Get().Sources.Read(filePath);
        using var reader = new StreamReader(dataStream);
        while (reader.ReadLine() is { } line)
            if (line.StartsWith("#include"))
            {
                var includeString = line[(line.IndexOf('"') + 1)..line.LastIndexOf('"')];
                if (included.Add(includeString))
                    foreach (var importedLine in ImportFile(includeString, included))
                        yield return importedLine;
            }
            else
            {
                yield return line;
            }
    }

    public SlangSession GetSession()
    {
        return _session;
    }
}