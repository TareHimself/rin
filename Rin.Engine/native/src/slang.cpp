#include "slang.hpp"
#include <array>
#include <fstream>
#include <iostream>

Slang::ComPtr<slang::IGlobalSession> GLOBAL_SESSION;


CustomBlob::~CustomBlob()
= default;
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
    if(auto inf = getInterface(guid))
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
SlangResult CustomFileSystem::queryInterface(const SlangUUID& uuid, void** outObject)
{
    return 1;
}
uint32_t CustomFileSystem::addRef()
{
    return 1;
}
uint32_t CustomFileSystem::release()
{
    return 1;
}

void* CustomFileSystem::castAs(const SlangUUID& guid)
{
    return this;
}

SlangResult CustomFileSystem::loadFile(const char* path, ISlangBlob** outBlob)
{
    std::cout << "Loading file " << path << std::endl;
    std::ifstream fileStream(path,std::ios::binary);
    *outBlob = new CustomStringBlob({
        std::istreambuf_iterator<char>(fileStream),
        std::istreambuf_iterator<char>()
    });
    return SLANG_OK;
}
SlangResult CustomFileSystem::getFileUniqueIdentity(const char* path, ISlangBlob** outUniqueIdentity)
{
    *outUniqueIdentity = new CustomStringBlob(path);
    return SLANG_OK;
}
SlangResult CustomFileSystem::calcCombinedPath(SlangPathType fromPathType, const char* fromPath, const char* path, ISlangBlob** pathOut)
{
    *pathOut = new CustomStringBlob(path);
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
    return SLANG_FAIL;
}
OSPathKind CustomFileSystem::getOSPathKind()
{
    return OSPathKind::None;
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

    sessionDesc.preprocessorMacros = preprocessorMacros.data();
    sessionDesc.preprocessorMacroCount = static_cast<SlangInt>(preprocessorMacros.size());
    sessionDesc.defaultMatrixLayoutMode = SLANG_MATRIX_LAYOUT_COLUMN_MAJOR;

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
{
    delete fileSystem;
}

EXPORT_IMPL SessionBuilder* slangSessionBuilderNew()
{
    if(!GLOBAL_SESSION)
    {
        slang::createGlobalSession(GLOBAL_SESSION.writeRef());
    }
    return new SessionBuilder();
}

EXPORT_IMPL void slangSessionBuilderFree(const SessionBuilder* builder)
{
    delete builder;
}
EXPORT_IMPL Module* slangSessionLoadModuleFromSourceString(const Session* session, char* moduleName, char* path, char* string, Blob* outDiagnostics)
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

EXPORT_IMPL Component* slangSessionCreateComposedProgram(const Session* session, Module* module, EntryPoint** entryPoints, int entryPointsCount, Blob* outDiagnostics)
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
EXPORT_IMPL void slangSessionFree(const Session* session)
{
    delete session;
}

EXPORT_IMPL void slangSessionBuilderAddTargetSpirv(SessionBuilder* builder)
{
    slang::TargetDesc desc{};
    desc.format = SLANG_SPIRV;
    desc.profile = GLOBAL_SESSION->findProfile("spirv_1_5");
    builder->targets.push_back(desc);
}

EXPORT_IMPL void slangSessionBuilderAddTargetGlsl(SessionBuilder* builder)
{
    slang::TargetDesc desc{};
    desc.format = SLANG_GLSL;
    desc.profile = GLOBAL_SESSION->findProfile("glsl_450");
    builder->targets.push_back(desc);
}

EXPORT_IMPL void slangSessionBuilderAddPreprocessorDefinition(SessionBuilder* builder, const char* name, const char* value)
{
    builder->preprocessorMacros.emplace_back(std::make_pair<std::string,std::string>(name,value));
}

EXPORT_IMPL void slangSessionBuilderAddSearchPath(SessionBuilder* builder, const char* path)
{
    builder->searchPaths.emplace_back(path);
}

EXPORT_IMPL Session* slangSessionBuilderBuild(const SessionBuilder* builder)
{
    return new Session(builder);
}

EXPORT_IMPL EntryPoint* slangModuleFindEntryPointByName(const Module* module, const char* entryPointName)
{
    Slang::ComPtr<slang::IEntryPoint> entryPoint;
    module->module->findEntryPointByName(entryPointName,entryPoint.writeRef());

    if(entryPoint)
    {
        return new EntryPoint{entryPoint};
    }

    return nullptr;
}
EXPORT_IMPL void slangEntryPointFree(const EntryPoint* entryPoint)
{
    delete entryPoint;
}
EXPORT_IMPL void slangModuleFree(const Module* module)
{
    delete module;
}

EXPORT_IMPL Blob* slangComponentGetEntryPointCode(const Component* component, int entryPointIndex, int targetIndex, Blob* outDiagnostics)
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

    return new Blob{code};
}
EXPORT_IMPL Component* slangComponentLink(const Component* component, Blob* outDiagnostics)
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

EXPORT_IMPL Blob* slangComponentToLayoutJson(const Component* component)
{
    Slang::ComPtr<slang::IBlob> code;

    component->component->getLayout()->toJson(code.writeRef());
    return new Blob{code};
}

EXPORT_IMPL void slangComponentFree(const Component* component)
{
    delete component;
}
EXPORT_IMPL Blob* slangBlobNew()
{
    return new Blob{};
}

EXPORT_IMPL int slangBlobGetSize(const Blob* blob)
{
    return static_cast<int>(blob->blob->getBufferSize());
}

EXPORT_IMPL void* slangBlobGetPointer(const Blob* blob)
{
    return const_cast<void*>(blob->blob->getBufferPointer());
}

EXPORT_IMPL void slangBlobFree(const Blob* blob)
{
    delete blob;
}
