#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/assets/LiveAsset.hpp"

namespace aerox::assets {

std::shared_ptr<meta::Metadata> LiveAsset::Meta = [](){
return meta::TypeBuilder()
.Create<LiveAsset>("LiveAsset","",{});
}();


std::shared_ptr<meta::Metadata> LiveAsset::GetMeta() const
{
return meta::find<LiveAsset>();
}

}
