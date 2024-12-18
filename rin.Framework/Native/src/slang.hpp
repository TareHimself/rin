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
    Session(const SessionBuilder * builder);
};

struct Result
{
    Slang::ComPtr<slang::IBlob> data{};
    std::vector<std::string> diagnostics{};
};

EXPORT_DECL SessionBuilder * slangSessionBuilderCreate();
EXPORT_DECL void slangSessionBuilderDestroy(const SessionBuilder * builder);

EXPORT_DECL void slangSessionBuilderAddTargetSpirv(SessionBuilder * builder);
EXPORT_DECL void slangSessionBuilderAddTargetGlsl(SessionBuilder * builder);
EXPORT_DECL void slangSessionBuilderAddPreprocessorDefinition(SessionBuilder * builder, const char * name, const char * value);
EXPORT_DECL void slangSessionBuilderAddSearchPath(SessionBuilder * builder, const char * path);

EXPORT_DECL Session * slangSessionCreate(const SessionBuilder * builder);
EXPORT_DECL void slangSessionDestroy(const SessionBuilder * builder);

EXPORT_DECL Result * slangSessionCompile(const Session * session, const char * moduleName, const char * modulePath, const char * data, const char * entry);

EXPORT_DECL void slangResultDestroy(const Result * result);

EXPORT_DECL int slangResultGetDiagnosticsCount(const Result * result);

EXPORT_DECL char * slangResultGetDiagnostic(const Result * result,int index);

EXPORT_DECL void * slangResultGetPointer(const Result * result);

EXPORT_DECL int slangResultGetSize(const Result * result);