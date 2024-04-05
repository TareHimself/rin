#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/scripting/ScriptSubsystem.hpp"

namespace aerox::scripting {

std::shared_ptr<meta::Metadata> ScriptSubsystem::Meta = [](){
return meta::TypeBuilder()
.Create<ScriptSubsystem>("ScriptSubsystem","",{});
}();


std::shared_ptr<meta::Metadata> ScriptSubsystem::GetMeta() const
{
return meta::find<ScriptSubsystem>();
}

}
