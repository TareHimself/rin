#include "rin/core/utils.hpp"
#include <fstream>

#include "rin/core/platform.hpp"
#if _WIN32 || WIN32
#include <windows.h>
#include <Shlwapi.h>
#include <io.h> 
#endif
std::vector<unsigned char> readFile(const std::filesystem::path& path)
{
    std::ifstream fileStream(path, std::ios::binary);
    return {(std::istreambuf_iterator<char>(fileStream)), std::istreambuf_iterator<char>()};
}

std::string readFileAsString(const std::filesystem::path& path)
{
    std::ifstream fileStream(path, std::ios::binary);
    return {std::istreambuf_iterator<char>(fileStream),
                                  std::istreambuf_iterator<char>()};
}

std::filesystem::path getResourcesPath()
{
    return rin::platform::getExecutablePath().parent_path() / "resources";
}