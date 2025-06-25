
#define STB_IMAGE_WRITE_IMPLEMENTATION
#include <stb/stb_image_write.h>
#include <iostream>
#include <vector>
#include "graphics.hpp"
#include "platform.hpp"
#include "video.hpp"


void testWindow() {
    VkInstance instance;
    VkDevice device;
    VkPhysicalDevice physicalDevice;
    VkQueue graphicsQueue;
    uint32_t graphicsQueueFamily;
    VkQueue transferQueue;
    uint32_t transferQueueFamily;
    VkSurfaceKHR vkSurface;
    VkDebugUtilsMessengerEXT debugMessenger;



    auto window = platformWindowCreate("Test Window",500,500,{});

    std::vector<WindowEvent> events{};
    events.resize(10);
    bool quit = false;
    while (!quit) {
        platformWindowPump();
        const auto count = platformWindowGetEvents(events.data(),events.size());
        for (auto i = 0; i < count; i++) {
            if (quit) {
                break;
            }
            switch (const auto event = events[i]; event.type) {
                case WindowEventType::Close:
                    quit = true;
                    break;
                case WindowEventType::Key:
                    std::cout << "Something happened to a key" << std::endl;
                    break;
                default:
                    break;
            }
        }
    }
    platformWindowDestroy(window);
}

void testFileSelect() {
    platformSelectFile("Select a file",false,"*.wav;*.ogg;*.flac;*.mp3",[](const char * path) {
        std::cout << "Got Path " << path << std::endl;
    });
}

void testVideo() {
    const char * data = R"(C:\Users\Taree\Downloads\videoplayback.webm)";
    auto ctx = videoContextFromFile(data);
    auto extent = videoContextGetVideoExtent(ctx);
    while (!videoContextEnded(ctx)) {
        videoContextDecode(ctx,3);

        auto data = static_cast<std::uint8_t*>(videoContextCopyRecentFrame(ctx,videoContextGetPosition(ctx) - 1.0f));
        stbi_write_png("./latest.png",extent.width,extent.height,4,data,extent.width * 4);
        delete[] data;
    }
    videoContextFree(ctx);
}
int main() {
    platformInit();
    //testFileSelect();
    testVideo();
    platformShutdown();
}
