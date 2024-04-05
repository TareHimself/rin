#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/drawing/DrawingSubsystem.hpp"

namespace aerox::drawing {

std::shared_ptr<meta::Metadata> DrawingSubsystem::Meta = [](){
return meta::TypeBuilder()
.Create<DrawingSubsystem>("DrawingSubsystem","",{});
}();


std::shared_ptr<meta::Metadata> DrawingSubsystem::GetMeta() const
{
return meta::find<DrawingSubsystem>();
}

}
