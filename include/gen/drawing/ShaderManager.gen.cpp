#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/drawing/ShaderManager.hpp"

namespace aerox::drawing {

std::shared_ptr<meta::Metadata> ShaderManager::Meta = [](){
return meta::TypeBuilder()
.Create<ShaderManager>("ShaderManager","",{});
}();


std::shared_ptr<meta::Metadata> ShaderManager::GetMeta() const
{
return meta::find<ShaderManager>();
}

}
