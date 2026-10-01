#include "slang.hpp"
#include <array>
#include <fstream>
#include <iostream>
Slang::ComPtr<slang::IGlobalSession> GLOBAL_SESSION;


CustomBlob::~CustomBlob() = default;
SlangResult CustomBlob::queryInterface(const SlangUUID& uuid, void** outObject)
{
    *outObject = getInterface(uuid);
    return SLANG_OK;
}
uint32_t CustomBlob::addRef()
{
    return ++refs;
}
uint32_t CustomBlob::release()
{
    return --refs;
}

ISlangUnknown* CustomBlob::getInterface(const Slang::Guid& guid)
{
    if(guid == ISlangUnknown::getTypeGuid() || guid == ISlangBlob::getTypeGuid())
    {
        return static_cast<ISlangBlob*>(this);
    }
    if(guid == ISlangCastable::getTypeGuid())
    {
        return static_cast<ISlangCastable*>(this);
    }
    return nullptr;
}
void* CustomBlob::castAs(const SlangUUID& guid)
{
    if(const auto inf = getInterface(guid))
    {
        return inf;
    }

    return nullptr;
}
CustomBinaryBlob::CustomBinaryBlob(const std::vector<char>& inData)
{
    data = inData;
}
const void* CustomBinaryBlob::getBufferPointer()
{
    return data.data();
}
size_t CustomBinaryBlob::getBufferSize()
{
    return data.size();
}
CustomStringBlob::CustomStringBlob(const std::string& inData)
{
    data = inData;
}
const void* CustomStringBlob::getBufferPointer()
{
    return data.data();
}
size_t CustomStringBlob::getBufferSize()
{
    return data.size();
}
CustomFileSystem::CustomFileSystem()
{
}
SlangResult CustomFileSystem::queryInterface(const SlangUUID& uuid, void** outObject)
{
    return 1;
}
uint32_t CustomFileSystem::addRef()
{
    return ++refs;
}
uint32_t CustomFileSystem::release()
{
    return --refs;
}

void* CustomFileSystem::castAs(const SlangUUID& guid)
{
    return this;
}

SlangResult CustomFileSystem::loadFile(const char* path, ISlangBlob** outBlob)
{
    return SLANG_FAIL;
    // std::cout << "Loading file " << path << std::endl;
    // char* data = nullptr;
    // std::string str{path};
    // const auto blob = new CustomStringBlob({data, data + size});
    // memoryFree(data);
    // *outBlob = blob;
    // return SLANG_OK;
}
SlangResult CustomFileSystem::getFileUniqueIdentity(const char* path, ISlangBlob** outUniqueIdentity)
{
    *outUniqueIdentity = new CustomStringBlob(path);
    return SLANG_OK;
}
SlangResult CustomFileSystem::calcCombinedPath(SlangPathType fromPathType, const char* fromPath, const char* path, ISlangBlob** pathOut)
{
    std::string asStr{path};
    asStr = "/" + asStr;
    *pathOut = new CustomStringBlob(asStr);
    return SLANG_OK;
}
SlangResult CustomFileSystem::getPathType(const char* path, SlangPathType* pathTypeOut)
{
    std::string str{path};
    if(str.ends_with(".slang"))
    {
        *pathTypeOut = SLANG_PATH_TYPE_FILE;
    }
    else
    {
        *pathTypeOut = SLANG_PATH_TYPE_DIRECTORY;
    }
    return SLANG_OK;
}
SlangResult CustomFileSystem::getPath(PathKind kind, const char* path, ISlangBlob** outPath)
{
    *outPath = new CustomStringBlob(path);
    return SLANG_OK;
}
void CustomFileSystem::clearCache()
{

}
SlangResult CustomFileSystem::enumeratePathContents(const char* path, FileSystemContentsCallBack callback, void* userData)
{
    return SLANG_OK;
}
OSPathKind CustomFileSystem::getOSPathKind()
{
    return OSPathKind::None;
}
SessionBuilder::SessionBuilder()
{
}
// SlangResult CustomFileSystem::queryInterface(const SlangUUID& uuid, void** outObject)
//// {
////     return SLANG_OK;
//// }
//// uint32_t CustomFileSystem::addRef()
//// {
////     return ++refs;
//// }
//// uint32_t CustomFileSystem::release()
//// {
////     return --refs;
//// }
//// void* CustomFileSystem::castAs(const SlangUUID& guid)
//// {
////     return static_cast<void*>(this);
//// }
////
//// SlangResult CustomFileSystem::loadFile(const char* path, ISlangBlob** outBlob)
//// {
////     std::cout << "Loading file: " << path << std::endl;
////     std::ifstream data(path);
//// }
Session::Session(const SessionBuilder* builder)
{
    fileSystem = new CustomFileSystem();
    slang::SessionDesc sessionDesc{};

    sessionDesc.targets = builder->targets.data();
    sessionDesc.targetCount = static_cast<SlangInt>(builder->targets.size());

    std::vector<slang::PreprocessorMacroDesc> preprocessorMacros{};

    preprocessorMacros.reserve(builder->preprocessorMacros.size());

    for(auto& macro : builder->preprocessorMacros)
    {
        preprocessorMacros.emplace_back(macro.first.c_str(),macro.second.c_str());
    }

    auto compilerOptions = builder->options;
    sessionDesc.preprocessorMacros = preprocessorMacros.data();
    sessionDesc.preprocessorMacroCount = static_cast<SlangInt>(preprocessorMacros.size());
    // Row-major to match System.Numerics Matrix4x4, so a raw upload is bit-identical.
    sessionDesc.defaultMatrixLayoutMode = SLANG_MATRIX_LAYOUT_ROW_MAJOR;
    sessionDesc.compilerOptionEntries = compilerOptions.data();
    sessionDesc.compilerOptionEntryCount = static_cast<uint32_t>(compilerOptions.size());
    
    std::vector<const char*> searchPaths{};

    searchPaths.reserve(builder->searchPaths.size());

    for(auto& searchPath : builder->searchPaths)
    {
        searchPaths.emplace_back(searchPath.c_str());
    }

    sessionDesc.searchPaths = searchPaths.data();
    sessionDesc.searchPathCount = static_cast<SlangInt>(searchPaths.size());
    sessionDesc.fileSystem = fileSystem;
    //sessionDesc.fileSystem = new FileS
    //sessionDesc.fileSystem = new FileSystem()
    GLOBAL_SESSION->createSession(sessionDesc,session.writeRef());
}
Session::~Session()
= default;

SessionBuilder* slangSessionBuilderNew()
{
    if(!GLOBAL_SESSION)
    {
        slang::createGlobalSession(GLOBAL_SESSION.writeRef());
    }
    return new SessionBuilder();
}

void slangSessionBuilderFree(const SessionBuilder* builder)
{
    delete builder;
}
Module* slangSessionLoadModuleFromSourceString(const Session* session, char* moduleName, char* path, char* string, Blob* outDiagnostics)
{
    try
    {
        Slang::ComPtr<slang::IBlob> diagnostics;
        Slang::ComPtr<slang::IModule> module;
        module = session->session->loadModuleFromSourceString(moduleName,path,string,diagnostics.writeRef());
        if(outDiagnostics)
        {
            outDiagnostics->blob = diagnostics;
        }
        if(module)
        {
            return new Module{module};
        }
        return nullptr;
    }
    catch(std::exception& e)
    {
        std::cout << "EXCEPTION: " << e.what() << std::endl;
    }
    return nullptr;
}

Component* slangSessionCreateComposedProgram(const Session* session, Module* module, EntryPoint** entryPoints, int entryPointsCount, Blob* outDiagnostics)
{
    std::vector<slang::IComponentType*> componentTypes{};
    componentTypes.reserve(entryPointsCount + 1);
    componentTypes.push_back(module->module);
    for(auto i = 0; i < entryPointsCount; ++i)
    {
        componentTypes.push_back(entryPoints[i]->entryPoint);
    }
    Slang::ComPtr<slang::IComponentType> composedProgram;

    Slang::ComPtr<slang::IBlob> diagnostics;
    auto operationResult = session->session->createCompositeComponentType(
        componentTypes.data(),
        static_cast<SlangInt>(componentTypes.size()),
        composedProgram.writeRef(),
        diagnostics.writeRef());

    if(outDiagnostics)
    {
        outDiagnostics->blob = diagnostics;
    }

    if(SLANG_FAILED(operationResult))
    {
        return nullptr;
    }

    return new Component{composedProgram};
}
void slangSessionFree(const Session* session)
{
    delete session;
}

void slangSessionBuilderAddTargetSpirv(SessionBuilder* builder)
{
    slang::TargetDesc desc{};
    desc.format = SLANG_SPIRV;
    desc.profile = GLOBAL_SESSION->findProfile("spirv_1_5");
    
    builder->options.push_back(
        {slang::CompilerOptionName::DebugInformation,
         {slang::CompilerOptionValueKind::Int, 1, 0, nullptr, nullptr}});
    builder->options.push_back(
        {slang::CompilerOptionName::EmitSpirvDirectly,
         {slang::CompilerOptionValueKind::Int, SLANG_DEBUG_INFO_LEVEL_STANDARD, 0, nullptr, nullptr}});
    // Spirv-OPT error https://github.com/KhronosGroup/SPIRV-Tools/issues/5959
    builder->options.push_back(
       {slang::CompilerOptionName::Optimization,
        {slang::CompilerOptionValueKind::Int, SLANG_OPTIMIZATION_LEVEL_NONE, 0, nullptr, nullptr}});
    builder->targets.push_back(desc);
}

void slangSessionBuilderAddTargetGlsl(SessionBuilder* builder)
{
    slang::TargetDesc desc{};
    desc.format = SLANG_GLSL;
    desc.profile = GLOBAL_SESSION->findProfile("glsl_450");
    builder->targets.push_back(desc);
}

void slangSessionBuilderAddTargetHostCallable(SessionBuilder* builder)
{
    slang::TargetDesc desc{};
    desc.format = SLANG_SHADER_HOST_CALLABLE;
    builder->targets.push_back(desc);
}

void slangSessionBuilderAddPreprocessorDefinition(SessionBuilder* builder, const char* name, const char* value)
{
    builder->preprocessorMacros.emplace_back(std::make_pair<std::string,std::string>(name,value));
}

void slangSessionBuilderAddSearchPath(SessionBuilder* builder, const char* path)
{
    builder->searchPaths.emplace_back(path);
}

Session* slangSessionBuilderBuild(const SessionBuilder* builder)
{
    return new Session(builder);
}

EntryPoint* slangModuleFindEntryPointByName(const Module* module, const char* entryPointName)
{
    Slang::ComPtr<slang::IEntryPoint> entryPoint;
    module->module->findEntryPointByName(entryPointName,entryPoint.writeRef());
    if(entryPoint)
    {
        return new EntryPoint{entryPoint};
    }

    return nullptr;
}
void slangEntryPointFree(const EntryPoint* entryPoint)
{
    delete entryPoint;
}
void slangModuleFree(const Module* module)
{
    delete module;
}

Blob* slangComponentGetEntryPointCode(const Component* component, int entryPointIndex, int targetIndex, Blob* outDiagnostics)
{
    Slang::ComPtr<slang::IBlob> code;

    Slang::ComPtr<slang::IBlob> diagnostics;

    try
    {
        component->component->getEntryPointCode(entryPointIndex,targetIndex,code.writeRef(),diagnostics.writeRef());
    }
    catch(std::exception& e)
    {
        std::cout << "EXCEPTION: " << e.what() << std::endl;
    }

    if(outDiagnostics != nullptr)
    {
        outDiagnostics->blob = diagnostics;
    }

    if(!code)
    {
        std::cout << "FAILED TO GET ENTRY POINT: " << std::string{(char*)diagnostics->getBufferPointer(),(char*)diagnostics->getBufferPointer() + diagnostics->getBufferSize()} << std::endl;
        return nullptr;
    }

    return new Blob{code};
}
#ifdef _MSC_VER
#include <windows.h>
#include <typeinfo>
// MSVC lays a thrown C++ object's type information out as image-relative offsets; just enough of it is
// declared here to read the name of the thrown type when an exception escapes from inside Slang.
struct RinCatchableType { unsigned int properties; int pType; int thisDisplacement[3]; int sizeOrOffset; int copyFunction; };
struct RinCatchableTypeArray { int nCatchableTypes; int arrayOfCatchableTypes[1]; };
struct RinThrowInfo { unsigned int attributes; int pmfnUnwind; int pForwardCompat; int pCatchableTypeArray; };

static int CaptureException(EXCEPTION_POINTERS* pointers, unsigned long* outCode, char* outTypeName, size_t outTypeNameSize)
{
    *outCode = pointers->ExceptionRecord->ExceptionCode;
    if(*outCode == 0xE06D7363 && pointers->ExceptionRecord->NumberParameters >= 4)
    {
        const uintptr_t base = pointers->ExceptionRecord->ExceptionInformation[3];
        const auto* throwInfo = reinterpret_cast<const RinThrowInfo*>(pointers->ExceptionRecord->ExceptionInformation[2]);
        const auto* types = reinterpret_cast<const RinCatchableTypeArray*>(base + throwInfo->pCatchableTypeArray);
        if(types->nCatchableTypes > 0)
        {
            const auto* type = reinterpret_cast<const RinCatchableType*>(base + types->arrayOfCatchableTypes[0]);
            const auto* info = reinterpret_cast<const std::type_info*>(base + type->pType);
            strncpy_s(outTypeName, outTypeNameSize, info->name(), _TRUNCATE);
        }
    }
    return EXCEPTION_EXECUTE_HANDLER;
}

static SlangResult CallGetEntryPointHostCallable(slang::IComponentType* component, int entryPointIndex, int targetIndex, ISlangSharedLibrary** outLibrary, slang::IBlob** outDiagnostics, unsigned long* outExceptionCode, char* outTypeName, size_t outTypeNameSize)
{
    __try
    {
        return component->getEntryPointHostCallable(entryPointIndex, targetIndex, outLibrary, outDiagnostics);
    }
    __except(CaptureException(GetExceptionInformation(), outExceptionCode, outTypeName, outTypeNameSize))
    {
        return SLANG_FAIL;
    }
}
#else
static SlangResult CallGetEntryPointHostCallable(slang::IComponentType* component, int entryPointIndex, int targetIndex, ISlangSharedLibrary** outLibrary, slang::IBlob** outDiagnostics, unsigned long*, char*, size_t)
{
    return component->getEntryPointHostCallable(entryPointIndex, targetIndex, outLibrary, outDiagnostics);
}
#endif

SharedLibrary* slangComponentGetEntryPointHostCallable(const Component* component, int entryPointIndex, int targetIndex, Blob* outDiagnostics)
{
    Slang::ComPtr<ISlangSharedLibrary> library;
    Slang::ComPtr<slang::IBlob> diagnostics;

    unsigned long exceptionCode = 0;
    char exceptionType[256] = {};
    const SlangResult result = CallGetEntryPointHostCallable(component->component.get(), entryPointIndex, targetIndex, library.writeRef(), diagnostics.writeRef(), &exceptionCode, exceptionType, sizeof(exceptionType));

    if(exceptionCode != 0)
    {
        // A fatal compile error has already been reported into the diagnostics and aborted with an
        // exception; hand those over, falling back to the exception's own type if there are none.
        if(outDiagnostics != nullptr)
        {
            if(diagnostics && diagnostics->getBufferSize() > 0)
            {
                outDiagnostics->blob = diagnostics;
            }
            else
            {
                outDiagnostics->blob = new CustomStringBlob("native exception code " + std::to_string(exceptionCode) + " type " + std::string(exceptionType));
            }
        }
        return nullptr;
    }

    if(outDiagnostics != nullptr)
    {
        outDiagnostics->blob = diagnostics;
    }

    if(SLANG_FAILED(result) || !library)
    {
        return nullptr;
    }

    return new SharedLibrary{library};
}

void* slangSharedLibraryFindFunc(const SharedLibrary* library, const char* name)
{
    return library->library->findFuncByName(name);
}

void slangSharedLibraryFree(const SharedLibrary* library)
{
    delete library;
}

Component* slangComponentLink(const Component* component, Blob* outDiagnostics)
{
    Slang::ComPtr<slang::IComponentType> outComponent;

    Slang::ComPtr<slang::IBlob> diagnostics;

    component->component->link(outComponent.writeRef(),diagnostics.writeRef());

    if(outDiagnostics)
    {
        outDiagnostics->blob = diagnostics;
    }

    return new Component{outComponent};
}

Blob* slangComponentToLayoutJson(const Component* component)
{
    Slang::ComPtr<slang::IBlob> code;

    component->component->getLayout()->toJson(code.writeRef());
    
    return new Blob{code};
}

void slangComponentFree(const Component* component)
{
    delete component;
}
Blob* slangBlobNew()
{
    return new Blob{};
}

int slangBlobGetSize(const Blob* blob)
{
    return static_cast<int>(blob->blob->getBufferSize());
}

void* slangBlobGetPointer(const Blob* blob)
{
    return const_cast<void*>(blob->blob->getBufferPointer());
}

void slangBlobFree(const Blob* blob)
{
    delete blob;
}
