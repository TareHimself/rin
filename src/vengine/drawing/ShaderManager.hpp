#pragma once
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"
#include "vengine/containers/Set.hpp"
#include <filesystem>
#include <map>
#include <glslang/Public/ShaderLang.h>

namespace vengine {
namespace drawing {
class Shader;
}
}

namespace vengine {
namespace drawing {
class Drawer;
}
}

namespace vengine {
namespace drawing {


class GlslShaderIncluder : public glslang::TShader::Includer {
  std::filesystem::path sourceFilePath;
  Set<IncludeResult *> results;
  bool bDebug = false;
public:
  GlslShaderIncluder(const std::filesystem::path &inPath);


  IncludeResult *includeSystem(const char*filePath, const char *includerName, size_t inclusionDepth) override;
  IncludeResult *includeLocal(const char*filePath, const char *includerName, size_t inclusionDepth) override;

  void releaseInclude(IncludeResult *result) override;

  ~GlslShaderIncluder() override;
  
};


class ShaderManager : public Object<Drawer> {

  std::map<std::filesystem::path,Shader *> _shaders;
  
public:
  
  static EShLanguage getLang(const std::filesystem::path &shaderPath);

  bool hasLoadedShader(const std::filesystem::path &shaderPath) const;

  Shader * getLoadedShader(const std::filesystem::path &shaderPath) const;
  
  Array<unsigned int> compile(const std::filesystem::path &shaderPath) const;

  Array<unsigned int> compileAndSave(const std::filesystem::path &shaderPath) const;
  
  Array<unsigned int> loadOrCompileSpv(const std::filesystem::path &shaderPath) const;

  Shader * registerShader(Shader * shader);

  void init(Drawer *outer) override;

  void handleCleanup() override;
};
}
}
