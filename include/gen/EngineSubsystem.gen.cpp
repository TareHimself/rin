#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../aerox/EngineSubsystem.hpp"

namespace aerox {

std::shared_ptr<meta::Metadata> EngineSubsystem::Meta = [](){
return meta::TypeBuilder()
.Create<EngineSubsystem>("EngineSubsystem","Object",{"Super=Object"});
}();


std::shared_ptr<meta::Metadata> EngineSubsystem::GetMeta() const
{
return meta::find<EngineSubsystem>();
}

}
