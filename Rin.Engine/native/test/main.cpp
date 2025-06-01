

#include <iostream>
#include <vector>

#include "graphics.hpp"
#include "platform.hpp"

int main() {
    platformInit();

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

    platformShutdown();
}
