
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

function(FetchAndBuild REPOSITORY BRANCH TARGET_NAME BUILD_DEST TEMP_DEST PRE_BUILD_FN BUILD_FN)
  set(TARGET_NAME_DEST ${BUILD_DEST}/${TARGET_NAME}_check)
  if(NOT EXISTS ${BUILD_DEST})
    set(CLONED_DIR ${TEMP_DEST})

    Fetch(${REPOSITORY} ${BRANCH} ${CLONED_DIR})

    if(NOT "${PRE_BUILD_FN}" STREQUAL "")
      cmake_language(CALL ${PRE_BUILD_FN} ${CLONED_DIR})
    endif()

    if(CMAKE_BUILD_TYPE)
      if("${CMAKE_BUILD_TYPE}" STREQUAL "Debug")
        BuildOnly(${CLONED_DIR} ${BUILD_DEST} "Debug" "${BUILD_FN}")
      else()
        BuildOnly(${CLONED_DIR} ${BUILD_DEST} "Release" "${BUILD_FN}")
      endif()
    else()
      BuildOnly(${CLONED_DIR} ${BUILD_DEST} "Release" "${BUILD_FN}")
    endif()

    #file(GENERATE OUTPUT ${TARGET_NAME_DEST} CONTENT "")

    file(REMOVE_RECURSE ${CLONED_DIR})
  endif()
endfunction()

# Fetches and builds a dependency
function(BuildThirdPartyDep FOLDER_NAME REPOSITORY VERSION RESULT PRE_BUILD_FN BUILD_ARGS)
  get_property(RIN_THIRD_PARTY_DIR GLOBAL PROPERTY RIN_THIRD_PARTY_DIR_PROPERTY)
  #set(RESULT_DIR ${THIRD_PARTY_DIR})
  set(RESULT_DIR ${RIN_THIRD_PARTY_DIR}/${FOLDER_NAME}_${VERSION})
  set(CLONE_DIR ${CMAKE_CURRENT_BINARY_DIR}/${FOLDER_NAME})

  FetchAndBuild(${REPOSITORY} ${VERSION} ${FOLDER_NAME} ${RESULT_DIR} ${CLONE_DIR} "${PRE_BUILD_FN}" "${BUILD_ARGS}")
  set(${RESULT} ${RESULT_DIR} PARENT_SCOPE)
endfunction()

macro(AddTargetLibs EXTERNAL_TARGET)
  get_target_property(EXTERNAL_TARGET_LIBRARY_PATH ${EXTERNAL_TARGET} LOCATION)
  list(APPEND VENGINE_LIBS ${EXTERNAL_TARGET_LIBRARY_PATH})
  set(EXTERNAL_TARGET_LIBRARY_PATH)
endmacro()
# SimdJson
macro(GetSimdJson VERSION)

  BuildThirdPartyDep(simdjson https://github.com/simdjson/simdjson ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/simdjson)

  find_package(simdjson CONFIG REQUIRED)
  target_include_directories(${PROJECT_NAME} PRIVATE ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC simdjson::simdjson)
  AddTargetLibs("simdjson::simdjson")
  unset(RESULT_DIR)
endmacro()

# Json
macro(GetJson VERSION)

  function(BuildJson B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DJSON_BuildTests=OFF -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(nlohmann_json https://github.com/nlohmann/json ${VERSION} RESULT_DIR "" "BuildJson")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/nlohmann_json)

  find_package(nlohmann_json REQUIRED)
  target_include_directories(${PROJECT_NAME} PRIVATE ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC nlohmann_json::nlohmann_json)

  unset(RESULT_DIR)
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
  target_include_directories(${PROJECT_NAME} PRIVATE ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC fastgltf::fastgltf)
  AddTargetLibs("fastgltf::fastgltf")

  unset(RESULT_DIR)
endmacro()

# Miniz
macro(GetMiniz VERSION)

  BuildThirdPartyDep(miniz https://github.com/richgel999/miniz ${VERSION} MINIZ_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${MINIZ_DIR}/lib/cmake/miniz)

  find_package(miniz CONFIG REQUIRED)
  target_include_directories(${PROJECT_NAME} PRIVATE ${MINIZ_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC miniz::miniz)
  AddTargetLibs("miniz::miniz")
endmacro()

# uuid
macro(GetStdUUID VERSION)

  BuildThirdPartyDep(stduuid https://github.com/mariusbancila/stduuid ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/stduuid)
  find_package(stduuid REQUIRED)
  target_include_directories(${PROJECT_NAME} PRIVATE ${RESULT_DIR}/include)
endmacro()

# sdl
macro(GetSDL VERSION)

  function(BuildSDL B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DSDL_SHARED=ON -DSDL_TEST_LIBRARY=OFF -DSDL_TESTS=OFF -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(sdl https://github.com/libsdl-org/SDL "${VERSION}" RESULT_DIR "" "BuildSDL")

  find_package(SDL3 REQUIRED CONFIG REQUIRED COMPONENTS SDL3-shared PATHS ${RESULT_DIR}/cmake)
  target_include_directories(${PROJECT_NAME} PUBLIC
  $<BUILD_INTERFACE:${RESULT_DIR}/include>
  $<INSTALL_INTERFACE:include> )
  
  target_link_libraries(${PROJECT_NAME} PUBLIC SDL3::SDL3)

  unset(RESULT_DIR)
endmacro()

# VulkanMemoryAllocator
macro(GetVulkanMemoryAllocator VERSION)
#  set(RESULT_DIR ${THIRD_PARTY_DIR}/vkma)
#  set(FILE_RESULT ${RESULT_DIR}/vk_mem_alloc.h)

  BuildThirdPartyDep(vkma https://github.com/GPUOpen-LibrariesAndSDKs/VulkanMemoryAllocator ${VERSION} RESULT_DIR "" "")

  find_package(VulkanMemoryAllocator CONFIG REQUIRED PATHS ${RESULT_DIR}/share/cmake/VulkanMemoryAllocator)

  target_include_directories(
          ${PROJECT_NAME}
          PUBLIC
          $<BUILD_INTERFACE:${RESULT_DIR}/include>
          $<INSTALL_INTERFACE:include>
  )

  target_link_libraries(
          ${PROJECT_NAME}
          PUBLIC
          GPUOpen::VulkanMemoryAllocator
  )

  unset(RESULT_DIR)
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
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DGLSLANG_TESTS=OFF -DENABLE_GLSLANG_BINARIES=OFF -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(glslang https://github.com/KhronosGroup/glslang ${VERSION} RESULT_DIR "UpdateGlslDeps" "BuildGlsl")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)
  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake/glslang)
  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/)

  find_package(SPIRV-Tools-opt REQUIRED)
  find_package(glslang CONFIG REQUIRED)
  target_link_libraries(${PROJECT_NAME} PUBLIC glslang::glslang glslang::SPIRV glslang::glslang-default-resource-limits)

  target_include_directories(
    ${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include>
    $<INSTALL_INTERFACE:include> 
  )

  install(
    DIRECTORY ${RESULT_DIR}/include/
    DESTINATION include
  )

  AddTargetLibs("glslang::glslang")
  AddTargetLibs("glslang::SPIRV")
  AddTargetLibs("glslang::glslang-default-resource-limits")

  unset(RESULT_DIR)
endmacro()

# VkBootstrap
macro(GetVkBootstrap VERSION)

  function(BuildVkb B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DVK_BOOTSTRAP_TEST=OFF -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(vkb https://github.com/charles-lunarg/vk-bootstrap ${VERSION} RESULT_DIR "" "BuildVkb")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(vk-bootstrap REQUIRED PATHS ${RESULT_DIR}/lib/cmake)
  target_include_directories(${PROJECT_NAME} PRIVATE ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC PRIVATE vk-bootstrap::vk-bootstrap)
  unset(RESULT_DIR)
endmacro()


# XXHash
macro(GetXXHash VERSION)

  function(BuildXXHash B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DXXHASH_BUILD_XXHSUM=OFF -S ${B_SRC}/cmake_unofficial -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(xxhash https://github.com/Cyan4973/xxHash ${VERSION} RESULT_DIR "" "BuildXXHash")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(xxHash REQUIRED)
  target_include_directories(${PROJECT_NAME} PRIVATE ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC xxHash::xxhash)

  unset(RESULT_DIR)
endmacro()

# FMT
macro(GetFmt VERSION)

  function(BuildFmt B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DFMT_DOC=OFF -DFMT_TEST=OFF -DCMAKE_BUILD_TYPE=${B_TYPE} -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(fmt https://github.com/fmtlib/fmt ${VERSION} RESULT_DIR "" "BuildFmt")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(fmt REQUIRED)
  target_link_libraries(${PROJECT_NAME} PUBLIC fmt::fmt)

  target_include_directories(
    ${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include>
    $<INSTALL_INTERFACE:include> 
  )

  install(
    DIRECTORY ${RESULT_DIR}/include/
    DESTINATION include
  )

  unset(RESULT_DIR)
endmacro()

# spdlog
macro(GetSpdLog VERSION)

  BuildThirdPartyDep(spdlog https://github.com/gabime/spdlog ${VERSION} RESULT_DIR "" "")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(spdlog REQUIRED)
  target_link_libraries(${PROJECT_NAME} PUBLIC spdlog::spdlog)

  target_include_directories(
    ${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include>
    $<INSTALL_INTERFACE:include> 
  )

  install(
    DIRECTORY ${RESULT_DIR}/include/
    DESTINATION include
  )

  unset(RESULT_DIR)
endmacro()


# Bass
macro(GetBass VERSION)

  BuildThirdPartyDep(bass https://github.com/TareHimself/bass-cpp ${VERSION} RESULT_DIR "" "")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(basscpp REQUIRED PATHS ${RESULT_DIR}/lib/cmake)
  target_link_libraries(${PROJECT_NAME} PUBLIC basscpp::basscpp)

  target_include_directories(
    ${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include>
    $<INSTALL_INTERFACE:include> 
  )

  install(
    DIRECTORY ${RESULT_DIR}/include/
    DESTINATION include
  )

  unset(RESULT_DIR)
endmacro()

# GLM
macro(GetGlm VERSION)

  function(BuildGlm B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DGLM_BUILD_TESTS=OFF -DCMAKE_BUILD_TYPE=${B_TYPE} -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()
  BuildThirdPartyDep(glm https://github.com/g-truc/glm ${VERSION} RESULT_DIR "" "BuildGlm")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/share)

  find_package(glm REQUIRED)

  target_include_directories(
    ${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include>
    $<INSTALL_INTERFACE:include> 
  )

  install(
    DIRECTORY ${RESULT_DIR}/include/
    DESTINATION include
  )

  unset(RESULT_DIR)
endmacro()

# shaderc
macro(GetShaderc VERSION)

  function(BuildShaderc B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DSHADERC_SKIP_COPYRIGHT_CHECK=ON -DSHADERC_SKIP_TESTS=ON -DSHADERC_SKIP_EXAMPLES=ON -DCMAKE_BUILD_TYPE=${B_TYPE} -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(shaderc https://github.com/google/shaderc ${VERSION} RESULT_DIR "" "BuildShaderc")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(shaderc REQUIRED)

  target_include_directories(
    ${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include>
    $<INSTALL_INTERFACE:include> 
  )

  install(
    DIRECTORY ${RESULT_DIR}/include/
    DESTINATION include
  )

  unset(RESULT_DIR)
endmacro()

# rsl
macro(GetRsl VERSION)

  function(BuildRsl B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -S ${B_SRC}/cpp -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(rsl https://github.com/TareHimself/rsl ${VERSION} RESULT_DIR "" "BuildRsl")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(rsl REQUIRED)

  target_link_libraries(${PROJECT_NAME} PUBLIC rsl::rsl)

  target_include_directories(
    ${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include>
    $<INSTALL_INTERFACE:include> 
  )

  install(
    DIRECTORY ${RESULT_DIR}/include/
    DESTINATION include
  )

  unset(RESULT_DIR)
endmacro()

# vscript
macro(GetVScript VERSION)



  BuildThirdPartyDep(vscript https://github.com/TareHimself/vscript ${VERSION} RESULT_DIR "" "")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/share)

  find_package(vscript REQUIRED)

  target_link_libraries(${PROJECT_NAME} PUBLIC vscript::vscript)

  target_include_directories(
    ${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include>
    $<INSTALL_INTERFACE:include> 
  )

  install(
    DIRECTORY ${RESULT_DIR}/include/
    DESTINATION include
  )

  unset(RESULT_DIR)
endmacro()

# ReactPhysics3D
macro(GetReactPhys VERSION)

  BuildThirdPartyDep(rp3d https://github.com/DanielChappuis/reactphysics3d ${VERSION} RESULT_DIR "" "")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(ReactPhysics3D REQUIRED PATHS ${RESULT_DIR}/lib/cmake)

  target_link_libraries(${PROJECT_NAME} PUBLIC ReactPhysics3D::ReactPhysics3D)
  
  target_include_directories(
    ${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include>
    $<INSTALL_INTERFACE:include> 
  )

  install(
    DIRECTORY ${RESULT_DIR}/include/
    DESTINATION include
  )
endmacro()


# Argparse
macro(GetArgparse VERSION)

  function(BuildArgparse B_TYPE B_SRC B_DEST)
    execute_process(
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DARGPARSE_BUILD_TESTS=OFF -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(argparse https://github.com/p-ranav/argparse ${VERSION} RESULT_DIR "" "BuildArgparse")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(argparse REQUIRED)
  target_include_directories(${PROJECT_NAME} PRIVATE ${RESULT_DIR}/include) 
  target_link_libraries(${PROJECT_NAME} PUBLIC argparse::argparse)
  
endmacro()

# STB
macro(GetStb VERSION)
  set(RESULT_DIR ${RIN_THIRD_PARTY_DIR}/stb_${VERSION})
  set(OUTPUT_FILES "")
  if(NOT EXISTS ${RESULT_DIR})
    set(REPO_DIR ${CMAKE_CURRENT_BINARY_DIR}/stb)
    Fetch(https://github.com/nothings/stb ${VERSION} ${REPO_DIR})

    file(MAKE_DIRECTORY ${RESULT_DIR})

    set(FILES_TO_COPY "stb_image.h" "stb_image_write.h")

    foreach(TO_COPY ${FILES_TO_COPY})
      file(COPY ${REPO_DIR}/${TO_COPY} DESTINATION ${RESULT_DIR}/include/stb)
      list(APPEND OUTPUT_FILES ${RESULT_DIR}/include/stb/${TO_COPY})
    endforeach()

    file(REMOVE_RECURSE ${REPO_DIR})
    unset(FILES_TO_COPY)
    unset(REPO_DIR)
  endif()

  target_include_directories(
    ${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include>
    $<INSTALL_INTERFACE:include> 
  )

  target_sources(${PROJECT_NAME} PUBLIC ${OUTPUT_FILES})
  unset(OUTPUT_FILES)
  unset(RESULT_DIR)
endmacro()

# FreeType
macro(GetFreeType VERSION)

  BuildThirdPartyDep(freetype https://gitlab.freedesktop.org/freetype/freetype ${VERSION} RESULT_DIR "" "")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(freetype REQUIRED PATHS ${RESULT_DIR}/lib/cmake)

  target_include_directories(
    ${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include/freetype2>
    $<INSTALL_INTERFACE:include>
  )

  target_link_libraries(${PROJECT_NAME} PUBLIC freetype)

  unset(RESULT_DIR)
endmacro()

# Tinyxml2
macro(GetTinyxml2 VERSION)
  BuildThirdPartyDep(tinyxml2 https://github.com/leethomason/tinyxml2 ${VERSION} RESULT_DIR "" "")

  # list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(tinyxml2 REQUIRED)

  target_include_directories(
          ${PROJECT_NAME}
          PUBLIC
          $<BUILD_INTERFACE:${RESULT_DIR}/include>
          $<INSTALL_INTERFACE:include>
  )

  target_link_libraries(${PROJECT_NAME} PUBLIC tinyxml2::tinyxml2)

  unset(RESULT_DIR)
endmacro()

# MsdfGen
macro(GetMsdfGen VERSION)


  function(BuildMsdfGen B_TYPE B_SRC B_DEST)
    execute_process(#
      COMMAND ${CMAKE_COMMAND} -DCMAKE_BUILD_TYPE=${B_TYPE} -DMSDFGEN_DYNAMIC_RUNTIME=ON -DBUILD_SHARED_LIBS=ON -DMSDFGEN_USE_SKIA=OFF -DMSDFGEN_INSTALL=ON -DMSDFGEN_CORE_ONLY=ON -DMSDFGEN_BUILD_STANDALONE=OFF -DMSDFGEN_USE_VCPKG=OFF -S ${B_SRC} -B ${B_DEST}
    )
  endfunction()

  BuildThirdPartyDep(msdfgen https://github.com/Chlumsky/msdfgen ${VERSION} RESULT_DIR "" "BuildMsdfGen")

  list(APPEND CMAKE_PREFIX_PATH ${RESULT_DIR}/lib/cmake)

  find_package(msdfgen REQUIRED)

  target_include_directories(
    ${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${RESULT_DIR}/include>
    $<INSTALL_INTERFACE:include>
  )
  
  target_link_libraries(${PROJECT_NAME} PUBLIC msdfgen::msdfgen)

  unset(RESULT_DIR)
endmacro()


macro(SetDefaults IN_SCOPE IN_MODULE_NAME)
  cmake_policy(SET CMP0079 NEW)
  set(CMAKE_CXX_STANDARD 20)
  set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)
  set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)
  set(SCOPE_NAME ${IN_SCOPE})
  set(MODULE_NAME ${IN_SCOPE}${IN_MODULE_NAME})
  set(MODULE_NAME_SCOPED "${IN_SCOPE}::${IN_MODULE_NAME}")
endmacro()


function(SetGlobalExtDir)
  set_property(GLOBAL PROPERTY RIN_THIRD_PARTY_DIR_PROPERTY "${CMAKE_CURRENT_LIST_DIR}/../../ext")
endfunction()

# SetupProject
macro(SetupProject)
  
  file(GLOB_RECURSE SOURCE_FILES "${CMAKE_CURRENT_SOURCE_DIR}/lib/*/*.cpp" )

  file(GLOB_RECURSE INCLUDE_FILES "${CMAKE_CURRENT_SOURCE_DIR}/include/*/*.hpp" )
  
  #file(GLOB_RECURSE GENERATED_SOURCE_FILES "include/gen/*pp" )
  
  add_library(${MODULE_NAME} SHARED ${SOURCE_FILES} ${INCLUDE_FILES})
  add_library(${MODULE_NAME_SCOPED} ALIAS ${MODULE_NAME})

  source_group(TREE ${CMAKE_CURRENT_SOURCE_DIR} FILES "${INCLUDE_FILES};${SOURCE_FILES}")

  SetGlobalExtDir()

  get_property(RIN_THIRD_PARTY_DIR GLOBAL PROPERTY RIN_THIRD_PARTY_DIR_PROPERTY)

  target_include_directories(
    ${MODULE_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${RIN_THIRD_PARTY_DIR}>
    $<INSTALL_INTERFACE:include/ext>
  )
  target_include_directories(
    ${MODULE_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
    $<INSTALL_INTERFACE:include> 
  )

  set(${MODULE_NAME}_resources)

  if(EXISTS ${CMAKE_CURRENT_SOURCE_DIR}/resources)
    
    set(RIN_RESOURCES ${RIN_RESOURCES} ${CMAKE_CURRENT_SOURCE_DIR}/resources PARENT_SCOPE)
    install(
      DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/resources/
      DESTINATION resources
    )
  endif()

  target_compile_definitions(${MODULE_NAME} PUBLIC -DNOMINMAX -DGLM_ENABLE_EXPERIMENTAL)

  if(MSVC)
    target_compile_options(${MODULE_NAME} PRIVATE "/MP")
  endif()

  install(
    DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/include/
    DESTINATION include
  )

  install(
    TARGETS ${MODULE_NAME}
    EXPORT ${MODULE_NAME}-targets
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
    RUNTIME DESTINATION bin
  )

  install(
    EXPORT ${MODULE_NAME}-targets
   FILE ${MODULE_NAME}Config.cmake 
   NAMESPACE ${SCOPE_NAME}:: 
   DESTINATION lib/cmake/${SCOPE_NAME}
   )
endmacro()


macro(SetOutputDir SPECIFIC_TARGET SPECIFIC_DIR)
  #set(TARGET_DIR ${CMAKE_CURRENT_LIST_DIR}/bin/${CMAKE_BUILD_TYPE})
  set_target_properties(${SPECIFIC_TARGET}
      PROPERTIES
      LIBRARY_OUTPUT_DIRECTORY  ${SPECIFIC_DIR}
      RUNTIME_OUTPUT_DIRECTORY ${SPECIFIC_DIR}
      LIBRARY_OUTPUT_DIRECTORY_DEBUG  ${SPECIFIC_DIR}
      RUNTIME_OUTPUT_DIRECTORY_DEBUG ${SPECIFIC_DIR}
      LIBRARY_OUTPUT_DIRECTORY_RELEASE  ${SPECIFIC_DIR}
      RUNTIME_OUTPUT_DIRECTORY_RELEASE ${SPECIFIC_DIR}
  )
endmacro()


macro(AddModuleToModule IN_MODULE_PATH IN_MODULE_NAME)
  target_include_directories(
    ${MODULE_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${IN_MODULE_PATH}/include>
    $<INSTALL_INTERFACE:include>
  )

  target_link_libraries(${MODULE_NAME} PUBLIC ${IN_MODULE_NAME})
  add_dependencies(${MODULE_NAME} ${IN_MODULE_NAME})
endmacro()


function(CopyRuntimeDlls IN_FROM_TARGET IN_TO_TARGET)
  add_custom_command (TARGET ${IN_TO_TARGET} POST_BUILD
      COMMAND python ${CMAKE_CURRENT_FUNCTION_LIST_DIR}/../python/copy_s.py "$<TARGET_RUNTIME_DLLS:${IN_FROM_TARGET}>" "$<TARGET_FILE_DIR:${IN_TO_TARGET}>"
  )
endfunction()

function(CopyResourcesTo IN_TO_TARGET)
  add_custom_command(TARGET ${IN_TO_TARGET} POST_BUILD
    COMMAND python ${CMAKE_CURRENT_FUNCTION_LIST_DIR}/../python/copy_s.py "${RIN_RESOURCES}" "$<TARGET_FILE_DIR:${IN_TO_TARGET}>"
  )
endfunction()


function(LinkToExecutable IN_EXECUTABLE IN_TARGET)
  #SetOutputDir(${IN_TARGET} ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE})
  target_link_libraries(${IN_EXECUTABLE} PUBLIC ${IN_TARGET})

  CopyRuntimeDlls(${IN_TARGET} ${IN_EXECUTABLE})
endfunction()

