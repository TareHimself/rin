#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/drawing/MaterialInstance.hpp"

namespace aerox::drawing {

std::shared_ptr<meta::Metadata> MaterialInstance::Meta = [](){
return meta::TypeBuilder()
.Create<MaterialInstance>("MaterialInstance","",{});
}();


std::shared_ptr<meta::Metadata> MaterialInstance::GetMeta() const
{
return meta::find<MaterialInstance>();
}

}
