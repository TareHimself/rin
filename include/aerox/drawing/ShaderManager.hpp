#pragma once
#include "aerox/WithLogger.hpp"
#include "aerox/containers/Array.hpp"
#include "aerox/containers/Set.hpp"
#include "aerox/scene/Scene.hpp"
#include <aerox/fs.hpp>
#include <map>
#include <glslang/Public/ShaderLang.h>
#include "gen/drawing/ShaderManager.gen.hpp"
#include "aerox/TObjectWithInit.hpp"
#include "aerox/TOwnedBy.hpp"

namespace aerox::drawing {
class ShaderManager;
class Shader;
class DrawingSubsystem;

class GlslShaderIncluder : public glslang::TShader::Includer {
  fs::path sourceFilePath;
  Set<IncludeResult *> _results;
  ShaderManager * _manager = nullptr;
  bool _bDebug = false;
public:
  GlslShaderIncluder(ShaderManager * manager,const fs::path &inPath);


  IncludeResult *includeSystem(const char*filePath, const char *includerName, size_t inclusionDepth) override;
  IncludeResult *includeLocal(const char*filePath, const char *includerName, size_t inclusionDepth) override;

  void releaseInclude(IncludeResult *result) override;

  ~GlslShaderIncluder() override;
  
};

META_TYPE()
class ShaderManager : public TOwnedBy<DrawingSubsystem>, public WithLogger {

  std::map<fs::path,std::shared_ptr<Shader>> _shaders;
  
public:
  META_BODY()
  
  static EShLanguage GetLang(const fs::path &shaderPath);

  bool HasLoadedShader(const fs::path &shaderPath) const;

  std::shared_ptr<Shader> GetLoadedShader(
      const fs::path &shaderPath) const;
  
  Array<unsigned int> Compile(const fs::path &shaderPath);

  Array<unsigned int> CompileAndSave(const fs::path &shaderPath);
  
  Array<unsigned int> LoadOrCompileSpv(const fs::path &shaderPath);

  std::shared_ptr<Shader> RegisterShader(std::shared_ptr<Shader> shader);

  std::shared_ptr<Shader> CreateShader(const fs::path &path);
  
  void UnRegisterShader(const Shader * shader);

  void OnInit(DrawingSubsystem * owner) override;

  void OnDestroy() override;
};
}
