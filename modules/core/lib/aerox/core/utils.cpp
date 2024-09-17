#include "aerox/core/utils.hpp"
#include <fstream>

#include "aerox/core/platform.hpp"
#if _WIN32 || WIN32
#include <windows.h>
#include <Shlwapi.h>
#include <io.h> 
#endif
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
        return platform::getExecutablePath().parent_path() / "resources";
    }
}
