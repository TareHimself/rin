using Rin.Shading.Ast.Nodes;

namespace Rin.Shading;

public interface ITranspiler
{
    public void Transpile(IEnumerable<INode> source, Stream output);
}