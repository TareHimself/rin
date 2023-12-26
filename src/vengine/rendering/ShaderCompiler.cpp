#include "ShaderCompiler.hpp"

#include "vengine/log.hpp"
#include "vengine/utils.hpp"
#include "vengine/io/io.hpp"

#include <glslang/MachineIndependent/localintermediate.h>
#include <glslang/Public/ResourceLimits.h>
#include <glslang/SPIRV/GlslangToSpv.h>
#include <vengine/rendering/Renderer.hpp>

namespace vengine {
namespace rendering {

ShaderInc::ShaderInc(const std::filesystem::path &inPath) {
  path = inPath;
}

glslang::TShader::Includer::IncludeResult * ShaderInc::includeSystem(const char*filePath, const char *includerName, size_t inclusionDepth) {
  const std::filesystem::path actualPath(filePath);
  const auto fileContent = new std::string(io::readFileAsString(actualPath));
  auto result = new IncludeResult(path.string(),fileContent->c_str(),fileContent->size(),fileContent);
  results.add(result);
  return result;
}

glslang::TShader::Includer::IncludeResult * ShaderInc::includeLocal(const char*filePath, const char *includerName, size_t inclusionDepth) {
  const auto actualPath = path / std::filesystem::path(filePath);
  const auto fileContent = new std::string(io::readFileAsString(actualPath));
  auto result = new IncludeResult(path.string(),fileContent->c_str(),fileContent->size(),fileContent);
  results.add(result);
  return result;
}

void ShaderInc::releaseInclude(IncludeResult *result) {
  if(result != NULL) {
    results.remove(result);
    delete static_cast<std::string *>(result->userData);
    delete result;
  }
}

ShaderInc::~ShaderInc() {

  for(const auto result : results) {
    delete static_cast<std::string *>(result->userData);
    delete result;
  }
  
  results.clear();
}


EShLanguage ShaderCompiler::getLang(const std::filesystem::path &shaderPath) {
  const auto ext = shaderPath.extension().string().substr(1);

  if (ext == "vert") {
    return EShLangVertex;
  }

  if (ext == "tesc") {
    return EShLangTessControl;
  }

  if (ext == "tese") {
    return EShLangTessEvaluation;
  }

  if (ext == "geom") {
    return EShLangGeometry;
  }
  if (ext == "frag") {
    return EShLangFragment;
  }
  if (ext == "comp") {
    return EShLangCompute;
  }

  if (ext == "rgen") {
    return EShLangRayGen;
  }
  if (ext == "rint") {
    return EShLangIntersect;
  }
  if (ext == "rahit") {
    return EShLangAnyHit;
  }
  if (ext == "rchit") {
    return EShLangClosestHit;
  }
  if (ext == "rmiss") {
    return EShLangMiss;
  }
  if (ext == "rcall") {
    return EShLangCallable;
  }
  if (ext == "mesh") {
    return EShLangMesh;
  }
  if (ext == "task") {
    return EShLangTask;
  }

  return EShLangVertex;
}

Array<unsigned int> ShaderCompiler::compile(const std::filesystem::path &shaderPath) const {
  if (!std::filesystem::exists(shaderPath)) {
    throw std::runtime_error(
        std::string("Shader file does not exist: ") + shaderPath.string());
  }

  log::shaders->info("Compiling Shader from file: " + shaderPath.string());
  
  const auto lang = getLang(shaderPath);
  
  const auto shader = new glslang::TShader(lang);

  const auto shaderFileContent = io::readFileAsString(shaderPath);

  shader->setEnvClient(glslang::EShClient::EShClientVulkan,glslang::EShTargetVulkan_1_3);
  shader->setEnvTarget(glslang::EshTargetSpv,glslang::EShTargetSpv_1_3);
  const char *sourcePtr = shaderFileContent.c_str();
  const char *const *sourcePtrArr = &(sourcePtr);
  const int sourcePtrSize = shaderFileContent.size();
  
  shader->setStringsWithLengths(sourcePtrArr,&sourcePtrSize,1);
  shader->setSourceEntryPoint("main");
  shader->setEntryPoint("main");

  shader->getIntermediate()->setSource(glslang::EShSourceGlsl);

  constexpr auto message = static_cast<EShMessages>(EShMessages::EShMsgVulkanRules | EShMessages::EShMsgSpvRules);

  ShaderInc includer(shaderPath);
  const auto resources =  GetResources();
  resources->maxDrawBuffers = true;
  resources->limits.nonInductiveForLoops = true;
  resources->limits.whileLoops = true;
  resources->limits.doWhileLoops = true;
  resources->limits.generalUniformIndexing = true;
  resources->limits.generalAttributeMatrixVectorIndexing = true;
  resources->limits.generalVaryingIndexing = true;
  resources->limits.generalSamplerIndexing = true;
  resources->limits.generalVariableIndexing = true;
  resources->limits.generalConstantMatrixVectorIndexing = true;
  
  if(!shader->parse(resources,450,ENoProfile,false,false,message,includer)) {
    throw std::runtime_error(std::string("Failed to parse shader at path: ") + shaderPath.string() + "\n" + shader->getInfoLog());
  }

  Array<unsigned int> spvResult;
  
  {
    glslang::TProgram program;
    program.addShader(shader);

    if(!program.link(message)) {
      throw std::runtime_error(std::string("Failed to parse shader at path: ") + shaderPath.string() + "\n" + program.getInfoLog());
    }

    glslang::SpvOptions options{};

    options.generateDebugInfo=false;
    options.stripDebugInfo=true;
    options.disableOptimizer=false;
    
    
    
    glslang::GlslangToSpv(*program.getIntermediate(lang),spvResult,&options);
  }

  delete shader;

  log::shaders->info("Compiled Shader from file: " + shaderPath.string());
  return spvResult;
}

Array<unsigned> ShaderCompiler::compileAndSave(
    const std::filesystem::path &shaderPath) const {
  const auto compiledPath = io::getCompiledShadersPath() / (shaderPath.filename().string() + ".spv");
  
  
  auto compiledData = compile(shaderPath);
    
  if(!std::filesystem::exists(compiledPath.parent_path())) {
    std::filesystem::create_directory(compiledPath.parent_path());
  }

  glslang::OutputSpvBin(compiledData,compiledPath.string().c_str());
  
  const auto sourceFile = io::readFileAsString(shaderPath);
  
  const auto sourceFileHash = utils::hash(sourceFile.data(),sourceFile.size() * sizeof(char));

  const auto compiledHashPath = io::getCompiledShadersPath() / (shaderPath.filename().string() + ".hash");
  
  io::writeStringToFile(compiledHashPath,sourceFileHash);
  
  return compiledData;
}

Array<unsigned int> ShaderCompiler::loadShader(const std::filesystem::path &shaderPath) const {
  log::shaders->info("Loading Shader " + shaderPath.string());
  const auto compiledPath = io::getCompiledShadersPath() / (shaderPath.filename().string() + ".spv");
  const auto compiledHashPath = io::getCompiledShadersPath() / (shaderPath.filename().string() + ".hash");
  
  if(std::filesystem::exists(compiledPath) && std::filesystem::exists(compiledHashPath)) {
    auto shader = io::readFile<unsigned int>(compiledPath);
    const auto oldShaderHash = io::readFileAsString(compiledHashPath);
    const auto sourceFile = io::readFileAsString(shaderPath);
    const auto newShaderHash = utils::hash(sourceFile.data(),sourceFile.size() * sizeof(char));
    
    if(oldShaderHash == newShaderHash) {
      log::shaders->info("Loaded shader from disk: " + shaderPath.string());
      return shader;
    } else {
      log::shaders->info("Detected change in source file: " + shaderPath.string());
      return compileAndSave(shaderPath);
    }
  }
  
  return compileAndSave(shaderPath);
}

void ShaderCompiler::init(Renderer *outer) {
  Object::init(outer);
  glslang::InitializeProcess();
}


void ShaderCompiler::onCleanup() {
  Object::onCleanup();
  glslang::FinalizeProcess();
}

}
}
