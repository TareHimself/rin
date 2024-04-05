#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/input/InputSubsystem.hpp"

namespace aerox::input {

std::shared_ptr<meta::Metadata> InputSubsystem::Meta = [](){
return meta::TypeBuilder()
.Create<InputSubsystem>("InputSubsystem","",{});
}();


std::shared_ptr<meta::Metadata> InputSubsystem::GetMeta() const
{
return meta::find<InputSubsystem>();
}

}
