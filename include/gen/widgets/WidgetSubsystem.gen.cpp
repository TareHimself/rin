#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/widgets/WidgetSubsystem.hpp"

namespace aerox::widgets {

std::shared_ptr<meta::Metadata> WidgetSubsystem::Meta = [](){
return meta::TypeBuilder()
.Create<WidgetSubsystem>("WidgetSubsystem","",{});
}();


std::shared_ptr<meta::Metadata> WidgetSubsystem::GetMeta() const
{
return meta::find<WidgetSubsystem>();
}

}
