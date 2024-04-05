#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/assets/AssetSubsystem.hpp"

namespace aerox::assets {

std::shared_ptr<meta::Metadata> AssetSubsystem::Meta = [](){
return meta::TypeBuilder()
.Create<AssetSubsystem>("AssetSubsystem","",{});
}();


std::shared_ptr<meta::Metadata> AssetSubsystem::GetMeta() const
{
return meta::find<AssetSubsystem>();
}

}
