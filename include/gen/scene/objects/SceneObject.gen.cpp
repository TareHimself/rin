#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../../aerox/scene/objects/SceneObject.hpp"

namespace aerox::scene {

std::shared_ptr<meta::Metadata> SceneObject::Meta = [](){
return meta::TypeBuilder()
.Create<SceneObject>("SceneObject","Object",{"Super=Object"});
}();


std::shared_ptr<meta::Metadata> SceneObject::GetMeta() const
{
return meta::find<SceneObject>();
}

}
