include(${CMAKE_CURRENT_LIST_DIR}/../../build.cmake)

# glfw
macro(GetGlfw SPECIFIC_PROJECT VERSION)

  function(BuildGlfw B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DBUILD_SHARED_LIBS=ON -DGLFW_BUILD_EXAMPLES=OFF -DGLFW_BUILD_TESTS=OFF -DGLFW_BUILD_DOCS=OFF -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(glfw ${CMAKE_CURRENT_LIST_DIR}/../../ext https://github.com/glfw/glfw ${VERSION} RESULT_DIR "" "BuildGlfw")
  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)
  message(STATUS "PATHS ${CMAKE_PREFIX_PATH}")
  find_package(glfw3 REQUIRED PATHS ${RESULT_DIR}/lib/cmake)
  target_include_directories(${SPECIFIC_PROJECT} PUBLIC  
  $<BUILD_INTERFACE:${RESULT_DIR}/include>
  $<INSTALL_INTERFACE:include> )
  
  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC glfw)
endmacro()

# VulkanMemoryAllocator
macro(GetVulkanMemoryAllocator SPECIFIC_TARGET VERSION)
  BuildThirdPartyDep(vkma ${CMAKE_CURRENT_LIST_DIR}/../../ext https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator ${VERSION} RESULT_DIR "" "")

  find_package(VulkanMemoryAllocator CONFIG REQUIRED PATHS ${RESULT_DIR}/share/cmake/VulkanMemoryAllocator)

  target_include_directories(
          ${SPECIFIC_TARGET}
          PUBLIC
          $<BUILD_INTERFACE:${RESULT_DIR}/include>
          $<INSTALL_INTERFACE:include>
  )

  target_link_libraries(
          ${SPECIFIC_TARGET}
          PUBLIC
          GPUOpen::VulkanMemoryAllocator
  )
endmacro()

# VkBootstrap
macro(GetVkBootstrap SPECIFIC_PROJECT VERSION)

  function(BuildVkb B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DVK_BOOTSTRAP_TEST=OFF -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(vkb ${CMAKE_CURRENT_LIST_DIR}/../../ext https://github.com/charles-lunarg/vk-bootstrap ${VERSION} RESULT_DIR "" "BuildVkb")

  find_package(vk-bootstrap REQUIRED PATHS ${RESULT_DIR}/lib/cmake)
  target_include_directories(${SPECIFIC_PROJECT} PRIVATE ${RESULT_DIR}/include) 
  target_link_libraries(${SPECIFIC_PROJECT} PUBLIC PRIVATE vk-bootstrap::vk-bootstrap)
endmacro()


macro(SetDynamicLibraryDir SPECIFIC_TARGET)
  set(TARGET_DIR ${CMAKE_CURRENT_LIST_DIR}/bin/${CMAKE_BUILD_TYPE})
  set_target_properties(${SPECIFIC_TARGET}
      PROPERTIES
      LIBRARY_OUTPUT_DIRECTORY  ${TARGET_DIR}
      RUNTIME_OUTPUT_DIRECTORY ${TARGET_DIR}
      LIBRARY_OUTPUT_DIRECTORY_DEBUG  ${TARGET_DIR}
      RUNTIME_OUTPUT_DIRECTORY_DEBUG ${TARGET_DIR}
      LIBRARY_OUTPUT_DIRECTORY_RELEASE  ${TARGET_DIR}
      RUNTIME_OUTPUT_DIRECTORY_RELEASE ${TARGET_DIR}
  )
  message(STATUS ${TARGET_DIR})
endmacro()


