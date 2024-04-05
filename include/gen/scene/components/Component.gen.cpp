#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/components/Component.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> Component::Meta = [](){
return meta::TypeBuilder()
.Create<Component>("Component","",{});
}();


std::shared_ptr<meta::Metadata> Component::GetMeta() const
{
return meta::find<Component>();
}

}
