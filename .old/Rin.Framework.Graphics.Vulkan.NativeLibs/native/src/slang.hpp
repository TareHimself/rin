#pragma once
#include <slang.h>
#include <slang-com-ptr.h>
#include <slang-com-helper.h>
#include <vector>
#include <string>
#include <cstddef>
#include "macro.hpp"


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

using LoadFileFunction = int(RIN_CALLBACK_CONVENTION *)(char* path,char** data);

struct CustomFileSystem final : ISlangFileSystemExt
{
    uint32_t refs;
    LoadFileFunction loadFunction;
    CustomFileSystem(LoadFileFunction load);
    SLANG_NO_THROW SlangResult queryInterface(const SlangUUID& uuid, void** outObject) override;
    SLANG_NO_THROW uint32_t addRef() override;
    SLANG_NO_THROW uint32_t release() override;
    SLANG_NO_THROW void* castAs(const SlangUUID& guid) override;
    SLANG_NO_THROW SlangResult loadFile(const char* path, ISlangBlob** outBlob) override;
    SLANG_NO_THROW SlangResult getFileUniqueIdentity(const char* path, ISlangBlob** outUniqueIdentity) override;
    SLANG_NO_THROW SlangResult calcCombinedPath(SlangPathType fromPathType, const char* fromPath, const char* path, ISlangBlob** pathOut) override;
    SLANG_NO_THROW SlangResult getPathType(const char* path, SlangPathType* pathTypeOut) override;
    SLANG_NO_THROW SlangResult getPath(PathKind kind, const char* path, ISlangBlob** outPath) override;
    SLANG_NO_THROW void clearCache() override;
    SLANG_NO_THROW SlangResult enumeratePathContents(const char* path, FileSystemContentsCallBack callback, void* userData) override;
    SLANG_NO_THROW OSPathKind getOSPathKind() override;
};


struct SessionBuilder
{
    SessionBuilder(LoadFileFunction inLoadFunction);
    LoadFileFunction loadFile;
    std::vector<slang::TargetDesc> targets{};
    std::vector<slang::CompilerOptionEntry> options;
    std::vector<std::pair<std::string,std::string>> preprocessorMacros{};
    std::vector<std::string> searchPaths{};
};

struct Session
{
    Slang::ComPtr<slang::ISession> session{};
    CustomFileSystem* fileSystem{nullptr};
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

enum class ShaderStage : int
{
    Vertex,
    Fragment,
    Compute,
};

RIN_NATIVE_API SessionBuilder * slangSessionBuilderNew(LoadFileFunction loadFileFunction);
RIN_NATIVE_API void slangSessionBuilderAddTargetSpirv(SessionBuilder * builder);
RIN_NATIVE_API void slangSessionBuilderAddTargetGlsl(SessionBuilder * builder);
RIN_NATIVE_API void slangSessionBuilderAddPreprocessorDefinition(SessionBuilder * builder, const char * name, const char * value);
RIN_NATIVE_API void slangSessionBuilderAddSearchPath(SessionBuilder * builder, const char * path);
RIN_NATIVE_API Session * slangSessionBuilderBuild(const SessionBuilder * builder);
RIN_NATIVE_API void slangSessionBuilderFree(const SessionBuilder * builder);

RIN_NATIVE_API void slangSessionClearCache(const Session * session);
RIN_NATIVE_API Module * slangSessionLoadModuleFromSourceString(const Session * session, char * moduleName,char * path,char * string,Blob * outDiagnostics);
RIN_NATIVE_API Component * slangSessionCreateComposedProgram(const Session * session, Module * module, EntryPoint** entryPoints, int entryPointsCount, Blob * outDiagnostics);
RIN_NATIVE_API Blob * slangSessionCompile(const Session * session,const char* compileId,const char* content, const char * entryPointName,int stage,Blob * outDiagnostics);
RIN_NATIVE_API void slangSessionFree(const Session * session);



RIN_NATIVE_API EntryPoint * slangModuleFindEntryPointByName(const Module * module, const char * entryPointName);
RIN_NATIVE_API void slangEntryPointFree(const EntryPoint * entryPoint);
RIN_NATIVE_API void slangModuleFree(const Module * module);

RIN_NATIVE_API Blob * slangComponentGetEntryPointCode(const Component * component,int entryPointIndex,int targetIndex,Blob * outDiagnostics);
RIN_NATIVE_API Component * slangComponentLink(const Component * component,Blob * outDiagnostics);
RIN_NATIVE_API Blob * slangComponentToLayoutJson(const Component * component);
RIN_NATIVE_API void slangComponentFree(const Component * component);

RIN_NATIVE_API Blob * slangBlobNew();
RIN_NATIVE_API int slangBlobGetSize(const Blob * blob);
RIN_NATIVE_API void * slangBlobGetPointer(const Blob * blob);
RIN_NATIVE_API void slangBlobFree(const Blob * blob);