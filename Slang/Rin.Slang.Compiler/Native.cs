using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

// ReSharper disable InconsistentNaming

[assembly: DisableRuntimeMarshalling]

namespace Rin.Slang.Compiler;

internal static partial class Native
{
#if OS_WINDOWS
    private const string DllName = "Rin.Slang.Native";
#else
    private const string DllName = "libRin.Slang.Native";
#endif

    [LibraryImport(DllName)]
    public static unsafe partial void* slangSessionBuilderNew();

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionBuilderAddTargetSpirv(void* builder);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionBuilderAddTargetGlsl(void* builder);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionBuilderAddPreprocessorDefinition(void* builder,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string name,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string value);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionBuilderAddSearchPath(void* builder,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string path);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangSessionBuilderBuild(void* builder);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionBuilderFree(void* builder);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangSessionLoadModuleFromSourceString(void* session,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string moduleName,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string path,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string content, void* outDiagnostics);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangSessionCreateComposedProgram(void* session, void* module,
        nuint* entryPoints, int entryPointsCount, void* outDiagnostics);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionFree(void* session);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangModuleFindEntryPointByName(void* module,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string name);

    [LibraryImport(DllName)]
    public static unsafe partial void slangEntryPointFree(void* entryPoint);

    [LibraryImport(DllName)]
    public static unsafe partial void slangModuleFree(void* module);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangComponentGetEntryPointCode(void* component, int entryPointIndex,
        int targetIndex, void* outDiagnostics);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangComponentLink(void* component, void* outDiagnostics);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangComponentToLayoutJson(void* component);

    [LibraryImport(DllName)]
    public static unsafe partial void slangComponentFree(void* component);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangBlobNew();

    [LibraryImport(DllName)]
    public static unsafe partial int slangBlobGetSize(void* blob);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangBlobGetPointer(void* blob);

    [LibraryImport(DllName)]
    public static unsafe partial void slangBlobFree(void* blob);
}
