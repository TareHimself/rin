#include "ScriptManager.hpp"
#define UUID_SYSTEM_GENERATOR
#include "Script.hpp"
#include "vengine/utils.hpp"
#include "uuid.h"
#include "scriptbuilder/scriptbuilder.h"
#include <scriptstdstring/scriptstdstring.h>

namespace vengine::scripting {
void ScriptManager::Init(Engine *outer)  {
  EngineSubsystem::Init(outer);
  _scriptEngine = asCreateScriptEngine();

  _scriptEngine->SetMessageCallback(asMETHOD(ScriptManager,MessageCallback),this,asCALL_THISCALL_ASGLOBAL);
  RegisterStdString(_scriptEngine);

  _scriptEngine->RegisterGlobalFunction("void print(const string &in)",asMETHOD(ScriptManager,DebugFromScript),asCALL_THISCALL_ASGLOBAL,this);
  
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

Script * ScriptManager::ScriptFromFile(const std::filesystem::path &path) {
  CScriptBuilder builder;
  const auto moduleId = to_string(uuids::uuid_system_generator{}());
  if(builder.StartNewModule(_scriptEngine,moduleId.c_str()) < 0) {
    GetLogger()->error("Failed to create script module, there is likely no more memory");
    return nullptr;
  }

  if(builder.AddSectionFromFile(path.string().c_str()) < 0) {
    GetLogger()->error("Failed to load script from file: {}",path.string());
    return nullptr;
  }

  if(builder.BuildModule() < 0) {
    GetLogger()->error("Errors were found while building script: {}",path.string());
    return nullptr;
  }

  const auto script = newObject<Script>();
  
  script->Init(this,path,moduleId);
  
  return script;
}

asIScriptEngine * ScriptManager::GetScriptEngine() const {
  return _scriptEngine;
}

void ScriptManager::HandleDestroy() {
  Object<Engine>::HandleDestroy();
  _scriptEngine->ShutDownAndRelease();
}

String ScriptManager::GetName() const {
  return "scripting";
}
}
