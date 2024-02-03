#include <vengine/scripting/ScriptManager.hpp>
#define UUID_SYSTEM_GENERATOR
#include <vengine/scripting/Script.hpp>
#include "uuid.h"
#include "scriptbuilder/scriptbuilder.h"
#include <scriptstdstring/scriptstdstring.h>

namespace vengine::scripting {
void ScriptManager::Init(Engine * outer) {
  EngineSubsystem::Init(outer);
  _scriptEngine = asCreateScriptEngine();

  _scriptEngine->SetMessageCallback(asMETHOD(ScriptManager,MessageCallback),this,asCALL_THISCALL_ASGLOBAL);
  RegisterStdString(_scriptEngine);

  _scriptEngine->RegisterGlobalFunction("void print(const string &in)",asMETHOD(ScriptManager,DebugFromScript),asCALL_THISCALL_ASGLOBAL,this);

  AddCleanup([=] {
    _scriptEngine->ShutDownAndRelease();
  });
}

void ScriptManager::MessageCallback(const asSMessageInfo *msg, void *param) const {
  if(msg->type == asMSGTYPE_ERROR) {
    GetLogger()->error("{} ({},{}) : {}\n",msg->section,msg->row,msg->col,msg->message);
  } else if(msg->type == asMSGTYPE_WARNING) {
    GetLogger()->warn("{} ({},{}) : {}\n",msg->section,msg->row,msg->col,msg->message);
  }else if(msg->type == asMSGTYPE_INFORMATION) {
    GetLogger()->info("{} ({},{}) : {}\n",msg->section,msg->row,msg->col,msg->message);
  }
}

void ScriptManager::DebugFromScript(const std::string &in) const {
  GetLogger()->info("SCRIPT DEBUG: {}",in);
}

Ref<Script> ScriptManager::ScriptFromFile(const std::filesystem::path &path) {
  CScriptBuilder builder;
  const auto moduleId = to_string(uuids::uuid_system_generator{}());
  if(builder.StartNewModule(_scriptEngine,moduleId.c_str()) < 0) {
    GetLogger()->error("Failed to create script module, there is likely no more memory");
    return {};
  }

  if(builder.AddSectionFromFile(path.string().c_str()) < 0) {
    GetLogger()->error("Failed to load script from file: {}",path.string());
    return {};
  }

  if(builder.BuildModule() < 0) {
    GetLogger()->error("Errors were found while building script: {}",path.string());
    return {};
  }

  auto script = newSharedObject<Script>();
  
  script->Init(this,path,moduleId);
  
  return script;
}

asIScriptEngine *ScriptManager::GetScriptEngine() const {
  return _scriptEngine;
}

void ScriptManager::HandleDestroy() {
  EngineSubsystem::HandleDestroy();
}

String ScriptManager::GetName() const {
  return "scripting";
}
}
