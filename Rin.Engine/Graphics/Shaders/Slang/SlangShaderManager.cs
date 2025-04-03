namespace Rin.Engine.Graphics.Shaders.Slang;

public class SlangShaderManager : IShaderManager
{
    private readonly BackgroundTaskQueue _compileTasks = new();
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
        foreach (var (key, shader) in _graphicsShaders) shader.Dispose();
        _graphicsShaders.Clear();
        _session.Dispose();
    }

    public Task Compile(IShader shader)
    {
        return _compileTasks.Enqueue(() => shader.Compile(new SlangCompilationContext(this)));
    }

    public IGraphicsShader MakeGraphics(string path)
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

    public IComputeShader MakeCompute(string path)
    {
        throw new NotImplementedException();
    }

    public event Action? OnBeforeDispose;

    public SlangSession GetSession()
    {
        return _session;
    }
}