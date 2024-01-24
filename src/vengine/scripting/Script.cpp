#include "Script.hpp"

#include "ScriptManager.hpp"

namespace vengine::scripting {
void Script::Init(ScriptManager *outer, const std::filesystem::path &scriptPath,
    const String &moduleId) {
  _moduleId = moduleId;
  _scriptPath = scriptPath;
  Init(outer);
}

void Script::Init(ScriptManager *outer) {
  Object<ScriptManager>::Init(outer);
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
  
  ctx->Release();
}
}
