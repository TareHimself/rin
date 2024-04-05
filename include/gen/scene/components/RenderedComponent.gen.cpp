#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/components/RenderedComponent.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> RenderedComponent::Meta = [](){
return meta::TypeBuilder()
.Create<RenderedComponent>("RenderedComponent","",{});
}();


std::shared_ptr<meta::Metadata> RenderedComponent::GetMeta() const
{
return meta::find<RenderedComponent>();
}

}
