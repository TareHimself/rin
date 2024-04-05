#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/async/AsyncSubsystem.hpp"

namespace aerox::async {

std::shared_ptr<meta::Metadata> AsyncSubsystem::Meta = [](){
return meta::TypeBuilder()
.Create<AsyncSubsystem>("AsyncSubsystem","",{});
}();


std::shared_ptr<meta::Metadata> AsyncSubsystem::GetMeta() const
{
return meta::find<AsyncSubsystem>();
}

}
