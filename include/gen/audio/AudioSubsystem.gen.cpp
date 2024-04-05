#include <memory>
#include "aerox/meta/Metadata.hpp"
#include "aerox/meta/Macro.hpp"
#include "aerox/meta/Factory.hpp"
#include "../../aerox/audio/AudioSubsystem.hpp"

namespace aerox::audio {

std::shared_ptr<meta::Metadata> AudioSubsystem::Meta = [](){
return meta::TypeBuilder()
.Create<AudioSubsystem>("AudioSubsystem","",{});
}();


std::shared_ptr<meta::Metadata> AudioSubsystem::GetMeta() const
{
return meta::find<AudioSubsystem>();
}

}
