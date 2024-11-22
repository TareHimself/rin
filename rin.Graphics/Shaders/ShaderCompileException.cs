using rsl.Nodes;

namespace rin.Graphics.Shaders;

public class ShaderCompileException(string message,string transpiledShader,ModuleNode ast) : Exception(message)
{
    public string Transpiled => transpiledShader;
    public ModuleNode Ast => ast;
}