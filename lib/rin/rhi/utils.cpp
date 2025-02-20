#include "rin/core/utils.h"
#include "rin/rhi/utils.h"

namespace rin::rhi
{
    std::filesystem::path getBuiltInShadersPath()
    {
        return getResourcesPath() / "rin" / "shaders" / "rin";
    }
}
