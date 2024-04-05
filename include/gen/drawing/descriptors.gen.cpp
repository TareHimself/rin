#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/drawing/descriptors.hpp"

namespace aerox::drawing {

std::shared_ptr<meta::Metadata> DescriptorSet::Meta = [](){
return meta::TypeBuilder()
.Create<DescriptorSet>("DescriptorSet","",{});
}();


std::shared_ptr<meta::Metadata> DescriptorSet::GetMeta() const
{
return meta::find<DescriptorSet>();
}

}
