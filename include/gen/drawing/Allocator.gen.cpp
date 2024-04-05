#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/drawing/Allocator.hpp"

namespace aerox::drawing {

std::shared_ptr<meta::Metadata> AllocatedBuffer::Meta = [](){
return meta::TypeBuilder()
.Create<AllocatedBuffer>("AllocatedBuffer","",{});
}();


std::shared_ptr<meta::Metadata> AllocatedBuffer::GetMeta() const
{
return meta::find<AllocatedBuffer>();
}

}
namespace aerox::drawing {

std::shared_ptr<meta::Metadata> AllocatedImage::Meta = [](){
return meta::TypeBuilder()
.Create<AllocatedImage>("AllocatedImage","",{});
}();


std::shared_ptr<meta::Metadata> AllocatedImage::GetMeta() const
{
return meta::find<AllocatedImage>();
}

}
namespace aerox::drawing {

std::shared_ptr<meta::Metadata> Allocator::Meta = [](){
return meta::TypeBuilder()
.Create<Allocator>("Allocator","",{});
}();


std::shared_ptr<meta::Metadata> Allocator::GetMeta() const
{
return meta::find<Allocator>();
}

}
