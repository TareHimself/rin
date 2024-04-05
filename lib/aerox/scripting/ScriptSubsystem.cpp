#include <aerox/scripting/ScriptSubsystem.hpp>
#define UUID_SYSTEM_GENERATOR
#include <aerox/scripting/Script.hpp>
#include "uuid.h"

namespace aerox::scripting {


ScriptLogger::ScriptLogger(ScriptSubsystem *subsystem)
  : DynamicObject({}) {
  _subsystem = subsystem;
}

void ScriptLogger::Init() {
  DynamicObject::Init();
  AddNativeMemberFunction(vs::frontend::ReservedDynamicFunctions::CALL, this,{}, &ScriptLogger::PrintInfo);
  AddNativeMemberFunction("info", this, {}, &ScriptLogger::PrintInfo);
  AddNativeMemberFunction("warn", this, {}, &ScriptLogger::PrintWarn);
  AddNativeMemberFunction("error", this, {}, &ScriptLogger::PrintError);
}

std::string ScriptLogger::ArgsToString(
    const std::shared_ptr<vs::frontend::FunctionScope> &fnScope) {
  std::string result;
  const auto args = fnScope->GetArgs();
  for (auto i = 0; i < args.size(); i++) {
    result += args[i]->ToString();
    if (i != args.size() - 1) {
      result += " ";
    }
  }
  return result;
}

std::shared_ptr<vs::frontend::Object> ScriptLogger::PrintInfo(
    const std::shared_ptr<vs::frontend::FunctionScope> &fnScope) {
  GetSubsystem()->GetLogger()->Info("{}", ArgsToString(fnScope));
  return vs::frontend::makeNull();
}

std::shared_ptr<vs::frontend::Object> ScriptLogger::PrintWarn(
    const std::shared_ptr<vs::frontend::FunctionScope> &fnScope) {
  GetSubsystem()->GetLogger()->Warn("{}", ArgsToString(fnScope));
  return vs::frontend::makeNull();
}

std::shared_ptr<vs::frontend::Object> ScriptLogger::PrintError(
    const std::shared_ptr<vs::frontend::FunctionScope> &fnScope) {
  GetSubsystem()->GetLogger()->Error("{}", ArgsToString(fnScope));
  return vs::frontend::makeNull();
}

ScriptSubsystem *ScriptLogger::GetSubsystem() const {
  return _subsystem;
}

void ScriptSubsystem::OnInit(Engine *outer) {
  EngineSubsystem::OnInit(outer);
  _program = vs::frontend::makeProgram();

  _program->Set("print", vs::frontend::makeObject<ScriptLogger>(this));

  AddCleanup([this] {
    _program.reset();
  });
}

std::shared_ptr<Script> ScriptSubsystem::ScriptFromFile(const fs::path &path) {
  const auto moduleId = to_string(uuids::uuid_system_generator{}());

  try {
    auto script = newObject<Script>(_program->ImportModule(path.string()));

    script->Init(this);

    return script;
  } catch (std::exception &e) {
    GetLogger()->Error("Failed to load script from file: {} \n{}",
                       path.string(), e.what());
  }

  return {};
}

std::weak_ptr<vs::frontend::Program> ScriptSubsystem::GetProgram() const {
  return _program;
}

void ScriptSubsystem::OnDestroy() {
  EngineSubsystem::OnDestroy();
}

String ScriptSubsystem::GetName() const {
  return "scripting";
}
}
