#pragma once
#include <slang.h>
#include <slang-com-ptr.h>
#include <slang-com-helper.h>
#include <vector>
#include <string>
#include <cstddef>
#include "macro.hpp"
#include "result.hpp"


// /** The size of this structure, in bytes.
//      */
//     size_t structureSize = sizeof(SessionDesc);
//
// /** Code generation targets to include in the session.
//  */
// TargetDesc const* targets = nullptr;
// SlangInt targetCount = 0;
//
// /** Flags to configure the session.
//  */
// SessionFlags flags = kSessionFlags_None;
//
// /** Default layout to assume for variables with matrix types.
//  */
// SlangMatrixLayoutMode defaultMatrixLayoutMode = SLANG_MATRIX_LAYOUT_ROW_MAJOR;
//
// /** Paths to use when searching for `#include`d or `import`ed files.
//  */
// char const* const* searchPaths = nullptr;
// SlangInt searchPathCount = 0;
//
// PreprocessorMacroDesc const* preprocessorMacros = nullptr;
// SlangInt preprocessorMacroCount = 0;
//
// ISlangFileSystem* fileSystem = nullptr;
//
// bool enableEffectAnnotations = false;
// bool allowGLSLSyntax = false;
//
// /** Pointer to an array of compiler option entries, whose size is compilerOptionEntryCount.
//  */
// CompilerOptionEntry* compilerOptionEntries = nullptr;
//
// /** Number of additional compiler option entries.
//  */
// uint32_t compilerOptionEntryCount = 0;


struct CustomBlob : ISlangBlob, ISlangCastable
{
    virtual ~CustomBlob();

    std::atomic<uint32_t> refs{0};
    SLANG_NO_THROW SlangResult queryInterface(const SlangUUID& uuid, void** outObject) override;
    SLANG_NO_THROW uint32_t addRef() override;
    SLANG_NO_THROW uint32_t release() override;
    ISlangUnknown* getInterface(const Slang::Guid& guid);
    SLANG_NO_THROW void* castAs(const SlangUUID& guid) override;
    
};

struct CustomBinaryBlob : CustomBlob
{
    std::vector<char> data{};
    explicit CustomBinaryBlob(const std::vector<char>& inData);
    SLANG_NO_THROW const void* getBufferPointer() override;
    SLANG_NO_THROW size_t getBufferSize() override;
};

struct CustomStringBlob : CustomBlob
{
    std::string data{};
    explicit CustomStringBlob(const std::string& inData);
    SLANG_NO_THROW const void* getBufferPointer() override;
    SLANG_NO_THROW size_t getBufferSize() override;
};

using LoadFileFunction = int(RIN_CALLBACK_CONVENTION *)(char* path,char** data,void* userData);

// Slang's global session is not thread-safe, so every session gets its own and sessions can run on
// separate threads.
struct SessionBuilder
{
    SessionBuilder();
    Slang::ComPtr<slang::IGlobalSession> globalSession{};
    std::vector<slang::TargetDesc> targets{};
    std::vector<slang::CompilerOptionEntry> options;
    std::vector<std::pair<std::string,std::string>> preprocessorMacros{};
    std::vector<std::string> searchPaths{};
};

struct Session
{
    Slang::ComPtr<slang::IGlobalSession> globalSession{};
    Slang::ComPtr<slang::ISession> session{};
    explicit Session(const SessionBuilder * builder);
    ~Session();
};

struct Module
{
    Slang::ComPtr<slang::IModule> module{};
};

struct EntryPoint
{
    Slang::ComPtr<slang::IEntryPoint> entryPoint{};
};

struct Blob
{
    Slang::ComPtr<slang::IBlob> blob{};
};

struct Component
{
    Slang::ComPtr<slang::IComponentType> component{};
};

struct SharedLibrary
{
    Slang::ComPtr<ISlangSharedLibrary> library{};
};

enum class ShaderStage : int
{
    Vertex,
    Fragment,
    Compute,
};

RIN_NATIVE_API SlangNativeResult slangSessionBuilderNew(SessionBuilder ** outBuilder);
RIN_NATIVE_API void slangSessionBuilderAddTargetSpirv(SessionBuilder * builder);
RIN_NATIVE_API void slangSessionBuilderAddTargetGlsl(SessionBuilder * builder);
RIN_NATIVE_API void slangSessionBuilderAddTargetHostCallable(SessionBuilder * builder);
RIN_NATIVE_API void slangSessionBuilderAddPreprocessorDefinition(SessionBuilder * builder, const char * name, const char * value);
RIN_NATIVE_API void slangSessionBuilderAddSearchPath(SessionBuilder * builder, const char * path);
RIN_NATIVE_API SlangNativeResult slangSessionBuilderBuild(const SessionBuilder * builder, Session ** outSession);
RIN_NATIVE_API void slangSessionBuilderFree(const SessionBuilder * builder);

RIN_NATIVE_API SlangNativeResult slangSessionLoadModuleFromSourceString(const Session * session, char * moduleName,char * path,char * string,Blob * outDiagnostics, Module ** outModule);
RIN_NATIVE_API SlangNativeResult slangSessionCreateComposedProgram(const Session * session, Module * module, EntryPoint** entryPoints, int entryPointsCount, Blob * outDiagnostics, Component ** outComponent);
RIN_NATIVE_API void slangSessionFree(const Session * session);



RIN_NATIVE_API SlangNativeResult slangModuleFindEntryPointByName(const Module * module, const char * entryPointName, EntryPoint ** outEntryPoint);
RIN_NATIVE_API void slangEntryPointFree(const EntryPoint * entryPoint);
RIN_NATIVE_API void slangModuleFree(const Module * module);

RIN_NATIVE_API SlangNativeResult slangComponentGetEntryPointCode(const Component * component,int entryPointIndex,int targetIndex,Blob * outDiagnostics, Blob ** outCode);
RIN_NATIVE_API SlangNativeResult slangComponentGetEntryPointHostCallable(const Component * component,int entryPointIndex,int targetIndex,Blob * outDiagnostics, SharedLibrary ** outLibrary);
RIN_NATIVE_API void * slangSharedLibraryFindFunc(const SharedLibrary * library, const char * name);
RIN_NATIVE_API void slangSharedLibraryFree(const SharedLibrary * library);
RIN_NATIVE_API SlangNativeResult slangComponentLink(const Component * component,Blob * outDiagnostics, Component ** outLinked);
RIN_NATIVE_API SlangNativeResult slangComponentToLayoutJson(const Component * component, Blob ** outJson);
RIN_NATIVE_API void slangComponentFree(const Component * component);

RIN_NATIVE_API SlangNativeResult slangBlobNew(Blob ** outBlob);
RIN_NATIVE_API int slangBlobGetSize(const Blob * blob);
RIN_NATIVE_API void * slangBlobGetPointer(const Blob * blob);
RIN_NATIVE_API void slangBlobFree(const Blob * blob);