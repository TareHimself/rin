

#include <iostream>
#include <vector>

#include "graphics.hpp"
#include "platform.hpp"


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
int main() {
    platformInit();
    testFileSelect();
    platformShutdown();
}
