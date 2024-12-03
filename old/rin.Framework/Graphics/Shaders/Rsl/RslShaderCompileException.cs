using rsl.Nodes;

namespace rin.Framework.Graphics.Shaders.Rsl;

public class RslShaderCompileException(string message,string transpiledShader,ModuleNode ast) : ShaderCompileException(message)
{
    public string Transpiled => transpiledShader;
    public ModuleNode Ast => ast;
}