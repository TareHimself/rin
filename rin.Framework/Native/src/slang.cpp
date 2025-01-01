#include "slang.hpp"
#include <array>

Slang::ComPtr<slang::IGlobalSession> GLOBAL_SESSION;

Session::Session(const SessionBuilder* builder)
{
    slang::SessionDesc sessionDesc{};
    
    sessionDesc.targets = builder->targets.data();
    sessionDesc.targetCount = static_cast<SlangInt>(builder->targets.size());
    
    std::vector<slang::PreprocessorMacroDesc> preprocessorMacros{};
    
    preprocessorMacros.reserve(builder->preprocessorMacros.size());
    
    for (auto& macro : builder->preprocessorMacros)
    {
        preprocessorMacros.emplace_back(macro.first.c_str(),macro.second.c_str());
    }

    sessionDesc.preprocessorMacros = preprocessorMacros.data();
    sessionDesc.preprocessorMacroCount = static_cast<SlangInt>(preprocessorMacros.size());

    std::vector<const char*> searchPaths{};

    searchPaths.reserve(builder->searchPaths.size());
    
    for (auto& searchPath : builder->searchPaths)
    {
        searchPaths.emplace_back(searchPath.c_str());
    }

    sessionDesc.searchPaths = searchPaths.data();
    sessionDesc.searchPathCount = static_cast<SlangInt>(searchPaths.size());
    
    GLOBAL_SESSION->createSession(sessionDesc,session.writeRef()); 
}

EXPORT_IMPL SessionBuilder* slangSessionBuilderNew()
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
Module* slangSessionLoadModuleFromSourceString(const Session* session, const char* moduleName, const char* path, const char* string, Blob* outDiagnostics)
{
    Slang::ComPtr<slang::IBlob> diagnostics;
    Slang::ComPtr<slang::IModule> module;
    module = session->session->loadModuleFromSourceString(moduleName,path,string,diagnostics.writeRef());
    if(outDiagnostics)
    {
        outDiagnostics->blob = diagnostics;
    }
    
    return new Module{module};
}

Component* slangSessionCreateComposedProgram(const Session* session, Module* module, EntryPoint* entryPoint, Blob* outDiagnostics)
{
    std::array<slang::IComponentType*,2> componentTypes =
                {
        module->module,
        entryPoint->entryPoint
    };
    Slang::ComPtr<slang::IComponentType> composedProgram;
    
    Slang::ComPtr<slang::IBlob> diagnostics;
    
    auto operationResult = session->session->createCompositeComponentType(
        componentTypes.data(),
        componentTypes.size(),
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

EXPORT_IMPL void slangSessionFree(const SessionBuilder* builder)
{
    delete builder;
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
void slangModuleFree(const Module* module)
{
    delete module;
}

Blob* slangComponentGetEntryPointCode(const Component* component, int entryPointIndex, int targetIndex, Blob * outDiagnostics)
{
    Slang::ComPtr<slang::IBlob> code;
    
    Slang::ComPtr<slang::IBlob> diagnostics;
    
    component->component->getEntryPointCode(entryPointIndex,targetIndex,code.writeRef(),diagnostics.writeRef());
    
    if(outDiagnostics)
    {
        outDiagnostics->blob = diagnostics;
    }
    
    return new Blob{code};
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

