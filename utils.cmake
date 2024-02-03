
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

function(BuildOnly CLONED_DIR BUILD_DEST LOCAL_BUILD_TYPE BUILD_FN)
  set(BUILD_DIR ${CLONED_DIR}/build/${LOCAL_BUILD_TYPE})
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
endfunction()

function(FetchAndBuild REPOSITORY BRANCH BUILD_DEST TEMP_DEST PRE_BUILD_FN BUILD_FN)
  if(NOT EXISTS ${BUILD_DEST})
    set(CLONED_DIR ${TEMP_DEST})
    
    if("${CMAKE_BUILD_TYPE}" STREQUAL "Debug")
      set(LOCAL_BUILD_TYPE "Debug")
    else()
      set(LOCAL_BUILD_TYPE "Release")
    endif()

    Fetch(${REPOSITORY} ${BRANCH} ${CLONED_DIR})

    if(NOT "${PRE_BUILD_FN}" STREQUAL "")
      cmake_language(CALL ${PRE_BUILD_FN} ${CLONED_DIR})
    endif()

    BuildOnly(${CLONED_DIR} ${BUILD_DEST} "Release" "${BUILD_FN}")
    BuildOnly(${CLONED_DIR} ${BUILD_DEST} "Debug" "${BUILD_FN}")
  endif()
endfunction()

# Fetches and builds a dependency
function(BuildThirdPartyDep FOLDER_NAME REPOSITORY VERSION RESULT PRE_BUILD_FN BUILD_ARGS)
  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
  set(RESULT_DIR ${THIRD_PARTY_DIR}/${FOLDER_NAME}_${VERSION})
  set(CLONE_DIR ${CMAKE_CURRENT_BINARY_DIR}/${FOLDER_NAME})

  FetchAndBuild(${REPOSITORY} ${VERSION} ${RESULT_DIR} ${CLONE_DIR} "${PRE_BUILD_FN}" "${BUILD_ARGS}")
  set(${RESULT} ${RESULT_DIR} PARENT_SCOPE)
endfunction()

# SimdJson
macro(GetSimdJson VERSION)

  BuildThirdPartyDep(simdjson https://github.com/simdjson/simdjson ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/simdjson)

  find_package(simdjson CONFIG REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC simdjson::simdjson)
endmacro()

# FastGLTF
macro(GetFastGLTF VERSION)
  

  function(BuildFastGLTF B_TYPE B_SRC B_DEST)
    # execute_process(
    #   COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DFASTGLTF_DOWNLOAD_SIMDJSON=OFF -DCMAKE_PREFIX_PATH=${CMAKE_PREFIX_PATH} -S ${B_SRC} -B ${B_DEST}
    # )
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(fastgltf https://github.com/TareHimself/fastgltf ${VERSION} RESULT_DIR "" "BuildFastGLTF")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/fastgltf)
  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/fastgltf/simdjson)

  find_package(fastgltf_simdjson REQUIRED)
  find_package(fastgltf CONFIG REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC fastgltf::fastgltf)
endmacro()

# Miniz
macro(GetMiniz VERSION)

  BuildThirdPartyDep(miniz https://github.com/richgel999/miniz ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/miniz)

  find_package(miniz CONFIG REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC miniz::miniz)
endmacro()

# uuid
macro(GetStdUUID VERSION)

  BuildThirdPartyDep(stduuid https://github.com/mariusbancila/stduuid ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/stduuid)
  find_package(stduuid REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include) 
endmacro()

# uuid
macro(GetReflect VERSION)

  BuildThirdPartyDep(reflect https://github.com/TareHimself/reflect ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)
  find_package(reflect REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include)
  target_link_libraries(${PROJECT_NAME} PUBLIC reflect::reflect)
endmacro()


# VulkanMemoryAllocator
macro(GetVulkanMemoryAllocator VERSION)
  set(RESULT_DIR ${THIRD_PARTY_DIR}/vkma_${VERSION})
  set(FILE_RESULT ${RESULT_DIR}/vk_mem_alloc.h)

  #BuildThirdPartyDep(vkma https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator ${VERSION} RESULT_DIR "" "")

  if(NOT EXISTS ${RESULT_DIR})
    file(DOWNLOAD https://raw.githubusercontent.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator/${VERSION}/include/vk_mem_alloc.h ${FILE_RESULT}  SHOW_PROGRESS)
  endif()

  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR})
  target_sources(${PROJECT_NAME} PUBLIC ${FILE_RESULT})
endmacro()

# GLSL
macro(GetGLSL VERSION)
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

  find_package(SPIRV-Tools-opt REQUIRED)
  find_package(glslang CONFIG REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC glslang::glslang glslang::SPIRV glslang::glslang-default-resource-limits)
endmacro()

# SPIRV Reflect
macro(GetSpirvCross VERSION)

  function(BuildSpirvCross B_TYPE B_SRC B_DEST)
    execute_process(#-DSPIRV_REFLECT_EXECUTABLE=OFF
      COMMAND ${CMAKE_COMMAND} -DSPIRV_CROSS_ENABLE_TESTS=OFF -DSPIRV_CROSS_CLI=OFF -DSPIRV_CROSS_ENABLE_HLSL=OFF -DSPIRV_CROSS_ENABLE_MSL=OFF -DSPIRV_CROSS_ENABLE_C_API=OFF -DCMAKE_BUILD_TYPE=${B_TYPE}  -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(spirvcross https://github.com/KhronosGroup/SPIRV-Cross ${VERSION} RESULT_DIR "" "BuildSpirvCross")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/share)
  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/glslang)
  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/)

  find_package(spirv_cross_core REQUIRED)
  find_package(spirv_cross_glsl REQUIRED)
  find_package(spirv_cross_cpp REQUIRED)
  
  

  # find_package(SPIRV-Tools-opt REQUIRED)
  # find_package(glslang CONFIG REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include)
  file(GLOB SPIRV_DLLS ${RESULT_DIR}/lib/*d.lib)
  target_link_libraries(${PROJECT_NAME} PUBLIC ${SPIRV_DLLS})
endmacro()

# VkBootstrap
macro(GetVkBootstrap VERSION)

  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
  set(VK_BOOTSTRAP_DIR ${THIRD_PARTY_DIR}/vkb)

  Fetch(https://github.com/charles-lunarg/vk-bootstrap ${VERSION} ${VK_BOOTSTRAP_DIR})

  add_subdirectory(${VK_BOOTSTRAP_DIR} vk-bootstrap)
  target_include_directories(${PROJECT_NAME} PRIVATE ${RESULT_DIR}) 
  target_link_libraries(${PROJECT_NAME} PUBLIC PRIVATE vk-bootstrap::vk-bootstrap)
endmacro()


# XXHash
macro(GetXXHash VERSION)

  function(BuildXXHash B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DXXHASH_BUILD_XXHSUM=OFF -S ${B_SRC}/cmake_unofficial -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(xxhash https://github.com/Cyan4973/xxHash ${VERSION} RESULT_DIR "" "BuildXXHash")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(xxHash REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC xxHash::xxhash)
endmacro()

# FMT
macro(GetFmt VERSION)

  BuildThirdPartyDep(fmt https://github.com/fmtlib/fmt ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(fmt REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC fmt::fmt)
endmacro()

# spdlog
macro(GetSpdLog VERSION)

  BuildThirdPartyDep(spdlog https://github.com/gabime/spdlog ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(spdlog REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC spdlog::spdlog)
endmacro()

# SDL
macro(GetSDL VERSION)

  BuildThirdPartyDep(sdl https://github.com/libsdl-org/SDL ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/cmake)

  find_package(SDL3 REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC SDL3::SDL3)
endmacro()

# GLM
macro(GetGlm VERSION)

  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
  set(GLM_DIR ${THIRD_PARTY_DIR}/glm)

  if(NOT EXISTS ${GLM_DIR})
    set(DOWNLOADED_FILE ${CMAKE_CURRENT_BINARY_DIR}/glm.zip)

    file(DOWNLOAD https://github.com/g-truc/glm/releases/download/${VERSION}/glm-${VERSION}.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)

    file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${THIRD_PARTY_DIR})
  endif()

  list(APPEND CMAKE_PREFIX_PATH ${GLM_DIR}/cmake/glm)

  find_package(glm REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include)
  
endmacro()

# AngelScript
macro(GetAngelScript VERSION)

  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
  set(RESULT_DIR ${THIRD_PARTY_DIR}/angelscript_${VERSION})
  set(ADDONS_DIR ${RESULT_DIR}/addons/angelscript)

  if(NOT EXISTS ${RESULT_DIR})
    set(DOWNLOADED_FILE ${CMAKE_CURRENT_BINARY_DIR}/angelscript_sdk.zip)

    file(DOWNLOAD https://www.angelcode.com/angelscript/sdk/files/angelscript_${VERSION}.zip ${DOWNLOADED_FILE} SHOW_PROGRESS)

    set(EXTRACT_FILE ${CMAKE_CURRENT_BINARY_DIR}/angelscript_sdk)

    file(ARCHIVE_EXTRACT INPUT ${DOWNLOADED_FILE} DESTINATION ${EXTRACT_FILE})
    set(ANGELSCRIPT_CMAKE_DIR ${EXTRACT_FILE}/sdk/angelscript/projects/cmake)

    
    BuildOnly(${ANGELSCRIPT_CMAKE_DIR} ${RESULT_DIR} Debug "")
    BuildOnly(${ANGELSCRIPT_CMAKE_DIR} ${RESULT_DIR} Release "")

    

    set(ADDONS "scriptstdstring;scriptbuilder;debugger;scripthelper;serializer;autowrapper")
    # function(BuildOnly CLONED_DIR BUILD_DEST LOCAL_BUILD_TYPE BUILD_FN)

    if(ADDONS)
      file(MAKE_DIRECTORY ${ADDONS_DIR})

      foreach(ADDON ${ADDONS})
        message(STATUS "Copying AngelScript Addon: ${ADDON}")
        # file(ADDON_FILES /*)
        file(COPY ${EXTRACT_FILE}/sdk/add_on/${ADDON} DESTINATION ${ADDONS_DIR})
      endforeach(ADDON)
    endif()
  endif()

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  target_sources(${PROJECT_NAME} PUBLIC ${STB_FILES})
  find_package(Angelscript REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include)
  target_link_libraries(${PROJECT_NAME} PUBLIC Angelscript::angelscript)

  if(EXISTS ${ADDONS_DIR})
    file(GLOB ADDON_SOURCES ${ADDONS_DIR}/**/*.cpp)
    file(GLOB ADDON_INCLUDES ${ADDONS_DIR}/**/*.h)
    set(ADDONS_PROJECT_NAME angelscript_addons)
    add_library(${ADDONS_PROJECT_NAME} STATIC ${ADDON_SOURCES} ${ADDON_INCLUDES})
    target_include_directories(${ADDONS_PROJECT_NAME} PUBLIC ${ADDONS_DIR} ${RESULT_DIR}/include)
    target_link_libraries(${PROJECT_NAME} PRIVATE ${ADDONS_PROJECT_NAME})
  endif()
endmacro()

macro(GetStbFiles)
  set(THIRD_PARTY_DIR ${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty)
  set(STB_DIR ${THIRD_PARTY_DIR}/stb)
  
  if(NOT EXISTS ${STB_DIR})
    file(DOWNLOAD https://raw.githubusercontent.com/nothings/stb/master/stb_image.h ${STB_DIR}/stb_image.h SHOW_PROGRESS)
  endif()

  file(GLOB STB_FILES ${STB_DIR}/*.h)

  target_include_directories(${PROJECT_NAME} PUBLIC ${STB_DIR})
  target_sources(${PROJECT_NAME} PUBLIC ${STB_FILES})

endmacro()


# ReactPhysics3D
macro(GetReactPhys VERSION)

  BuildThirdPartyDep(rp3d https://github.com/DanielChappuis/reactphysics3d ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(ReactPhysics3D REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC ReactPhysics3D::ReactPhysics3D)
  
endmacro()

# Argparse
macro(GetArgparse VERSION)

  function(BuildArgparse B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DARGPARSE_BUILD_TESTS=OFF -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(argpaarse https://github.com/p-ranav/argparse ${VERSION} RESULT_DIR "" "BuildArgparse")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(argparse REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC argparse::argparse)
  
endmacro()

# Pugixml
macro(GetPugiXml VERSION)

  BuildThirdPartyDep(pugixml https://github.com/zeux/pugixml ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(pugixml REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC pugixml::pugixml)
  
endmacro()

# OpenCV
macro(GetOpenCV VERSION)

  function(BuildOpenCV B_TYPE B_SRC B_DEST)
    execute_process(#
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DBUILD_LIST=core,imgproc,imgcodecs -DBUILD_EXAMPLES=OFF -DBUILD_DOCS=OFF -DBUILD_PERF_TESTS=OFF -DBUILD_TESTS=OFF -DWITH_CSTRIPES=OFF -DWITH_OPENCL=OFF -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(opencv https://github.com/opencv/opencv ${VERSION} RESULT_DIR "" "BuildOpenCV")
  
  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR})

  find_package(OpenCV REQUIRED)
  target_include_directories(${PROJECT_NAME} PUBLIC ${OpenCV_INCLUDE_DIRS}) 
  target_link_libraries(${PROJECT_NAME} PUBLIC ${OpenCV_LIBS})
endmacro()

# Reflection
macro(AutoReflectTarget REFLECT_TARGET_NAME ${})
  set(ReflectionExec "ReflectHeadersExec")
  set(ReflectHeaders_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/reflection/main.cpp)
  add_executable(${ReflectionExec} ${ReflectHeaders_SOURCES})
  target_link_libraries(${ReflectionExec} PUBLIC reflect::reflect)
  target_link_libraries(${ReflectionExec} PUBLIC argparse::argparse)
  target_include_directories(${ReflectionExec}
      PRIVATE
      "$<TARGET_PROPERTY:${PROJECT_NAME},INTERFACE_INCLUDE_DIRECTORIES>"
  )

  add_custom_target(${REFLECT_TARGET_NAME}
      COMMAND ${ReflectionExec} -s ${CMAKE_CURRENT_SOURCE_DIR}/include -o ${CMAKE_CURRENT_SOURCE_DIR}/include/generated
      WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
      COMMENT "Reflect vengine headers"
      SOURCES ${ReflectHeaders_SOURCES}
      )

  add_dependencies(${PROJECT_NAME} ReflectHeaders)
endmacro()