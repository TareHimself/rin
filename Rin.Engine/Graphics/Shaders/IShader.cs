namespace Rin.Engine.Graphics.Shaders;

public interface IShader : IDisposable
{
    public Dictionary<string, Resource> Resources { get; }
    public Dictionary<string, PushConstant> PushConstants { get; }
    public bool Ready { get; }
    public bool Bind(IExecutionContext ctx, bool wait = true);
    public void Compile(ICompilationContext context);
    public void Push<T>(IExecutionContext ctx, in T data, uint offset = 0) where T : unmanaged;
}