#include "slang.hpp"
#include <array>
#include <iostream>

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
    sessionDesc.defaultMatrixLayoutMode = SLANG_MATRIX_LAYOUT_COLUMN_MAJOR;

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

EXPORT_IMPL void slangSessionBuilderFree(const SessionBuilder* builder)
{
    delete builder;
}
EXPORT_IMPL Module* slangSessionLoadModuleFromSourceString(const Session* session, char* moduleName, char* path,char* string, Blob* outDiagnostics)
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
        
        if (module) {
            return new Module{ module };
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

EXPORT_IMPL Blob* slangComponentGetEntryPointCode(const Component* component, int entryPointIndex, int targetIndex, Blob * outDiagnostics)
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

