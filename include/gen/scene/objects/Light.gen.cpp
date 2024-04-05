#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/objects/Light.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> Light::Meta = [](){
return meta::TypeBuilder()
.Create<Light>("Light","",{});
}();


std::shared_ptr<meta::Metadata> Light::GetMeta() const
{
return meta::find<Light>();
}

}
