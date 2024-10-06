#pragma once
#include <filesystem>
#include <string>
#include <vector>

namespace rin::platform {
    #ifdef _WIN32
    #define RIN_PLATFORM_WINDOWS
    #endif

    #ifdef __APPLE__
    #define RIN_PLATFORM_APPLE
    #endif

    #ifdef __linux__
    #define RIN_PLATFORM_LINUX
    #endif

    enum class Platform
    {
        Windows,
        Linux,
        Apple
    };

    void init();
    
    Platform get();
    
    std::vector<std::string> selectFile(const std::string& title,bool multiple = false, const std::string& filter = {});

    std::vector<std::string> selectPath(const std::string& title,bool multiple = false);

    std::filesystem::path getExecutablePath();
}

