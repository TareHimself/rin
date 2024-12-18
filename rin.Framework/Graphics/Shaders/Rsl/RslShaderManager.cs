using System.Runtime.InteropServices;
using rin.Framework.Core;
using rsl.Generator;
using rsl.Nodes;
using shaderc;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.Shaders.Rsl;

public class RslShaderManager : Disposable, IShaderManager
{
    private readonly Dictionary<string, NativeBuffer<uint>> _spirv = [];
    private readonly Compiler _compiler;
    private readonly BackgroundTaskQueue<CompiledShader> _backgroundTask = new ();
    public event Action? OnBeforeDispose;
    public RslShaderManager()
    {
        var opts = new Options()
        {
            Optimization = OptimizationLevel.Zero,
        };

        _compiler = new Compiler(opts);
    }
    
    public NativeBuffer<uint> CompileAstToSpirv(string id, VkShaderStageFlags stage, ModuleNode node)
    {
        
        var shader =
            "#version 450\n#extension GL_EXT_buffer_reference : require\n#extension GL_EXT_nonuniform_qualifier : require\n#extension GL_EXT_scalar_block_layout : require\n";

        var transpiler = new GlslGenerator();
        shader += transpiler.Run(node.Statements);
        
        using var shaderCompileResult = _compiler.Compile(shader, "<shader>", stage switch
        {
            VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT => ShaderKind.VertexShader,
            VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT => ShaderKind.FragmentShader,
            VkShaderStageFlags.VK_SHADER_STAGE_COMPUTE_BIT => ShaderKind.ComputeShader,
            _ => throw new RslShaderCompileException("Unknown shader stage",shader,node)
        });
        
        if (shaderCompileResult.Status != Status.Success)
            throw new RslShaderCompileException(shaderCompileResult.ErrorMessage,shader,node);
        
        unsafe
        {
            
            var buff = new NativeBuffer<uint>((int)shaderCompileResult.CodeLength / Marshal.SizeOf<uint>());
            _spirv.Add(id,buff);
            buff.Write(shaderCompileResult.CodePointer, shaderCompileResult.CodeLength);
            return buff;
        }
    }


    public Task<CompiledShader> Compile(IShader shader)
    {
        return _backgroundTask.Put(() => shader.Compile(this));
    }

    public IGraphicsShader GraphicsFromPath(string path)
    {
        return RslGraphicsShader.FromFile(path);
    }

    public IComputeShader ComputeFromPath(string path)
    {
        throw new NotImplementedException();
    }

    protected override void OnDispose(bool isManual)
    {
        OnBeforeDispose?.Invoke();
        _compiler.Dispose();
        foreach (var (_,buff) in _spirv)
        {
            buff.Dispose();
        }
    }
}