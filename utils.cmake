
function(Fetch REPOSITORY BRANCH DESTINATION)
  if(NOT EXISTS ${DESTINATION})
    execute_process(
      COMMAND git clone --depth 1 --branch ${BRANCH} ${REPOSITORY} ${DESTINATION}
    )
  endif()
endfunction()

function(GetBuildExt B_EXT)
  if("${CMAKE_BUILD_TYPE}" STREQUAL "Debug")
    set(${B_EXT} "_debug" PARENT_SCOPE)
  else()
    set(${B_EXT} "" PARENT_SCOPE)
  endif()
endfunction()

function(FetchAndBuild REPOSITORY BRANCH BUILD_DEST TEMP_DEST PRE_BUILD_FN BUILD_FN)
  if(NOT EXISTS ${BUILD_DEST})
    set(CLONED_DIR ${TEMP_DEST})
    
    if("${CMAKE_BUILD_TYPE}" STREQUAL "Debug")
      set(LOCAL_BUILD_TYPE "Debug")
    else()
      set(LOCAL_BUILD_TYPE "Release")
    endif()

    set(BUILD_DIR ${CLONED_DIR}/build/${LOCAL_BUILD_TYPE})

    Fetch(${REPOSITORY} ${BRANCH} ${CLONED_DIR})

    if(NOT "${PRE_BUILD_FN}" STREQUAL "")
      cmake_language(CALL ${PRE_BUILD_FN} ${CLONED_DIR})
    endif()

    if("${BUILD_FN}" STREQUAL "")
      execute_process(
        COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${LOCAL_BUILD_TYPE} -S ${CLONED_DIR} -B ${BUILD_DIR}
      )
    else()
      cmake_language(CALL ${BUILD_FN} ${LOCAL_BUILD_TYPE} ${CLONED_DIR} ${BUILD_DIR})
    endif()

    execute_process(
      COMMAND ${CMAKE_COMMAND} --build ${BUILD_DIR} --config ${LOCAL_BUILD_TYPE}
    )

    execute_process(
      COMMAND ${CMAKE_COMMAND} --install ${BUILD_DIR} --prefix ${BUILD_DEST} --config ${LOCAL_BUILD_TYPE}
    )
  endif()
endfunction()

# Fetches and builds a dependency
function(BuildThirdPartyDep FOLDER_NAME REPOSITORY VERSION RESULT PRE_BUILD_FN BUILD_ARGS)
  GetBuildExt(BUILD_EXT)
  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
  set(RESULT_DIR ${THIRD_PARTY_DIR}/${FOLDER_NAME}_${VERSION}${BUILD_EXT})
  set(CLONE_DIR ${CMAKE_CURRENT_BINARY_DIR}/${FOLDER_NAME})

  FetchAndBuild(${REPOSITORY} ${VERSION} ${RESULT_DIR} ${CLONE_DIR} "${PRE_BUILD_FN}" "${BUILD_ARGS}")
  set(${RESULT} ${RESULT_DIR} PARENT_SCOPE)
endfunction()

# VulkanMemoryAllocator
macro(GetImGui VERSION RESULT)
  set(OUT_DIR ${THIRD_PARTY_DIR}/imgui_${VERSION})

  if(NOT EXISTS ${OUT_DIR})
    Fetch(https://github.com/ocornut/imgui ${VERSION} ${OUT_DIR})

    file(REMOVE_RECURSE ${OUT_DIR}/examples)
    file(REMOVE_RECURSE ${OUT_DIR}/misc)
    file(COPY ${OUT_DIR}/backends/imgui_impl_sdl3.h DESTINATION ${OUT_DIR})
    file(COPY ${OUT_DIR}/backends/imgui_impl_sdl3.cpp DESTINATION ${OUT_DIR})
    file(COPY ${OUT_DIR}/backends/imgui_impl_vulkan.h DESTINATION ${OUT_DIR})
    file(COPY ${OUT_DIR}/backends/imgui_impl_vulkan.cpp DESTINATION ${OUT_DIR})
    file(REMOVE_RECURSE ${OUT_DIR}/backends)
  endif()
  set(${RESULT} ${OUT_DIR})
endmacro()

# SimdJson
macro(GetSimdJson VERSION RESULT)

  BuildThirdPartyDep(simdjson https://github.com/simdjson/simdjson ${VERSION} RESULT_DIR "" "")

  set(${RESULT} ${RESULT_DIR})

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/simdjson)
endmacro()

# FastGLTF
macro(GetFastGLTF VERSION RESULT)
  

  function(BuildFastGLTF B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DFASTGLTF_DOWNLOAD_SIMDJSON=OFF -DCMAKE_PREFIX_PATH=${CMAKE_PREFIX_PATH} -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(fastgltf https://github.com/spnda/fastgltf ${VERSION} RESULT_DIR "" "BuildFastGLTF")

  set(${RESULT} ${RESULT_DIR})

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/fastgltf)
endmacro()

# Miniz
macro(GetMiniz VERSION RESULT)

  BuildThirdPartyDep(miniz https://github.com/richgel999/miniz ${VERSION} RESULT_DIR "" "")

  set(${RESULT} ${RESULT_DIR})

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/miniz)
endmacro()

# OpenFBX
macro(GetTinyObjLoader VERSION RESULT)

  BuildThirdPartyDep(tinyol https://github.com/tinyobjloader/tinyobjloader ${VERSION} RESULT_DIR "" "")

  set(${RESULT} ${RESULT_DIR})

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/tinyobjloader/cmake)
endmacro()

# VulkanMemoryAllocator
macro(GetVulkanMemoryAllocator VERSION RESULT)

  BuildThirdPartyDep(vkma https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)
  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/VulkanMemoryAllocator)
  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/)

  set(${RESULT} ${RESULT_DIR})
endmacro()

# VulkanMemoryAllocatorHpp
macro(GetVulkanMemoryAllocatorHpp VERSION RESULT)

  function(UpdateVkmaSubModules CLONED_PATH)
    execute_process(
      COMMAND git submodule update --init --recursive
      WORKING_DIRECTORY ${CLONED_PATH}
    )
      
  endfunction()

  function(BuildVkmaHpp B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(vkmahpp https://github.com/YaaZ/VulkanMemoryAllocator-Hpp ${VERSION} RESULT_DIR "UpdateVkmaSubModules" "BuildVkmaHpp")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)
  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/VulkanMemoryAllocator-Hpp)
  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/)

  set(${RESULT} ${RESULT_DIR})
endmacro()

# macro(GetVulkanMemoryAllocator VERSION RESULT)

#   set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
#   set(RESULT_DIR ${THIRD_PARTY_DIR}/vkma_${VERSION})

#   Fetch(https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator ${VERSION} ${RESULT_DIR})

#   set(${RESULT} ${RESULT_DIR})
# endmacro()

# GLSL
macro(GetGLSL VERSION RESULT)
  function(UpdateGlslDeps CLONED_PATH)
    execute_process(
      COMMAND python update_glslang_sources.py
      WORKING_DIRECTORY ${CLONED_PATH}
    )
    
  endfunction()

  function(BuildGlsl B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DENABLE_GLSLANG_BINARIES=OFF -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(glslang https://github.com/KhronosGroup/glslang ${VERSION} RESULT_DIR "UpdateGlslDeps" "BuildGlsl")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)
  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/glslang)
  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/)
  set(${RESULT} ${RESULT_DIR})
endmacro()

# VkBootstrap
macro(GetVkBootstrap VERSION RESULT)

  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
  set(VK_BOOTSTRAP_DIR ${THIRD_PARTY_DIR}/vkb)

  Fetch(https://github.com/charles-lunarg/vk-bootstrap ${VERSION} ${VK_BOOTSTRAP_DIR})

  set(${RESULT} ${VK_BOOTSTRAP_DIR})
endmacro()

# XXHash
macro(GetXXHash VERSION RESULT)

  function(BuildXXHash B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DXXHASH_BUILD_XXHSUM=OFF -S ${B_SRC}/cmake_unofficial -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(xxhash https://github.com/Cyan4973/xxHash ${VERSION} RESULT_DIR "" "BuildXXHash")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)
  set(${RESULT} ${RESULT_DIR})
endmacro()

# FMT
macro(GetFmt VERSION RESULT)

  BuildThirdPartyDep(fmt https://github.com/fmtlib/fmt ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)
  set(${RESULT} ${RESULT_DIR})
endmacro()

# spdlog
macro(GetSpdLog VERSION RESULT)

  BuildThirdPartyDep(spdlog https://github.com/gabime/spdlog ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)
  set(${RESULT} ${RESULT_DIR})
endmacro()

# SDL
macro(GetSDL VERSION RESULT)

  BuildThirdPartyDep(sdl https://github.com/libsdl-org/SDL ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/cmake)
  set(${RESULT} ${RESULT_DIR})
endmacro()

# GLM
macro(GetGlm VERSION RESULT)

  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
  set(GLM_DIR ${THIRD_PARTY_DIR}/glm)

  if(NOT EXISTS ${GLM_DIR})
    set(DOWNLOADED_FILE ${CMAKE_CURRENT_BINARY_DIR}/glm.zip)

    file(DOWNLOAD https://github.com/g-truc/glm/releases/download/${VERSION}/glm-${VERSION}.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)

    file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${THIRD_PARTY_DIR})
  endif()

  list(APPEND CMAKE_PREFIX_PATH ${GLM_DIR}/cmake/glm)
  set(${RESULT} ${GLM_DIR})
endmacro()

# ReactPhysics3D
macro(GetReactPhys VERSION RESULT)

  BuildThirdPartyDep(rp3d https://github.com/DanielChappuis/reactphysics3d ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  set(${RESULT} ${RESULT_DIR})
endmacro()