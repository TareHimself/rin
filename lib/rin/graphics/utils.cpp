#include "rin/core/utils.h"
#include "rin/graphics/utils.h"

namespace rin::graphics
{
    std::filesystem::path getBuiltInShadersPath()
    {
        return getResourcesPath() / "rin" / "shaders" / "rin";
    }
}
