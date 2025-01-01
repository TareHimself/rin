#pragma once
#include <slang.h>
#include <slang-com-ptr.h>
#include <slang-com-helper.h>
#include <slang-com-helper.h>
#include <vector>
#include <string>
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

struct SessionBuilder
{
    std::vector<slang::TargetDesc> targets{};
    std::vector<std::pair<std::string,std::string>> preprocessorMacros{};
    std::vector<std::string> searchPaths{};
};

struct Session
{
    Slang::ComPtr<slang::ISession> session{};
    explicit Session(const SessionBuilder * builder);
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

EXPORT_DECL SessionBuilder * slangSessionBuilderNew();
EXPORT_DECL void slangSessionBuilderAddTargetSpirv(SessionBuilder * builder);
EXPORT_DECL void slangSessionBuilderAddTargetGlsl(SessionBuilder * builder);
EXPORT_DECL void slangSessionBuilderAddPreprocessorDefinition(SessionBuilder * builder, const char * name, const char * value);
EXPORT_DECL void slangSessionBuilderAddSearchPath(SessionBuilder * builder, const char * path);
EXPORT_DECL Session * slangSessionBuilderBuild(const SessionBuilder * builder);
EXPORT_DECL void slangSessionBuilderFree(const SessionBuilder * builder);

EXPORT_DECL Module * slangSessionLoadModuleFromSourceString(const Session * session, const char * moduleName, const char * path, const char * string,Blob * outDiagnostics);
EXPORT_DECL Component * slangSessionCreateComposedProgram(const Session * session,Module * module,EntryPoint * entryPoint,Blob * outDiagnostics);
EXPORT_DECL void slangSessionFree(const Session * session);

EXPORT_DECL EntryPoint * slangModuleFindEntryPointByName(const Module * module, const char * entryPointName);
EXPORT_DECL void slangEntryPointFree(const EntryPoint * entryPoint);
EXPORT_DECL void slangModuleFree(const Module * module);

EXPORT_DECL Blob * slangComponentGetEntryPointCode(const Component * component,int entryPointIndex,int targetIndex,Blob * outDiagnostics);
EXPORT_DECL Component * slangComponentLink(const Component * component,Blob * outDiagnostics);
EXPORT_DECL Blob * slangComponentToLayoutJson(const Component * component);
EXPORT_DECL void slangComponentFree(const Component * component);

EXPORT_DECL Blob * slangBlobNew();
EXPORT_DECL int slangBlobGetSize(const Blob * blob);
EXPORT_DECL void * slangBlobGetPointer(const Blob * blob);
EXPORT_DECL void slangBlobFree(const Blob * blob);