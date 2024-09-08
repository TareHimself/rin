#include "aerox/core/utils.hpp"
#include <fstream>

namespace aerox
{
    std::string readFileAsString(const std::filesystem::path& path)
    {
        std::ifstream fileStream(path, std::ios::binary);
        return {std::istreambuf_iterator<char>(fileStream),
                                      std::istreambuf_iterator<char>()};
    }

    std::filesystem::path getResourcesPath()
    {
        return std::filesystem::current_path() / "resources";
    }
}
