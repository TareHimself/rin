using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Rin.Slang.Native.Fake;

// Just enough to make ShaderCompiler.TryCompile succeed for the compute-only shaders
// Rin.Slang.Compiler.Tests uses: every "object" is an opaque 1-byte allocation, diagnostics
// are never written to (the real tests never read them on the success path), and the two
// blobs whose content actually gets read (entry point code, layout json) are tracked by
// pointer so slangBlobGetSize/slangBlobGetPointer can answer for them.
public static unsafe class NativeFake
{
    private static readonly ConcurrentDictionary<nint, int> BlobSizes = new();

    // 0 is Ok in SlangNativeResult; the fake never fails.
    private const int Ok = 0;

    [UnmanagedCallersOnly(EntryPoint = "slangSessionBuilderNew")]
    public static int SlangSessionBuilderNew(void** outBuilder)
    {
        *outBuilder = NativeMemory.Alloc(1);
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "slangSessionBuilderAddTargetSpirv")]
    public static void SlangSessionBuilderAddTargetSpirv(void* builder)
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "slangSessionBuilderAddTargetGlsl")]
    public static void SlangSessionBuilderAddTargetGlsl(void* builder)
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "slangSessionBuilderAddPreprocessorDefinition")]
    public static void SlangSessionBuilderAddPreprocessorDefinition(void* builder, byte* name, byte* value)
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "slangSessionBuilderAddSearchPath")]
    public static void SlangSessionBuilderAddSearchPath(void* builder, byte* path)
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "slangSessionBuilderBuild")]
    public static int SlangSessionBuilderBuild(void* builder, void** outSession)
    {
        *outSession = NativeMemory.Alloc(1);
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "slangSessionBuilderFree")]
    public static void SlangSessionBuilderFree(void* builder) => NativeMemory.Free(builder);

    [UnmanagedCallersOnly(EntryPoint = "slangSessionLoadModuleFromSourceString")]
    public static int SlangSessionLoadModuleFromSourceString(void* session, byte* moduleName, byte* path,
        byte* content, void* outDiagnostics, void** outModule)
    {
        *outModule = NativeMemory.Alloc(1);
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "slangSessionCreateComposedProgram")]
    public static int SlangSessionCreateComposedProgram(void* session, void* module, nuint* entryPoints,
        int entryPointsCount, void* outDiagnostics, void** outComponent)
    {
        *outComponent = NativeMemory.Alloc(1);
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "slangSessionFree")]
    public static void SlangSessionFree(void* session) => NativeMemory.Free(session);

    [UnmanagedCallersOnly(EntryPoint = "slangModuleFindEntryPointByName")]
    public static int SlangModuleFindEntryPointByName(void* module, byte* name, void** outEntryPoint)
    {
        *outEntryPoint = NativeMemory.Alloc(1);
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "slangEntryPointFree")]
    public static void SlangEntryPointFree(void* entryPoint) => NativeMemory.Free(entryPoint);

    [UnmanagedCallersOnly(EntryPoint = "slangModuleFree")]
    public static void SlangModuleFree(void* module) => NativeMemory.Free(module);

    [UnmanagedCallersOnly(EntryPoint = "slangComponentGetEntryPointCode")]
    public static int SlangComponentGetEntryPointCode(void* component, int entryPointIndex, int targetIndex,
        void* outDiagnostics, void** outCode)
    {
        var blob = NativeMemory.Alloc(4);
        new Span<byte>(blob, 4).Clear();
        BlobSizes[(nint)blob] = 4;
        *outCode = blob;
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "slangComponentLink")]
    public static int SlangComponentLink(void* component, void* outDiagnostics, void** outLinked)
    {
        *outLinked = NativeMemory.Alloc(1);
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "slangComponentToLayoutJson")]
    public static int SlangComponentToLayoutJson(void* component, void** outJson)
    {
        var json = "{\"entryPoints\":[{\"name\":\"compute\",\"threadGroupSize\":[1,1,1]}]}"u8;
        var blob = (byte*)NativeMemory.Alloc((nuint)(json.Length + 1));
        json.CopyTo(new Span<byte>(blob, json.Length));
        blob[json.Length] = 0;
        BlobSizes[(nint)blob] = json.Length;
        *outJson = blob;
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "slangComponentFree")]
    public static void SlangComponentFree(void* component) => NativeMemory.Free(component);

    [UnmanagedCallersOnly(EntryPoint = "slangBlobNew")]
    public static int SlangBlobNew(void** outBlob)
    {
        *outBlob = NativeMemory.Alloc(1);
        return Ok;
    }

    [UnmanagedCallersOnly(EntryPoint = "slangBlobGetSize")]
    public static int SlangBlobGetSize(void* blob) => BlobSizes.TryGetValue((nint)blob, out var size) ? size : 0;

    [UnmanagedCallersOnly(EntryPoint = "slangBlobGetPointer")]
    public static void* SlangBlobGetPointer(void* blob) => blob;

    [UnmanagedCallersOnly(EntryPoint = "slangBlobFree")]
    public static void SlangBlobFree(void* blob)
    {
        BlobSizes.TryRemove((nint)blob, out _);
        NativeMemory.Free(blob);
    }
}
