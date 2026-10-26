using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

// ReSharper disable InconsistentNaming

[assembly: DisableRuntimeMarshalling]

namespace Rin.Slang.Compiler;

internal static partial class Native
{
    private const string DllName = "Rin.Slang.Native";

    private static void Check(SlangNativeResult result, string function)
    {
        if (result != SlangNativeResult.Ok) throw new SlangNativeException(function, result);
    }

    // Shader problems come back as null with the details in the diagnostics blob. Anything else throws.
    private static bool Succeeded(SlangNativeResult result, string function)
    {
        switch (result)
        {
            case SlangNativeResult.Ok:
                return true;
            case SlangNativeResult.CompileFailed:
            case SlangNativeResult.EntryPointNotFound:
            case SlangNativeResult.CompositionFailed:
            case SlangNativeResult.LinkFailed:
                return false;
            default:
                throw new SlangNativeException(function, result);
        }
    }

    public static unsafe void* SessionBuilderNew()
    {
        void* builder;
        Check(slangSessionBuilderNew(&builder), nameof(slangSessionBuilderNew));
        return builder;
    }

    public static unsafe void* SessionBuilderBuild(void* builder)
    {
        void* session;
        Check(slangSessionBuilderBuild(builder, &session), nameof(slangSessionBuilderBuild));
        return session;
    }

    public static unsafe void* LoadModuleFromSourceString(void* session, string moduleName, string path,
        string content, void* outDiagnostics)
    {
        void* module;
        var result = slangSessionLoadModuleFromSourceString(session, moduleName, path, content, outDiagnostics, &module);
        return Succeeded(result, nameof(slangSessionLoadModuleFromSourceString)) ? module : null;
    }

    public static unsafe void* CreateComposedProgram(void* session, void* module, nuint* entryPoints,
        int entryPointsCount, void* outDiagnostics)
    {
        void* component;
        var result = slangSessionCreateComposedProgram(session, module, entryPoints, entryPointsCount, outDiagnostics,
            &component);
        return Succeeded(result, nameof(slangSessionCreateComposedProgram)) ? component : null;
    }

    public static unsafe void* FindEntryPointByName(void* module, string name)
    {
        void* entryPoint;
        var result = slangModuleFindEntryPointByName(module, name, &entryPoint);
        return Succeeded(result, nameof(slangModuleFindEntryPointByName)) ? entryPoint : null;
    }

    public static unsafe void* GetEntryPointCode(void* component, int entryPointIndex, int targetIndex,
        void* outDiagnostics)
    {
        void* code;
        var result = slangComponentGetEntryPointCode(component, entryPointIndex, targetIndex, outDiagnostics, &code);
        return Succeeded(result, nameof(slangComponentGetEntryPointCode)) ? code : null;
    }

    public static unsafe void* GetEntryPointHostCallable(void* component, int entryPointIndex, int targetIndex,
        void* outDiagnostics)
    {
        void* library;
        var result = slangComponentGetEntryPointHostCallable(component, entryPointIndex, targetIndex, outDiagnostics,
            &library);
        return Succeeded(result, nameof(slangComponentGetEntryPointHostCallable)) ? library : null;
    }

    public static unsafe void* Link(void* component, void* outDiagnostics)
    {
        void* linked;
        var result = slangComponentLink(component, outDiagnostics, &linked);
        return Succeeded(result, nameof(slangComponentLink)) ? linked : null;
    }

    public static unsafe void* ToLayoutJson(void* component)
    {
        void* json;
        Check(slangComponentToLayoutJson(component, &json), nameof(slangComponentToLayoutJson));
        return json;
    }

    public static unsafe void* BlobNew()
    {
        void* blob;
        Check(slangBlobNew(&blob), nameof(slangBlobNew));
        return blob;
    }

    [LibraryImport(DllName)]
    private static unsafe partial SlangNativeResult slangSessionBuilderNew(void** outBuilder);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionBuilderAddTargetSpirv(void* builder);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionBuilderAddTargetGlsl(void* builder);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionBuilderAddTargetHostCallable(void* builder);

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
    private static unsafe partial SlangNativeResult slangSessionBuilderBuild(void* builder, void** outSession);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionBuilderFree(void* builder);

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    private static unsafe partial SlangNativeResult slangSessionLoadModuleFromSourceString(void* session,
        string moduleName, string path, string content, void* outDiagnostics, void** outModule);

    [LibraryImport(DllName)]
    private static unsafe partial SlangNativeResult slangSessionCreateComposedProgram(void* session, void* module,
        nuint* entryPoints, int entryPointsCount, void* outDiagnostics, void** outComponent);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSessionFree(void* session);

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    private static unsafe partial SlangNativeResult slangModuleFindEntryPointByName(void* module, string name,
        void** outEntryPoint);

    [LibraryImport(DllName)]
    public static unsafe partial void slangEntryPointFree(void* entryPoint);

    [LibraryImport(DllName)]
    public static unsafe partial void slangModuleFree(void* module);

    [LibraryImport(DllName)]
    private static unsafe partial SlangNativeResult slangComponentGetEntryPointCode(void* component, int entryPointIndex,
        int targetIndex, void* outDiagnostics, void** outCode);

    [LibraryImport(DllName)]
    private static unsafe partial SlangNativeResult slangComponentGetEntryPointHostCallable(void* component,
        int entryPointIndex, int targetIndex, void* outDiagnostics, void** outLibrary);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangSharedLibraryFindFunc(void* library,
        [MarshalUsing(typeof(Utf8StringMarshaller))]
        string name);

    [LibraryImport(DllName)]
    public static unsafe partial void slangSharedLibraryFree(void* library);

    [LibraryImport(DllName)]
    private static unsafe partial SlangNativeResult slangComponentLink(void* component, void* outDiagnostics,
        void** outLinked);

    [LibraryImport(DllName)]
    private static unsafe partial SlangNativeResult slangComponentToLayoutJson(void* component, void** outJson);

    [LibraryImport(DllName)]
    public static unsafe partial void slangComponentFree(void* component);

    [LibraryImport(DllName)]
    private static unsafe partial SlangNativeResult slangBlobNew(void** outBlob);

    [LibraryImport(DllName)]
    public static unsafe partial int slangBlobGetSize(void* blob);

    [LibraryImport(DllName)]
    public static unsafe partial void* slangBlobGetPointer(void* blob);

    [LibraryImport(DllName)]
    public static unsafe partial void slangBlobFree(void* blob);
}
