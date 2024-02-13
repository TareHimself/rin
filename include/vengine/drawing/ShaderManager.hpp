#pragma once
#include "vengine/Object.hpp"
#include "vengine/WithLogger.hpp"
#include "vengine/containers/Array.hpp"
#include "vengine/containers/Set.hpp"
#include "vengine/scene/Scene.hpp"
#include <filesystem>
#include <map>
#include <glslang/Public/ShaderLang.h>
#include "generated/drawing/ShaderManager.reflect.hpp"

namespace vengine {
namespace drawing {
class ShaderManager;
}
}

namespace vengine::drawing {
class Shader;
}

namespace vengine::drawing {
class DrawingSubsystem;
}

namespace vengine::drawing {


class GlslShaderIncluder : public glslang::TShader::Includer {
  std::filesystem::path sourceFilePath;
  Set<IncludeResult *> _results;
  ShaderManager * _manager = nullptr;
  bool _bDebug = false;
public:
  GlslShaderIncluder(ShaderManager * manager,const std::filesystem::path &inPath);


  IncludeResult *includeSystem(const char*filePath, const char *includerName, size_t inclusionDepth) override;
  IncludeResult *includeLocal(const char*filePath, const char *includerName, size_t inclusionDepth) override;

  void releaseInclude(IncludeResult *result) override;

  ~GlslShaderIncluder() override;
  
};

RCLASS()
class ShaderManager : public Object<DrawingSubsystem>, public WithLogger {

  std::map<std::filesystem::path,Managed<Shader>> _shaders;
  
public:
  
  static EShLanguage GetLang(const std::filesystem::path &shaderPath);

  bool HasLoadedShader(const std::filesystem::path &shaderPath) const;

  Managed<Shader> GetLoadedShader(
      const std::filesystem::path &shaderPath) const;
  
  Array<unsigned int> Compile(const std::filesystem::path &shaderPath);

  Array<unsigned int> CompileAndSave(const std::filesystem::path &shaderPath);
  
  Array<unsigned int> LoadOrCompileSpv(const std::filesystem::path &shaderPath);

  Managed<Shader> RegisterShader(Managed<Shader> shader);

  Managed<Shader> CreateShader(const std::filesystem::path &path);
  
  void UnRegisterShader(const Shader * shader);

  void Init(DrawingSubsystem * outer) override;

  void BeforeDestroy() override;
};

REFLECT_IMPLEMENT(ShaderManager)
}
