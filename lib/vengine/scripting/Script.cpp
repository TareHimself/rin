#include<vengine/scripting/Script.hpp>
#include <vengine/scripting/ScriptSubsystem.hpp>
#include <vengine/utils.hpp>

namespace vengine::scripting {
void Script::Init(ScriptSubsystem * outer,
    const std::filesystem::path &scriptPath, const String &moduleId) {
  _moduleId = moduleId;
  _scriptPath = scriptPath;
  Init(outer);
}

void Script::Init( ScriptSubsystem * outer) {
  Object::Init(outer);
  _module = GetOuter()->GetScriptEngine()->GetModule(_moduleId.c_str());
}

asIScriptModule * Script::GetModule() {
  return _module;
}

void Script::InitScript() {
  
  const auto initFunc = _module->GetFunctionByDecl("void Init()");
  if(initFunc == nullptr) {
    GetOuter()->GetLogger()->error("Script missing init function: {}",_scriptPath.string());
    return;
  }

  asIScriptContext * ctx = GetOuter()->GetScriptEngine()->CreateContext();
  
  ctx->Prepare(initFunc);
  const auto result = ctx->Execute();
  if(result != asEXECUTION_FINISHED) {
    if(result == asEXECUTION_EXCEPTION) {
      GetOuter()->GetLogger()->error("Script : {}\nException: {}",_scriptPath.string(),ctx->GetExceptionString());
    }
  }
  
  utils::vassert(ctx->Release() == 0,"Failed to release script context");
}
}
