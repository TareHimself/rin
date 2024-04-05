#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/components/LightComponent.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> LightComponent::Meta = [](){
return meta::TypeBuilder()
.Create<LightComponent>("LightComponent","",{});
}();


std::shared_ptr<meta::Metadata> LightComponent::GetMeta() const
{
return meta::find<LightComponent>();
}

}
