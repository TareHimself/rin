#pragma once
#include "aerox/EngineSubsystem.hpp"
#include "aerox/Object.hpp"
#include <vscript/vscript.hpp>
#include <aerox/fs.hpp>
#include "gen/scripting/ScriptSubsystem.gen.hpp"


namespace aerox::scripting {
class Script;
}


namespace aerox {
class Engine;
}


namespace aerox::scripting {

class ScriptSubsystem;
class ScriptLogger : public vs::frontend::DynamicObject {
  ScriptSubsystem * _subsystem = nullptr;
public:
  ScriptLogger(ScriptSubsystem * subsystem);
  void Init() override;

  static std::string ArgsToString(const std::shared_ptr<vs::frontend::FunctionScope>& fnScope);
  
  std::shared_ptr<vs::frontend::Object> PrintInfo(const std::shared_ptr<vs::frontend::FunctionScope>& fnScope);
  std::shared_ptr<vs::frontend::Object> PrintWarn(const std::shared_ptr<vs::frontend::FunctionScope>& fnScope);
  std::shared_ptr<vs::frontend::Object> PrintError(const std::shared_ptr<vs::frontend::FunctionScope>& fnScope);

  ScriptSubsystem * GetSubsystem() const;

  
};
META_TYPE()
class ScriptSubsystem : public EngineSubsystem {
  std::shared_ptr<vs::frontend::Program> _program;
public:

  META_BODY()
  
  void OnInit(Engine * outer) override;
  std::shared_ptr<Script> ScriptFromFile(const fs::path &path);
  std::weak_ptr<vs::frontend::Program> GetProgram() const;
  void OnDestroy() override;

  String GetName() const override;
};
}