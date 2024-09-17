#pragma once
#include <filesystem>
#include <string>
#include <vector>

namespace aerox::platform {
    #ifdef _WIN32
    #define AEROX_PLATFORM_WINDOWS
    #endif

    #ifdef __APPLE__
    #define AEROX_PLATFORM_APPLE
    #endif

    #ifdef __linux__
    #define AEROX_PLATFORM_LINUX
    #endif

    enum class Platform
    {
        Windows,
        Linux,
        Apple
    };

    void init();
    
    Platform get();
    
    std::vector<std::string> selectFile(const std::string& title,bool multiple, const std::string& filter);

    std::vector<std::string> selectPath(const std::string& title,bool multiple);

    std::filesystem::path getExecutablePath();
}

