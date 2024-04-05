#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/io/IoSubsystem.hpp"

namespace aerox::io {

std::shared_ptr<meta::Metadata> IoSubsystem::Meta = [](){
return meta::TypeBuilder()
.Create<IoSubsystem>("IoSubsystem","",{});
}();


std::shared_ptr<meta::Metadata> IoSubsystem::GetMeta() const
{
return meta::find<IoSubsystem>();
}

}
