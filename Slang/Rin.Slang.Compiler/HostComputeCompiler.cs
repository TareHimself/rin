using System.Runtime.InteropServices;

namespace Rin.Slang.Compiler;

/// <summary>
///     Compiles a self-contained compute shader to an in-process CPU function (Slang's host-callable
///     target, JIT compiled with LLVM), so the same Slang the GPU runs can be executed against ordinary
///     managed memory. A buffer pointer in the shader is a real process pointer here, which is what lets
///     a test pin a C# array, run the shader over it and compare with a CPU reference.
/// </summary>
public sealed class HostComputeCompiler : IDisposable
{
    private readonly SlangSession _session;

    public HostComputeCompiler()
    {
        using var builder = new SlangSessionBuilder();
        builder.AddTargetHostCallable();
        _session = builder.Build();
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    public HostComputeShader Compile(string slangSource, string moduleName = "host_shader")
    {
        using var diagnostics = new SlangBlob();
        using var module = _session.LoadModuleFromSourceString(moduleName, moduleName + ".slang", slangSource,
                               diagnostics) ??
                           throw new SlangCompileException("Failed to load slang module:\n" + diagnostics.GetString());

        using var entryPoint = module.FindEntryPointByName("compute") ??
                               throw new NotAShaderException("Shader has no 'compute' entry point");

        using var composed = _session.CreateComposedProgram(module, [entryPoint]) ??
                             throw new SlangCompileException("Failed to create composed program.");
        using var linked = composed.Link() ?? throw new SlangCompileException("Failed to link composed program.");

        using var codeDiagnostics = new SlangBlob();
        var library = linked.GetEntryPointHostCallable(0, 0, codeDiagnostics) ??
                      throw new SlangCompileException("Failed to generate host-callable code.\n" +
                                                      codeDiagnostics.GetString());

        var function = library.FindFunction("compute");
        if (function == IntPtr.Zero)
        {
            library.Dispose();
            throw new SlangCompileException("Host-callable library has no 'compute' function.");
        }

        return new HostComputeShader(library, function);
    }
}

public sealed unsafe class HostComputeShader : IDisposable
{
    private readonly IntPtr _function;
    private readonly SlangSharedLibrary _library;

    internal HostComputeShader(SlangSharedLibrary library, IntPtr function)
    {
        _library = library;
        _function = function;
    }

    public void Dispose()
    {
        _library.Dispose();
    }

    /// <summary>
    ///     Runs the shader over the given thread-group counts. <paramref name="pushConstants" /> is the
    ///     push-constant struct, laid out exactly as it is for the GPU (scalar layout): pointers are plain
    ///     addresses of pinned managed memory.
    /// </summary>
    public void Dispatch<TPush>(uint groupsX, uint groupsY, uint groupsZ, ref TPush pushConstants)
        where TPush : unmanaged
    {
        // ComputeVaryingInput { uint3 startGroupID; uint3 endGroupID (exclusive); }
        var varying = stackalloc uint[6] { 0, 0, 0, groupsX, groupsY, groupsZ };

        fixed (TPush* push = &pushConstants)
        {
            // The global parameters are one pointer to the push struct.
            var globalParams = stackalloc void*[1] { push };
            ((delegate* unmanaged[Cdecl]<uint*, void*, void*, void>)_function)(varying, null, globalParams);
        }
    }
}
