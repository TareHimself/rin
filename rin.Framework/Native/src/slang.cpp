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

EXPORT_IMPL SessionBuilder* slangSessionBuilderCreate()
{
    if(!GLOBAL_SESSION)
    {
        slang::createGlobalSession(GLOBAL_SESSION.writeRef());
    }
    return new SessionBuilder();
}

void slangSessionBuilderDestroy(const SessionBuilder* builder)
{
    delete builder;
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

EXPORT_IMPL Session* slangSessionCreate(const SessionBuilder* builder)
{
    return new Session(builder);
}

EXPORT_IMPL void slangSessionDestroy(const SessionBuilder* builder)
{
    delete builder;
}

EXPORT_IMPL Result* slangSessionCompile(const Session* session, const char* moduleName, const char* modulePath, const char* data, const char* entry)
{
    const auto result = new Result();
    
    Slang::ComPtr<slang::IModule> slangModule;
    {
        Slang::ComPtr<slang::IBlob> diagnostics{};
        // const char* moduleName = "shortest";
        // const char* modulePath = "shortest.slang";
        slangModule = session->session->loadModuleFromSourceString(moduleName,modulePath,data,diagnostics.writeRef());
        
        if(diagnostics)
        {
            result->diagnostics.emplace_back(static_cast<const char *>(diagnostics->getBufferPointer()));
        }
        
        if (!slangModule)
        {
            result->diagnostics.emplace_back("failed to load module");
            return result;
        }
    }
    
    Slang::ComPtr<slang::IEntryPoint> entryPoint;
    {
        slangModule->findEntryPointByName(entry, entryPoint.writeRef());
        if (!entryPoint)
        {
            result->diagnostics.emplace_back("Failed to find entry point");
            return result;
        }
    }
    
    std::array<slang::IComponentType*, 2> componentTypes =
        {
        slangModule,
        entryPoint
    };
    
    Slang::ComPtr<slang::IComponentType> composedProgram;
    {
        Slang::ComPtr<slang::IBlob> diagnostics;
        auto operationResult = session->session->createCompositeComponentType(
            componentTypes.data(),
            componentTypes.size(),
            composedProgram.writeRef(),
            diagnostics.writeRef());
        
        if(diagnostics)
        {
            result->diagnostics.emplace_back(static_cast<const char *>(diagnostics->getBufferPointer()));
        }
        
        if(SLANG_FAILED(operationResult))
        {
            result->diagnostics.emplace_back("Failed to create composite component");
            return result;
        }
    }
    
    Slang::ComPtr<slang::IComponentType> linkedProgram;
    {
        Slang::ComPtr<slang::IBlob> diagnostics;
        auto operationResult = composedProgram->link(
            linkedProgram.writeRef(),
            diagnostics.writeRef());

        if(diagnostics)
        {
            result->diagnostics.emplace_back(static_cast<const char *>(diagnostics->getBufferPointer()));
        }
        
        if(SLANG_FAILED(operationResult))
        {
            result->diagnostics.emplace_back("Failed to link");
            return result;
        }
    }
    
    Slang::ComPtr<slang::IBlob> generatedCode;
    {
        Slang::ComPtr<slang::IBlob> diagnostics;
        auto operationResult = linkedProgram->getEntryPointCode(
            0, // entryPointIndex
            0, // targetIndex
            result->data.writeRef(),
            diagnostics.writeRef());

        //linkedProgram->getLayout()->toJson(generatedCode.writeRef());
        
        if(diagnostics)
        {
            result->diagnostics.emplace_back(static_cast<const char *>(diagnostics->getBufferPointer()));
        }

        if(SLANG_FAILED(operationResult))
        {
            result->diagnostics.emplace_back("Failed to generate code");
            return result;
        }
    }

    return result;
}

EXPORT_IMPL void slangResultDestroy(const Result* result)
{
    delete result;
}

EXPORT_IMPL int slangResultGetDiagnosticsCount(const Result* result)
{
    return static_cast<int>(result->diagnostics.size());
}

EXPORT_IMPL char* slangResultGetDiagnostic(const Result* result, int index)
{
    return const_cast<char*>(result->diagnostics[index].data());
}

EXPORT_IMPL void* slangResultGetPointer(const Result* result)
{
    if(result->data)
    {
        return const_cast<void*>(result->data->getBufferPointer());
    }
    
    return nullptr;
}

EXPORT_IMPL int slangResultGetSize(const Result* result)
{
    if(result->data)
    {
        return static_cast<int>(result->data->getBufferSize());
    }
    
    return 0;
}

