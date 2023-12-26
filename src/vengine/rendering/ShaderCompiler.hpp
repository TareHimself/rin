#pragma once
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"
#include "vengine/containers/Set.hpp"

#include <string>
#include <filesystem>
#include <glslang/Public/ShaderLang.h>
#include <vulkan/vulkan.hpp>

namespace vengine {
namespace rendering {
class Renderer;
}
}

namespace vengine {
namespace rendering {


class ShaderInc : public glslang::TShader::Includer {
  std::filesystem::path path;
  Set<IncludeResult *> results;
public:
  ShaderInc(const std::filesystem::path &inPath);


  IncludeResult *includeSystem(const char*filePath, const char *includerName, size_t inclusionDepth) override;
  IncludeResult *includeLocal(const char*filePath, const char *includerName, size_t inclusionDepth) override;

  void releaseInclude(IncludeResult *result) override;

  ~ShaderInc() override;
  
};


class ShaderCompiler : public Object<Renderer> {
public:
  
  static EShLanguage getLang(const std::filesystem::path &shaderPath);
  
  Array<unsigned int> compile(const std::filesystem::path &shaderPath) const;

  Array<unsigned int> compileAndSave(const std::filesystem::path &shaderPath) const;
  
  Array<unsigned int> loadShader(const std::filesystem::path &shaderPath) const;

  void init(Renderer *outer) override;

  void onCleanup() override;
};
}
}
